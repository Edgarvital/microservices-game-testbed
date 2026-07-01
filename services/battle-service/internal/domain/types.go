package domain

import (
	"math"
	"sync"
	"time"
)

type Vector2D struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
}

type BattleStats struct {
	MaxHP      int     `json:"maxHp"`
	BaseDamage int     `json:"baseDamage"`
	MoveSpeed  float64 `json:"moveSpeed"`
}

type Player struct {
	ID            string   `json:"id"`
	Position      Vector2D `json:"position"`
	TargetPosition Vector2D `json:"targetPosition"`
	MoveSpeed     float64  `json:"moveSpeed"`
	HP            int      `json:"hp"`
	MaxHP         int      `json:"maxHp"`
	BaseDamage    int      `json:"baseDamage"`
}

type Projectile struct {
	ID       string   `json:"id"`
	OwnerID  string   `json:"ownerId"`
	Position Vector2D `json:"position"`
	Velocity Vector2D `json:"velocity"`
	Damage   int      `json:"damage"`
	Radius   float64  `json:"radius"`
}

type GameStateSnapshot struct {
	MatchID     string       `json:"matchId"`
	TickAtUtc   time.Time    `json:"tickAtUtc"`
	Players     []Player     `json:"players"`
	Projectiles []Projectile `json:"projectiles"`
}

type PlayerInputType string

const (
	InputMove      PlayerInputType = "Move"
	InputCastSpell PlayerInputType = "CastSpell"
)

type PlayerInput struct {
	PlayerID string
	Type     PlayerInputType
	Target   Vector2D
}

type MatchFoundEvent struct {
	MatchID       string `json:"matchId"`
	ArenaID       string `json:"arenaId"`
	ArenaName     string `json:"arenaName"`
	PlayerAID     string `json:"playerAId"`
	PlayerBID     string `json:"playerBId"`
	PlayerAPower  int    `json:"playerAPower"`
	PlayerBPower  int    `json:"playerBPower"`
}

type GameRoom struct {
	Mu          sync.RWMutex
	MatchID     string
	ArenaID     string
	ArenaName   string
	Players     map[string]*Player
	Projectiles []Projectile
	Inputs      chan PlayerInput
	Snapshots   chan GameStateSnapshot
	createdAt   time.Time
}

func NewGameRoom(matchID, arenaID, arenaName string, playerA, playerB *Player) *GameRoom {
	return &GameRoom{
		MatchID:   matchID,
		ArenaID:   arenaID,
		ArenaName: arenaName,
		Players: map[string]*Player{
			playerA.ID: playerA,
			playerB.ID: playerB,
		},
		Projectiles: make([]Projectile, 0, 16),
		Inputs:      make(chan PlayerInput, 64),
		Snapshots:   make(chan GameStateSnapshot, 8),
		createdAt:   time.Now().UTC(),
	}
}

func NewPlayer(id string, position Vector2D, stats BattleStats) *Player {
	return &Player{
		ID:            id,
		Position:      position,
		TargetPosition: position,
		MoveSpeed:     stats.MoveSpeed,
		HP:            stats.MaxHP,
		MaxHP:         stats.MaxHP,
		BaseDamage:    stats.BaseDamage,
	}
}

func (r *GameRoom) HasPlayer(playerID string) bool {
	r.Mu.RLock()
	defer r.Mu.RUnlock()
	_, ok := r.Players[playerID]
	return ok
}

func (r *GameRoom) HandleInput(input PlayerInput) {
	r.Mu.Lock()
	defer r.Mu.Unlock()

	player, ok := r.Players[input.PlayerID]
	if !ok {
		return
	}

	switch input.Type {
	case InputMove:
		player.TargetPosition = input.Target
	case InputCastSpell:
		direction := Vector2D{X: input.Target.X - player.Position.X, Y: input.Target.Y - player.Position.Y}
		length := math.Hypot(direction.X, direction.Y)
		if length == 0 {
			return
		}

		velocity := Vector2D{X: direction.X / length * 18, Y: direction.Y / length * 18}
		r.Projectiles = append(r.Projectiles, Projectile{
			ID:       randomID(),
			OwnerID:  player.ID,
			Position: player.Position,
			Velocity: velocity,
			Damage:   player.BaseDamage,
			Radius:   0.6,
		})
	}
}

func (r *GameRoom) Snapshot() GameStateSnapshot {
	r.Mu.RLock()
	defer r.Mu.RUnlock()
	return r.SnapshotLocked()
}

func (r *GameRoom) SnapshotLocked() GameStateSnapshot {

	players := make([]Player, 0, len(r.Players))
	for _, player := range r.Players {
		players = append(players, *player)
	}

	projectiles := make([]Projectile, len(r.Projectiles))
	copy(projectiles, r.Projectiles)

	return GameStateSnapshot{
		MatchID:     r.MatchID,
		TickAtUtc:   time.Now().UTC(),
		Players:     players,
		Projectiles: projectiles,
	}
}
