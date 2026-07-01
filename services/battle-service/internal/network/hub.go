package network

import (
	"encoding/json"
	"time"
	"sync"

	"github.com/gorilla/websocket"
	"onlinegame/battle-service/internal/domain"
)

type Hub struct {
	mu          sync.RWMutex
	connections map[string]map[string]*websocket.Conn
	started     map[string]bool
}

func NewHub() *Hub {
	return &Hub{
		connections: make(map[string]map[string]*websocket.Conn),
		started:     make(map[string]bool),
	}
}

func (h *Hub) Register(matchID, playerID string, conn *websocket.Conn) {
	h.mu.Lock()
	defer h.mu.Unlock()
	if _, ok := h.connections[matchID]; !ok {
		h.connections[matchID] = make(map[string]*websocket.Conn)
	}
	h.connections[matchID][playerID] = conn
}

func (h *Hub) Unregister(matchID, playerID string) {
	h.mu.Lock()
	defer h.mu.Unlock()
	if room, ok := h.connections[matchID]; ok {
		if conn, exists := room[playerID]; exists {
			_ = conn.Close()
			delete(room, playerID)
		}
		if len(room) == 0 {
			delete(h.connections, matchID)
		}
	}
}

func (h *Hub) StartRoom(room *domain.GameRoom) {
	h.mu.Lock()
	if h.started[room.MatchID] {
		h.mu.Unlock()
		return
	}
	h.started[room.MatchID] = true
	h.mu.Unlock()

	go func() {
		for snapshot := range room.Snapshots {
			h.broadcast(room.MatchID, snapshot)
		}
	}()
}

func (h *Hub) broadcast(matchID string, snapshot domain.GameStateSnapshot) {
	payload, err := json.Marshal(snapshot)
	if err != nil {
		return
	}

	h.mu.RLock()
	conns := h.connections[matchID]
	copyConns := make([]*websocket.Conn, 0, len(conns))
	for _, conn := range conns {
		copyConns = append(copyConns, conn)
	}
	h.mu.RUnlock()

	for _, conn := range copyConns {
		_ = conn.SetWriteDeadline(time.Now().Add(5 * time.Second))
		if err := conn.WriteMessage(websocket.TextMessage, payload); err != nil {
			_ = conn.Close()
		}
	}
}
