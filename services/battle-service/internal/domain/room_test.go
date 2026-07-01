package domain

import "testing"

func TestGameRoomHandleMoveInput(t *testing.T) {
	room := NewGameRoom(
		"match-1",
		"arena-1",
		"Arena Bronze",
		NewPlayer("player-a", Vector2D{X: 20, Y: 50}, BattleStats{MaxHP: 100, BaseDamage: 10, MoveSpeed: 8}),
		NewPlayer("player-b", Vector2D{X: 80, Y: 50}, BattleStats{MaxHP: 100, BaseDamage: 10, MoveSpeed: 8}),
	)
	room.HandleInput(PlayerInput{PlayerID: "player-a", Type: InputMove, Target: Vector2D{X: 40, Y: 60}})

	if got := room.Players["player-a"].TargetPosition; got.X != 40 || got.Y != 60 {
		t.Fatalf("unexpected target position: %+v", got)
	}
}
