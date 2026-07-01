package gateway

import (
	"context"
	"fmt"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	"onlinegame/battle-service/internal/domain"
	"onlinegame/battle-service/internal/gateway/inventorypb"
)

type InventoryGRPCClient struct {
	conn *grpc.ClientConn
}

func NewInventoryGRPCClient(ctx context.Context, address string) (*InventoryGRPCClient, error) {
	if address == "" {
		return nil, fmt.Errorf("inventory grpc address is required")
	}

	conn, err := grpc.DialContext(ctx, address, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		return nil, err
	}

	return &InventoryGRPCClient{conn: conn}, nil
}

func (c *InventoryGRPCClient) Close() error {
	if c == nil || c.conn == nil {
		return nil
	}

	return c.conn.Close()
}

func (c *InventoryGRPCClient) GetBattleStats(ctx context.Context, playerID string) (domain.BattleStats, error) {
	req, err := inventorypb.NewGetBattleStatsRequest(playerID)
	if err != nil {
		return domain.BattleStats{}, err
	}

	resp, err := inventorypb.NewGetBattleStatsResponse()
	if err != nil {
		return domain.BattleStats{}, err
	}

	if err := c.conn.Invoke(ctx, inventorypb.MethodGetBattleStats, req, resp); err != nil {
		return domain.BattleStats{}, err
	}

	return inventorypb.BattleStatsFromResponse(resp)
}
