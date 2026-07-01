package network

import (
	"net/http"
	"time"

	"github.com/gorilla/websocket"
	"onlinegame/battle-service/internal/domain"
	"onlinegame/battle-service/internal/engine"
)

type Server struct {
	roomManager *engine.RoomManager
	hub         *Hub
	upgrader    websocket.Upgrader
}

func NewServer(roomManager *engine.RoomManager, hub *Hub) *Server {
	return &Server{
		roomManager: roomManager,
		hub:         hub,
		upgrader: websocket.Upgrader{
			CheckOrigin: func(*http.Request) bool { return true },
		},
	}
}

func (s *Server) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	matchID := r.URL.Query().Get("matchId")
	playerID := r.URL.Query().Get("playerId")
	if matchID == "" || playerID == "" {
		http.Error(w, "matchId and playerId are required", http.StatusBadRequest)
		return
	}

	room, ok := s.roomManager.Get(matchID)
	if !ok || !room.HasPlayer(playerID) {
		http.Error(w, "match not found", http.StatusNotFound)
		return
	}

	conn, err := s.upgrader.Upgrade(w, r, nil)
	if err != nil {
		return
	}

	s.hub.Register(matchID, playerID, conn)
	defer s.hub.Unregister(matchID, playerID)

	if snapshot := room.Snapshot(); snapshot.MatchID != "" {
		_ = conn.WriteJSON(snapshot)
	}

	for {
		var raw struct {
			Type   string  `json:"type"`
			TargetX float64 `json:"targetX"`
			TargetY float64 `json:"targetY"`
		}
		if err := conn.ReadJSON(&raw); err != nil {
			return
		}

		var inputType domain.PlayerInputType
		switch raw.Type {
		case string(domain.InputMove):
			inputType = domain.InputMove
		case string(domain.InputCastSpell):
			inputType = domain.InputCastSpell
		default:
			continue
		}

		select {
		case room.Inputs <- domain.PlayerInput{PlayerID: playerID, Type: inputType, Target: domain.Vector2D{X: raw.TargetX, Y: raw.TargetY}}:
		case <-time.After(100 * time.Millisecond):
		}
	}
}
