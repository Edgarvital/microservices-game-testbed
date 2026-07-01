package gateway

import "os"

type Config struct {
	HTTPAddr     string
	RedisAddr    string
	InventoryGRPCAddr string
	MatchChannel string
}

func LoadConfigFromEnv() Config {
	return Config{
		HTTPAddr:     getEnv("HTTP_ADDR", ":8080"),
		RedisAddr:    getEnv("REDIS_ADDR", "localhost:6379"),
		InventoryGRPCAddr: getEnv("INVENTORY_GRPC_ADDR", "localhost:5017"),
		MatchChannel: getEnv("MATCH_CHANNEL", "battle:match-found"),
	}
}

func getEnv(key, fallback string) string {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}
	return value
}
