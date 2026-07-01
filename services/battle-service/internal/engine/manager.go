package engine

import (
	"context"
	"fmt"
	"sync"

	"onlinegame/battle-service/internal/domain"
)

type BattleStatsProvider interface {
	GetBattleStats(ctx context.Context, playerID string) (domain.BattleStats, error)
}

type RoomManager struct {
	mu    sync.RWMutex
	rooms map[string]*domain.GameRoom
	ctx   context.Context
	stats BattleStatsProvider
}

func NewRoomManager() *RoomManager {
	return &RoomManager{
		rooms: make(map[string]*domain.GameRoom),
		ctx:   context.Background(),
	}
}

func NewRoomManagerWithContext(ctx context.Context, statsProvider BattleStatsProvider) *RoomManager {
	if ctx == nil {
		ctx = context.Background()
	}

	return &RoomManager{
		rooms: make(map[string]*domain.GameRoom),
		ctx:   ctx,
		stats: statsProvider,
	}
}

func (m *RoomManager) Get(matchID string) (*domain.GameRoom, bool) {
	m.mu.RLock()
	defer m.mu.RUnlock()
	room, ok := m.rooms[matchID]
	return room, ok
}

func (m *RoomManager) CreateOrGet(ctx context.Context, event domain.MatchFoundEvent) (*domain.GameRoom, error) {
	m.mu.RLock()
	room, ok := m.rooms[event.MatchID]
	m.mu.RUnlock()
	if ok {
		return room, nil
	}

	if m.stats == nil {
		return nil, fmt.Errorf("battle stats provider is not configured")
	}

	var (
		playerAStats domain.BattleStats
		playerBStats domain.BattleStats
		fetchErr     error
		fetchMu      sync.Mutex
		wg           sync.WaitGroup
	)

	fetchPlayerStats := func(playerID string, target *domain.BattleStats) {
		defer wg.Done()

		stats, err := m.stats.GetBattleStats(ctx, playerID)
		if err != nil {
			fetchMu.Lock()
			if fetchErr == nil {
				fetchErr = err
			}
			fetchMu.Unlock()
			return
		}

		*target = stats
	}

	wg.Add(2)
	go fetchPlayerStats(event.PlayerAID, &playerAStats)
	go fetchPlayerStats(event.PlayerBID, &playerBStats)
	wg.Wait()

	if fetchErr != nil {
		return nil, fetchErr
	}

	playerA := domain.NewPlayer(event.PlayerAID, domain.Vector2D{X: 20, Y: 50}, playerAStats)
	playerB := domain.NewPlayer(event.PlayerBID, domain.Vector2D{X: 80, Y: 50}, playerBStats)

	room = domain.NewGameRoom(event.MatchID, event.ArenaID, event.ArenaName, playerA, playerB)

	m.mu.Lock()
	defer m.mu.Unlock()
	if existing, ok := m.rooms[event.MatchID]; ok {
		return existing, nil
	}

	m.rooms[event.MatchID] = room
	go RunRoomLoop(m.ctx, room)
	return room, nil
}
