package gateway

import (
	"context"
	"encoding/json"
	"errors"
	"log"

	"github.com/redis/go-redis/v9"
	"onlinegame/battle-service/internal/domain"
	"onlinegame/battle-service/internal/engine"
	"onlinegame/battle-service/internal/network"
)

type MatchFoundListener struct {
	client       *redis.Client
	channelName  string
	roomManager  *engine.RoomManager
	hub          *network.Hub
}

func NewMatchFoundListener(client *redis.Client, channelName string, roomManager *engine.RoomManager, hub *network.Hub) *MatchFoundListener {
	return &MatchFoundListener{client: client, channelName: channelName, roomManager: roomManager, hub: hub}
}

func (l *MatchFoundListener) Run(ctx context.Context) error {
	sub := l.client.Subscribe(ctx, l.channelName)
	defer func() { _ = sub.Close() }()

	if _, err := sub.Receive(ctx); err != nil {
		return err
	}

	ch := sub.Channel()
	for {
		select {
		case <-ctx.Done():
			return ctx.Err()
		case msg, ok := <-ch:
			if !ok {
				return errors.New("redis subscription closed")
			}

			var event domain.MatchFoundEvent
			if err := json.Unmarshal([]byte(msg.Payload), &event); err != nil {
				log.Printf("invalid match event payload: %v", err)
				continue
			}

			room, err := l.roomManager.CreateOrGet(ctx, event)
			if err != nil {
				log.Printf("unable to create game room for match %s: %v", event.MatchID, err)
				continue
			}
			l.hub.StartRoom(room)
		}
	}
}
