using Mirror;
using SparkAge.Model.Hex;
using SparkAge.Model.Units;
using System.Collections.Generic;

namespace SparkAge.Controller.Network
{
    #region 一、联机房间消息
    //客户端->服务端
    public struct PlayerNameMsg : NetworkMessage { public string Name; }
    public struct PlayerReadyMsg : NetworkMessage { public bool Ready; }
    public struct PlayerCharacterMsg : NetworkMessage { public int CharacterId; }
    public struct SelMapMsg : NetworkMessage { public int mapId; }
    //服务端->客户端
    public struct PlayerIdMsg : NetworkMessage { public int PlayerId; }
    //服务端->所有
    public struct RoomStateMsg : NetworkMessage { public SlotData[] slots; }
    public struct GameMapMsg : NetworkMessage { public int mapId; }
    public struct StartGameMsg : NetworkMessage { public bool AllReady; public SlotData[] Slots; public int MapId; }
    public struct SlotData
    {
        public int PlayerId;
        public string Name;
        public int CharacterId;
        public bool Ready;
        public bool isAI;
        //为人类玩家分配
        public void Assign(string name)
        {
            Name = name;
            CharacterId = 0;
            Ready = false;
            isAI = false;
        }
        //重置槽位，分配给Ai
        public void Reset()
        {
            Name = "AI";
            CharacterId = 0;
            Ready = true;
            isAI = true;
        }
    }
    #endregion

    #region 二、游戏局内消息
    //客户端->服务端
    public enum OrderType
    {
        MoveUnit,
        AttackUnit,
        AttackCity,
        BuildUnit,
        FoundCity,
        EndPhase
    }
    public struct OrderMsg : NetworkMessage
    {
        //必填
        public OrderType Type;
        public int PlayerId;
        //根据类型填
        public int AggressiveUnitId;
        public int AggressiveCityId;
        public UnitType PassiveUnitType;
        public int PassiveUnitId;
        public int PassiveCityId;
        public HexCoord Target;
    }
    public struct GameReadyMsg: NetworkMessage
    {

    }
    //服务端->客户端
    public struct TipMsg : NetworkMessage
    {
        public string Tip;
    }
    //服务端->所有
    public struct GameUpdateMsg : NetworkMessage
    {
        public GameStateDeltaMsg Delta;
        public HintMsg Hint;
    }

    //游戏状态变化
    public struct GameStateDeltaMsg
    {
        public int turnNumber;
        public int curPlayer;
        public List<UnitData> UnitDatas;
        public List<CityData> CityDatas;
        public List<PlayerData> PlayerDatas;
    }
    public struct UnitData
    {
        public int Id;
        public int Owner;
        public UnitType Type;
        public HexCoord Position;
        public int Hp;
        public int MovementLeft;
        public bool IsDead;
    }
    public struct CityData
    {
        public int Id;
        public int Owner;
        public string Name;
        public HexCoord Position;
        public int Production;
        public int Hp;
    }
    public struct PlayerData
    {
        public int Id;
        public bool IsAlive;
    }
    //动画消息
    public enum HintType
    {
        NoHint,
        InitialGameUpdate,
        BuildUnit,
        FoundCity,
        MoveUnit,
        AttackUnit,
        AttackCity,
        EndPhase
    }
    public struct HintMsg
    {
        public HintType Type;
        public List<int> InitialUnits;
        public UnitHintData UnitHintData;
        public CityHintData CityHintData;
    }
    public struct UnitHintData
    {
        public int UnitId;
        public List<HexCoord> Path;
        public int TargetUnitId;
        public int TargetCityId;
        public bool CanEnter;
        public bool AttackerIsDead;
        public bool DefenderIsDead;
        public bool CityIsCaptured;
    }
    public struct CityHintData
    {
        public int CityId;
        public int UnitId;
    }
    #endregion
}
