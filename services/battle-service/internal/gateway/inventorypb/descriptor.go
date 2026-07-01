package inventorypb

import (
	"fmt"
	"sync"

	"onlinegame/battle-service/internal/domain"

	"google.golang.org/protobuf/proto"
	"google.golang.org/protobuf/reflect/protodesc"
	"google.golang.org/protobuf/reflect/protoreflect"
	"google.golang.org/protobuf/types/descriptorpb"
	"google.golang.org/protobuf/types/dynamicpb"
)

const MethodGetBattleStats = "/onlinegame.inventory.v1.InventoryService/GetBattleStats"

var (
	once                        sync.Once
	initErr                     error
	fileDescriptor              protoreflect.FileDescriptor
	requestDescriptor           protoreflect.MessageDescriptor
	responseDescriptor          protoreflect.MessageDescriptor
	requestPlayerIDField        protoreflect.FieldDescriptor
	responseMaxHPField          protoreflect.FieldDescriptor
	responseBaseDamageField     protoreflect.FieldDescriptor
	responseMoveSpeedField      protoreflect.FieldDescriptor
)

func NewGetBattleStatsRequest(playerID string) (*dynamicpb.Message, error) {
	if err := ensureDescriptors(); err != nil {
		return nil, err
	}

	msg := dynamicpb.NewMessage(requestDescriptor)
	msg.Set(requestPlayerIDField, protoreflect.ValueOfString(playerID))
	return msg, nil
}

func NewGetBattleStatsResponse() (*dynamicpb.Message, error) {
	if err := ensureDescriptors(); err != nil {
		return nil, err
	}

	return dynamicpb.NewMessage(responseDescriptor), nil
}

func BattleStatsFromResponse(msg *dynamicpb.Message) (domain.BattleStats, error) {
	if err := ensureDescriptors(); err != nil {
		return domain.BattleStats{}, err
	}

	if msg == nil {
		return domain.BattleStats{}, fmt.Errorf("battle stats response is nil")
	}

	return domain.BattleStats{
		MaxHP:      int(msg.Get(responseMaxHPField).Int()),
		BaseDamage: int(msg.Get(responseBaseDamageField).Int()),
		MoveSpeed:  msg.Get(responseMoveSpeedField).Float(),
	}, nil
}

func ensureDescriptors() error {
	once.Do(func() {
		fileProto := &descriptorpb.FileDescriptorProto{
			Syntax:  proto.String("proto3"),
			Name:    proto.String("player_inventory.proto"),
			Package: proto.String("onlinegame.inventory.v1"),
			MessageType: []*descriptorpb.DescriptorProto{
				{
					Name: proto.String("GetBattleStatsRequest"),
					Field: []*descriptorpb.FieldDescriptorProto{
						{
							Name:     proto.String("player_id"),
							Number:   proto.Int32(1),
							Label:    descriptorpb.FieldDescriptorProto_LABEL_OPTIONAL.Enum(),
							Type:     descriptorpb.FieldDescriptorProto_TYPE_STRING.Enum(),
							JsonName: proto.String("playerId"),
						},
					},
				},
				{
					Name: proto.String("GetBattleStatsResponse"),
					Field: []*descriptorpb.FieldDescriptorProto{
						{
							Name:     proto.String("max_hp"),
							Number:   proto.Int32(1),
							Label:    descriptorpb.FieldDescriptorProto_LABEL_OPTIONAL.Enum(),
							Type:     descriptorpb.FieldDescriptorProto_TYPE_INT32.Enum(),
							JsonName: proto.String("maxHp"),
						},
						{
							Name:     proto.String("base_damage"),
							Number:   proto.Int32(2),
							Label:    descriptorpb.FieldDescriptorProto_LABEL_OPTIONAL.Enum(),
							Type:     descriptorpb.FieldDescriptorProto_TYPE_INT32.Enum(),
							JsonName: proto.String("baseDamage"),
						},
						{
							Name:     proto.String("move_speed"),
							Number:   proto.Int32(3),
							Label:    descriptorpb.FieldDescriptorProto_LABEL_OPTIONAL.Enum(),
							Type:     descriptorpb.FieldDescriptorProto_TYPE_DOUBLE.Enum(),
							JsonName: proto.String("moveSpeed"),
						},
					},
				},
			},
			Service: []*descriptorpb.ServiceDescriptorProto{
				{
					Name: proto.String("InventoryService"),
					Method: []*descriptorpb.MethodDescriptorProto{
						{
							Name:       proto.String("GetBattleStats"),
							InputType:  proto.String(".onlinegame.inventory.v1.GetBattleStatsRequest"),
							OutputType: proto.String(".onlinegame.inventory.v1.GetBattleStatsResponse"),
						},
					},
				},
			},
		}

		fileDescriptor, initErr = protodesc.NewFile(fileProto, nil)
		if initErr != nil {
			return
		}

		requestDescriptor = fileDescriptor.Messages().ByName("GetBattleStatsRequest")
		responseDescriptor = fileDescriptor.Messages().ByName("GetBattleStatsResponse")
		requestPlayerIDField = requestDescriptor.Fields().ByName("player_id")
		responseMaxHPField = responseDescriptor.Fields().ByName("max_hp")
		responseBaseDamageField = responseDescriptor.Fields().ByName("base_damage")
		responseMoveSpeedField = responseDescriptor.Fields().ByName("move_speed")
	})

	return initErr
}
