package engine

import (
	"context"
	"math"
	"time"

	"onlinegame/battle-service/internal/domain"
)

const ticksPerSecond = 30

func RunRoomLoop(ctx context.Context, room *domain.GameRoom) {
	ticker := time.NewTicker(time.Second / ticksPerSecond)
	defer ticker.Stop()

	const dt = 1.0 / ticksPerSecond

	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			stepRoom(room, dt)
		}
	}
}

func stepRoom(room *domain.GameRoom, dt float64) {
	for {
		select {
		case input := <-room.Inputs:
			room.HandleInput(input)
		default:
			goto updates
		}
	}

updates:
	room.Mu.Lock()
	for _, player := range room.Players {
		moveTowards(&player.Position, player.TargetPosition, player.MoveSpeed*dt)
	}

	updated := room.Projectiles[:0]
	for _, projectile := range room.Projectiles {
		projectile.Position.X += projectile.Velocity.X * dt
		projectile.Position.Y += projectile.Velocity.Y * dt

		hit := false
		for _, player := range room.Players {
			if player.ID == projectile.OwnerID || player.HP <= 0 {
				continue
			}

			if distance(player.Position, projectile.Position) <= projectile.Radius+0.8 {
				player.HP -= projectile.Damage
				if player.HP < 0 {
					player.HP = 0
				}
				hit = true
				break
			}
		}

		if !hit {
			updated = append(updated, projectile)
		}
	}
	room.Projectiles = updated
	snapshot := room.SnapshotLocked()
	room.Mu.Unlock()

	select {
	case room.Snapshots <- snapshot:
	default:
	}
}

func moveTowards(position *domain.Vector2D, target domain.Vector2D, maxDistance float64) {
	dx := target.X - position.X
	dy := target.Y - position.Y
	distance := math.Hypot(dx, dy)
	if distance == 0 || distance <= maxDistance {
		position.X = target.X
		position.Y = target.Y
		return
	}
	position.X += dx / distance * maxDistance
	position.Y += dy / distance * maxDistance
}

func distance(a, b domain.Vector2D) float64 {
	return math.Hypot(a.X-b.X, a.Y-b.Y)
}
