using Mirror;
using SparkAge.Model.Orders;
using SparkAge.View.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SparkAge.Controller.GameController;

namespace SparkAge.Controller.Network
{
    public class NetworkMgr : NetworkManager
    {
        public static NetworkMgr Instance { get; private set; }

        //连接状态相关事件
        public event Action Connected;
        public event Action Disconnected;
        public event Action<string> ConnectFailed;

        //房间界面内容
        int[] slotConns = new int[4];
        SlotData[] slots = new SlotData[4];
        public SlotData[] Slots => slots;
        int GetPlayerId(int connectionId) => slots[Array.FindIndex(slotConns, id => id == connectionId)].PlayerId;
        int mapId;
        public int MapId => mapId;
        int myPlayerId;
        public int MyPlayerId => myPlayerId;
        //UI事件
        public event Action<SlotData[]> RoomUpdateEvent;
        //GameController接口
        INetworkInput networkInput;
        public void SetNetworkInput(INetworkInput networkInput) => this.networkInput = networkInput;
        //消息队列
        Queue<GameStateDeltaMsg> gameStateDeltaMsgQueue = new();
        public Queue<GameStateDeltaMsg> GameStateDeltaMsgQueue => gameStateDeltaMsgQueue;


        public override void Awake()
        {
            base.Awake();
            Instance = this;

            //初始化slots和slotConns
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].PlayerId = i + 1;
                slots[i].Reset();
                slotConns[i] = -1;
            }
            //初始化map
            mapId = 0;
        }

        #region 一、消息注册与消息处理

        #region 1 服务端侧
        public override void OnStartServer()
        {
            base.OnStartServer();
            //注册处理器
            NetworkServer.ReplaceHandler<PlayerNameMsg>(OnPlayerNameMsg);
            NetworkServer.ReplaceHandler<PlayerReadyMsg>(OnPlayerReadyMsg);
            NetworkServer.ReplaceHandler<PlayerCharacterMsg>(OnPlayerCharacterMsg);
            NetworkServer.ReplaceHandler<SelMapMsg>(OnSelMapMsg);

            NetworkServer.ReplaceHandler<OrderMsg>(OnOrderMsg);
        }
        //联机房间消息处理器
        void OnPlayerNameMsg(NetworkConnectionToClient conn, PlayerNameMsg msg)
        {
            int idx = 0;
            while (idx < slots.Length && slotConns[idx] >= 0) idx++;
            if (idx >= slots.Length) return;
            //分配槽位
            slots[idx].Assign(msg.Name);
            slotConns[idx] = conn.connectionId;
            //单发给客户端Id信息
            conn.Send(new PlayerIdMsg { PlayerId = slots[idx].PlayerId });
            //广播槽位信息
            NetworkServer.SendToAll(new RoomStateMsg { slots = slots });
        }
        void OnPlayerReadyMsg(NetworkConnectionToClient conn, PlayerReadyMsg msg)
        {
            for (int i = 0; i < slotConns.Length; i++)
            {
                if (slotConns[i] == conn.connectionId)
                {
                    slots[i].Ready = msg.Ready;
                    break;
                }
            }
            NetworkServer.SendToAll(new RoomStateMsg { slots = slots });
            //检查是否全部准备
            if (slots[0].Ready && slots[1].Ready && slots[2].Ready && slots[3].Ready)
            {
                NetworkServer.SendToAll(new StartGameMsg { AllReady = true, Slots = slots, MapId = mapId });
            }
        }
        void OnPlayerCharacterMsg(NetworkConnectionToClient conn, PlayerCharacterMsg msg)
        {
            bool isLock = false;
            for (int j = 0; j < slots.Length; j++)
            {
                if (slots[j].CharacterId == msg.CharacterId && msg.CharacterId != 0)
                {
                    isLock = true;
                }
            }
            if (!isLock)
            {
                for (int i = 0; i < slotConns.Length; i++)
                {
                    if (slotConns[i] == conn.connectionId)
                    {
                        slots[i].CharacterId = msg.CharacterId;
                        break;
                    }
                }
            }
            NetworkServer.SendToAll(new RoomStateMsg { slots = slots });
        }
        void OnSelMapMsg(NetworkConnectionToClient conn, SelMapMsg msg)
        {
            mapId = msg.mapId;
            NetworkServer.SendToAll(new GameMapMsg { mapId = mapId });
        }
        //游戏局内消息处理器
        void OnOrderMsg(NetworkConnectionToClient conn, OrderMsg msg)
        {
            BaseOrder order = ToOrder(msg);
            ExecuteResult result = networkInput.ExecuteOrder(order);
            if (result.Type == ExecuteResultType.Tip)
                conn.Send(result.Tip);
            else
                NetworkServer.SendToAll(result.GameStateDelta);
        }

        #endregion

        #region 2 客户端侧
        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkClient.ReplaceHandler<PlayerIdMsg>(OnPlayerIdMsg);
            NetworkClient.ReplaceHandler<RoomStateMsg>(OnRoomStateMsg);
            NetworkClient.ReplaceHandler<StartGameMsg>(OnStartGameMsg);
            NetworkClient.ReplaceHandler<GameMapMsg>(OnGameMapMsg);

            NetworkClient.ReplaceHandler<TipMsg>(OnTipMsg);
            NetworkClient.ReplaceHandler<GameStateDeltaMsg>(OnGameStateDeltaMsg);
        }
        //联机房间消息处理器
        void OnPlayerIdMsg(PlayerIdMsg msg)
        {
            //本地记录玩家Id
            myPlayerId = msg.PlayerId;
            UIManager.Instance.ShowPanel<RoomPanel>();
        }
        void OnRoomStateMsg(RoomStateMsg msg)
        {
            slots = msg.slots;
            //更新房间UI状态
            RoomUpdateEvent?.Invoke(msg.slots);
        }
        void OnGameMapMsg(GameMapMsg msg)
        {
            //修改UI，并存到本地
            mapId = msg.mapId;

        }
        void OnStartGameMsg(StartGameMsg msg)
        {
            print("开始游戏");
            //存储来自服务端的槽位和地图数据
            slots = msg.Slots;
            mapId = msg.MapId;
            //隐藏UI
            UIManager.Instance.HidePanel<RoomPanel>();
            UIManager.Instance.HidePanel<BeginPanel>();
            //切换场景
            SceneManager.LoadSceneAsync("GameScene");
        }
        //游戏局内消息处理器
        void OnTipMsg(TipMsg msg)
        {
            networkInput.ApplyTip(msg);
        }
        void OnGameStateDeltaMsg(GameStateDeltaMsg msg)
        {
            if (NetworkServer.active) return;
            if(networkInput == null)
            {
                gameStateDeltaMsgQueue.Enqueue(msg);
                return;
            }
            networkInput.ApplySnapShot(msg);
        }

        #endregion

        #endregion

        #region 二、连接状态相关
        //处理服务端侧断连逻辑
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            for (int i = 0; i < slotConns.Length; i++)
            {
                if (slotConns[i] == conn.connectionId)
                { 
                    slotConns[i] = -1;
                    slots[i].Reset();
                    break;
                }
            }
            NetworkServer.SendToAll(new RoomStateMsg { slots = slots });
        }
        //处理客户端侧连接逻辑
        public override void OnClientConnect()
        {
            base.OnClientConnect();
            NetworkClient.Send(new PlayerNameMsg() { Name = LocalPlayerProfile.NickName });
            Connected?.Invoke();
        }
        //处理客户端侧断连逻辑
        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Disconnected?.Invoke();
        }
        public override void OnClientError(TransportError error, string reason)
        {
            base.OnClientError(error, reason);
            ConnectFailed?.Invoke(reason);
        }
        #endregion

        #region 三、提供给UI的方法
        public void LeaveRoom()
        {
            if (mode == NetworkManagerMode.Host)
                StopHost();
            else if (mode == NetworkManagerMode.ClientOnly)
                StopClient();
        }
        public void PlayerIsReady(bool ready)
        {
            //向服务端发送准备信息
            NetworkClient.Send(new PlayerReadyMsg() { Ready = ready });
        }
        public void SetCharacter(int idx)
        {
            //向服务端发送取消准备信息
            NetworkClient.Send(new PlayerCharacterMsg() { CharacterId = idx });
        }
        public void SetMap(int idx)
        {
            if (mode == NetworkManagerMode.Host)
                NetworkClient.Send<SelMapMsg>(new SelMapMsg() { mapId = idx });
        }
        #endregion

        #region 四、Order翻译器+提供给GameController的方法
        OrderMsg ToMsg(BaseOrder order)
        {
            switch (order)
            {
                case MoveUnitOrder o:
                    return new OrderMsg { Type = OrderType.MoveUnit, PlayerId = o.PlayerId, AggressiveUnitId = o.UnitID, Target = o.Target };
                case AttackUnitOrder o:
                    return new OrderMsg { Type = OrderType.AttackUnit, PlayerId = o.PlayerId, AggressiveUnitId = o.AttackerID, PassiveUnitId = o.DefenderID };
                case AttackCityOrder o:
                    return new OrderMsg { Type = OrderType.AttackCity, PlayerId = o.PlayerId, AggressiveUnitId = o.AttackerID, PassiveCityId = o.CityID };
                case FoundCityOrder o:
                    return new OrderMsg { Type = OrderType.FoundCity, PlayerId = o.PlayerId, AggressiveUnitId = o.UnitID };
                case BuildUnitOrder o:
                    return new OrderMsg { Type = OrderType.BuildUnit, PlayerId = o.PlayerId, AggressiveCityId = o.CityID, PassiveUnitType = o.Type };
                case EndPhaseOrder o:
                    return new OrderMsg { Type = OrderType.EndPhase, PlayerId = o.PlayerId };
                default:
                    return default(OrderMsg);
            }
        }
        BaseOrder ToOrder(OrderMsg msg)
        {
            switch (msg.Type)
            {
                case OrderType.MoveUnit:
                    return new MoveUnitOrder(msg.PlayerId, msg.AggressiveUnitId, msg.Target);
                case OrderType.AttackUnit:
                    return new AttackUnitOrder(msg.PlayerId, msg.AggressiveUnitId, msg.PassiveUnitId);
                case OrderType.AttackCity:
                    return new AttackCityOrder(msg.PlayerId, msg.AggressiveUnitId, msg.PassiveCityId);
                case OrderType.FoundCity:
                    return new FoundCityOrder(msg.PlayerId, msg.AggressiveUnitId);
                case OrderType.BuildUnit:
                    return new BuildUnitOrder(msg.PlayerId, msg.AggressiveCityId, msg.PassiveUnitType);
                case OrderType.EndPhase:
                    return new EndPhaseOrder(msg.PlayerId);
                default:
                    return null;
            }
        } 
        public void SendOrder(BaseOrder order)
        {
            NetworkClient.Send(ToMsg(order));
        }
        public void SendInitialSnapShot(GameStateDeltaMsg msg)
        {
            NetworkServer.SendToAll(msg);
        }
        #endregion
    }
};
