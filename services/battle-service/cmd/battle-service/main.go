package main

import (
	"context"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"onlinegame/battle-service/internal/engine"
	"onlinegame/battle-service/internal/gateway"
	"onlinegame/battle-service/internal/network"
	"github.com/redis/go-redis/v9"
)

func main() {
	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	config := gateway.LoadConfigFromEnv()

	redisClient := redis.NewClient(&redis.Options{Addr: config.RedisAddr})
	defer func() { _ = redisClient.Close() }()

	inventoryClient, err := gateway.NewInventoryGRPCClient(ctx, config.InventoryGRPCAddr)
	if err != nil {
		log.Fatal(err)
	}
	defer func() { _ = inventoryClient.Close() }()

	roomManager := engine.NewRoomManagerWithContext(ctx, inventoryClient)
	hub := network.NewHub()
	listener := gateway.NewMatchFoundListener(redisClient, config.MatchChannel, roomManager, hub)
	server := network.NewServer(roomManager, hub)

	go func() {
		if err := listener.Run(ctx); err != nil && ctx.Err() == nil {
			log.Printf("redis listener stopped: %v", err)
		}
	}()

	mux := http.NewServeMux()
	mux.Handle("/ws", server)
	mux.HandleFunc("/healthz", func(w http.ResponseWriter, _ *http.Request) {
		w.WriteHeader(http.StatusOK)
		_, _ = w.Write([]byte("ok"))
	})

	httpServer := &http.Server{
		Addr:              config.HTTPAddr,
		Handler:           mux,
		ReadHeaderTimeout:  5 * time.Second,
		IdleTimeout:        60 * time.Second,
		WriteTimeout:       10 * time.Second,
	}

	go func() {
		<-ctx.Done()
		shutdownCtx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		defer cancel()
		_ = httpServer.Shutdown(shutdownCtx)
	}()

	log.Printf("battle-service listening on %s", config.HTTPAddr)
	if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		log.Fatal(err)
	}
}
