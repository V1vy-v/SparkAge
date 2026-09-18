using Mirror;
using SparkAge.Config;
using SparkAge.Controller.Network;
using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Orders;
using SparkAge.Model.Players;
using SparkAge.Model.StaticInfos;
using SparkAge.Model.Units;
using SparkAge.View;
using SparkAge.View.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static SparkAge.Controller.GameController;
using static SparkAge.Framework.EventCenter.EventDefine;
using static SparkAge.Model.GameState;

namespace SparkAge.Controller
{
    /// <summary>
    /// UI层输入接口
    /// </summary>
    public interface IUIInput
    {
        public void RequestBuildUnit(int cityId, UnitType unitType);
        public void RequestFoundCity(int unitId);
        public void RequestEndPhase();
    }
    /// <summary>
    /// 网络层输入接口
    /// </summary>
    public interface INetworkInput
    {
        public ExecuteResult ExecuteOrder(BaseOrder order);
        public void ApplyTip(TipMsg msg);
        public void ApplySnapShot(GameStateDeltaMsg msg);
    }
    public enum GamePhase
    {
        PlayerTurn,  //等待玩家输入
        OtherPhase,  //其他玩家操作中
        AiPhase,     //Ai操作中
        GameOver     //玩家失败
    }
    /// <summary>
    /// 游戏控制层
    /// </summary>
    public class GameController : MonoBehaviour, IUIInput, INetworkInput
    {
        [SerializeField] float hexSize = 1f;                //单位大小
        [SerializeField] CameraController CameraController; //相机控制器

        //控制层引用
        AiOrders ai;
        GameSession session;
        //数据层引用
        GameState state;
        GameInfo gameInfo;
        //视图层引用
        MapView mapView;
        UnitView unitView;
        SelectionView selectionView;
        CityView cityView;

        //控制器状态
        GamePhase phase = GamePhase.PlayerTurn;
        bool isBlockingInput = false;

        public int MyPlayerId => NetworkMgr.Instance.MyPlayerId;
        public bool IsMyTurn => NetworkMgr.Instance.MyPlayerId == state.CurrentPlayer;
        public bool IsMine(int own) => MyPlayerId == own;

        private void Awake()
        {
            //配置装配与注入
            InitGameInfo();
            NetworkMgr.Instance.SetNetworkInput(this);
            UIManager.Instance.SetUIInput(this);
            //创建游戏状态
            StartGame();
        }
        private void Start()
        {
            //构建地图
            mapView.BuildTiles();

            //显示HUD
            var panel = UIManager.Instance.ShowPanel<HUD>();
            panel.InitMyInfo(gameInfo.GetPlayerInfo(MyPlayerId));
            panel.UpdateHUD(gameInfo.GetPlayerInfo(state.CurrentPlayer).Name, state.TurnNumber);

            //初始化摄像机脚本
            (HexCoord, HexCoord, HexCoord) keyPos = state.GetMapKeyPos();
            CameraController.Init(keyPos.Item1, keyPos.Item2, keyPos.Item3, keyPos.Item1);

            //服务端创建初始状态并广播
            if (NetworkServer.active)
            {
                state.CreateInitialUnits();

                List<UnitData> initUnits = new List<UnitData>();
                foreach(var unit in state.AllUnits)
                {
                    //更新本地表现层
                    unitView.BuildUnit(unit);

                    //获取初始世界状态并打包进msg
                    initUnits.Add(new UnitData
                    { 
                        Id = unit.ID,
                        Owner = unit.Owner,
                        Type = unit.Type,
                        Position = unit.Position,
                        Hp = unit.Hp,
                        MovementLeft = unit.MovementLeft,
                        IsDead = unit.IsDead
                    });
                }
                GameStateDeltaMsg msg = new GameStateDeltaMsg
                {
                    turnNumber = state.TurnNumber,
                    curPlayer = state.CurrentPlayer,
                    UnitDatas = initUnits,
                    CityDatas = new List<CityData> { },
                    PlayerDatas = new List<PlayerData> { }
                };
                NetworkMgr.Instance.SendInitialSnapShot(msg);
            }
            else
            {
                if(NetworkMgr.Instance.GameStateDeltaMsgQueue.Count > 0)
                    ApplySnapShot(NetworkMgr.Instance.GameStateDeltaMsgQueue.Dequeue());
            }
        }
        private void Update()
        {
            switch (phase)
            {
                case GamePhase.PlayerTurn:
                    HandlePlayerInput();
                    break;
                case GamePhase.OtherPhase:
                    break;
                case GamePhase.AiPhase:
                    break;
                case GamePhase.GameOver:
                    return;
            }
        }
        private void OnDestroy()
        {
            NetworkMgr.Instance.SetNetworkInput(null);
            UIManager.Instance.SetUIInput(null);
        }

        //================ 初始化相关 ==================
        /// <summary>
        /// 初始配置表读取与注入
        /// </summary>
        private void InitGameInfo()
        {
            //装配游戏房间配置（玩家选择角色和地图）
            gameInfo = new GameInfo();
            gameInfo.MapInfo = ConfigMgr.Instance.StaticInfo.MapInfos[NetworkMgr.Instance.MapId];
            foreach(var slot in NetworkMgr.Instance.Slots)
            {
                gameInfo.PlayerInfos.Add(new PlayerInfo { Id = slot.PlayerId, Name = slot.Name, CharacterInfo = ConfigMgr.Instance.StaticInfo.CharacterInfos[slot.CharacterId] });
            }
        }
        private void StartGame()
        {
            state = new GameState(gameInfo, ConfigMgr.Instance.StaticInfo);
            state.Init();

            ai = new AiOrders();
            ai.Init(state);

            session = new GameSession();
            session.Init(NetworkMgr.Instance.Slots);

            mapView = gameObject.AddComponent<MapView>();
            mapView.Init(state, hexSize);

            unitView = gameObject.AddComponent<UnitView>();
            unitView.Init(state, hexSize);

            selectionView = gameObject.AddComponent<SelectionView>();
            selectionView.Init(state, hexSize, mapView.HexMesh);

            cityView = gameObject.AddComponent<CityView>();
            cityView.Init(state, hexSize);
        }

        //================= 交互相关 ===================
        /// <summary>
        /// 获取点击处地块Hex
        /// </summary>
        /// <returns></returns>
        public HexCoord? GetClickHex()
        {
            //能被射线检测即在地图内
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float dist))
            {
                HexCoord clickHex = HexLayout.PixelToHex(ray.GetPoint(dist), hexSize);

                if (state.Map.IsInMap(clickHex))
                    return clickHex;
            }

            //不在地图内，无高亮
            return null;
        }
        /// <summary>
        /// 玩家输入监听入口
        /// </summary>
        private void HandlePlayerInput()
        {
            //============= 键盘输入 ==============
            //回合结束
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SubmitOrder(new EndPhaseOrder(MyPlayerId));
            }
            //F键建城
            if (Input.GetKeyDown(KeyCode.F) && selectionView.SelectedUnit != null && selectionView.SelectedUnit.Type == UnitType.Settler)
            {
                SubmitOrder(new FoundCityOrder(MyPlayerId, selectionView.SelectedUnit.ID));
            }
            //1 2键造兵
            if (selectionView.SelectedCity != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    SubmitOrder(new BuildUnitOrder(MyPlayerId, selectionView.SelectedCity.ID, UnitType.Settler));
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    SubmitOrder(new BuildUnitOrder(MyPlayerId, selectionView.SelectedCity.ID, UnitType.Warrior));
                }
            }

            //============= 鼠标输入 ==============
            //输入锁定：动画锁定+UI锁定
            if (isBlockingInput && UIManager.Instance.IsBlockingUI || UIManager.Instance.IsPointerOverUI)
                return;

            //鼠标左键点击
            if (Input.GetMouseButtonDown(0))
            {
                //高亮
                selectionView.HandleClick(GetClickHex());
                //UI显示
                if(selectionView.SelectedUnit != null && selectionView.SelectedUnit.Owner == MyPlayerId)
                {
                    var panel = UIManager.Instance.ShowPanel<SelUnitPanel>();
                    panel.UpdatePanel(selectionView.SelectedUnit);
                }
                else
                    UIManager.Instance.HidePanel<SelUnitPanel>();

                if (selectionView.SelectedCity != null && selectionView.SelectedCity.Owner == MyPlayerId)
                {
                    var panel = UIManager.Instance.ShowPanel<SelCityPanel>();
                    panel.UpdatePanel(selectionView.SelectedCity);
                }
                else
                    UIManager.Instance.HidePanel<SelCityPanel>();
            }
            //鼠标右键点击
            if (Input.GetMouseButtonDown(1) && selectionView.SelectedUnit != null)
            {
                HexCoord? hex = GetClickHex();
                if (hex != null)
                {
                    Unit tarUnit = state.GetUnitAt((HexCoord)hex);
                    City tarCity = state.GetCityAt((HexCoord)hex);
                    if (tarUnit == null && (tarCity == null || tarCity.Owner == selectionView.SelectedUnit.Owner))
                        SubmitOrder(new MoveUnitOrder(MyPlayerId, selectionView.SelectedUnit.ID, (HexCoord)hex));
                    else if (tarUnit != null)
                        SubmitOrder(new AttackUnitOrder(MyPlayerId, selectionView.SelectedUnit.ID, tarUnit.ID));
                    else if (tarCity != null)
                        SubmitOrder(new AttackCityOrder(MyPlayerId, selectionView.SelectedUnit.ID, tarCity.ID));
                }
            }
        }

        //================== Ai相关 ===================
        /// <summary>
        /// 处理Ai决策
        /// </summary>
        private void HandleAiOrders()
        {
            ai.BeginAiPhase();
            StartCoroutine(AiOrders());
        }
        IEnumerator AiOrders()
        {
            BaseOrder order;
            WaitUntil wu = new WaitUntil(() => !isBlockingInput);
            int i = 1;
            while (true)
            {
                if (i++ >= 100)
                {
                    TryEndPhase();
                    break;
                }
                order = ai.DecideOrders();
                if (order == null)
                {
                    TryEndPhase();
                    break;
                }

                yield return null;
            }
        }

        //================== 网络交互相关 ===================
        #region 一、非主机客户端侧
        /// <summary>
        /// 提交Order：主机直接本地执行；非主机将order翻译成msg发送给服务端
        /// </summary>
        /// <param name="order"></param>
        private void SubmitOrder(BaseOrder order)
        {
            //先本地判断是否持有输入权限
            if (order.PlayerId != state.CurrentPlayer || isBlockingInput)
            {
                UIManager.Instance.GetPanel<HUD>().UpdateTips("非当前玩家命令");
                return;
            }

            NetworkMgr.Instance.SendOrder(order);
        }
        /// <summary>
        /// order操作被拒绝，返回Tips
        /// </summary>
        /// <param name="msg"></param>
        public void ApplyTip(TipMsg msg)
        {
            UIManager.Instance.GetPanel<HUD>().UpdateTips(msg.Tip);
        }
        /// <summary>
        /// order操作成功，同步返回的世界状态
        /// </summary>
        /// <param name="msg"></param>
        public void ApplySnapShot(GameStateDeltaMsg msg)
        {
            //更新GameState
            AppliedDelta appliedDelta = state.ApplySnapshot(msg);
            //更新回合数和当前玩家
            UIManager.Instance.GetPanel<HUD>().UpdateHUD(gameInfo.GetPlayerInfo(state.CurrentPlayer).Name, state.TurnNumber);
            //更新View和其它UI
            //单位
            foreach (var unit in appliedDelta.AddedUnits)
                unitView.BuildUnit(unit);
            foreach (var unit in appliedDelta.UpdatedUnits)
                unitView.UpdateUnit(unit);
            foreach (var unit in appliedDelta.RemovedUnits)
                unitView.DestroyUnit(unit);
            //城市
            foreach (var city in appliedDelta.AddedCities)
                cityView.BuildCity(city);
            foreach (var city in appliedDelta.UpdatedCities)
                cityView.UpadateCity(city);
            //玩家：主要是UI
            if (state.CurrentPlayer == MyPlayerId)
                phase = GamePhase.PlayerTurn;
            else
                phase = GamePhase.OtherPhase;
        }
        #endregion

        #region 三、主机服务端侧
        public enum ExecuteResultType { Tip, GameStateDelta }
        public struct ExecuteResult
        {
            public ExecuteResultType Type;
            public TipMsg Tip;
            public GameStateDeltaMsg GameStateDelta;
        }
        /// <summary>
        /// 执行order：接收来自自身或其他客户端的order，分发给GameState执行
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public ExecuteResult ExecuteOrder(BaseOrder order)
        {
            switch (order)
            {
                case MoveUnitOrder o:
                    return TryMoveUnit(o.UnitID, o.Target);
                case AttackUnitOrder o:
                    return TryAttackUnit(o.AttackerID, o.DefenderID);
                case AttackCityOrder o:
                    return TryAttackCity(o.AttackerID, o.CityID);
                case FoundCityOrder o:
                    return TryFoundCity(o.UnitID);
                case BuildUnitOrder o:
                    return TryBuildUnit(o.CityID, o.Type);
                case EndPhaseOrder o:
                    return TryEndPhase();
                default:
                    Debug.LogError($"未知命令类型：{order.GetType().Name}");
                    return default;
            }
        }
        private ExecuteResult TryEndPhase()
        {
            state.EndPhase();

            //更新回合数和当前玩家
            UIManager.Instance.GetPanel<HUD>().UpdateHUD(gameInfo.GetPlayerInfo(state.CurrentPlayer).Name, state.TurnNumber);

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameStateDelta,
                GameStateDelta = new GameStateDeltaMsg
                {
                    turnNumber = state.TurnNumber,
                    curPlayer = state.CurrentPlayer,
                    UnitDatas = new List<UnitData> { },
                    CityDatas = new List<CityData> { },
                    PlayerDatas = new List<PlayerData> { }
                }
            };

            //if (NetworkServer.active && session.GetControllerType(state.CurrentPlayer) == ControllerType.AI) 
            //{ 
            //    phase = GamePhase.AiPhase;
            //    HandleAiOrders();
            //}
            //else
            //    phase = GamePhase.PlayerTurn;
        }
        private ExecuteResult TryMoveUnit(int unitId, HexCoord tarHex)
        {
            Unit unit = state.TryGetUnit(unitId);
            if (unit == null)
                return new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = "操作失败" } };

            MoveResult result = state.MoveUnit(unit, tarHex);
            if (!result.Success)
            {
                return new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = "操作失败" } };
            }

            //更新表现层
            unitView.MoveUnit(unit, result.Path);

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameStateDelta,
                GameStateDelta = new GameStateDeltaMsg
                {
                    turnNumber = state.TurnNumber,
                    curPlayer = state.CurrentPlayer,
                    UnitDatas = new List<UnitData> { Unit2Data(unit) },
                    CityDatas = new List<CityData> { },
                    PlayerDatas = new List<PlayerData> { }
                }
            };
        }
        private ExecuteResult TryFoundCity(int unitID)
        {
            Unit unit = state.TryGetUnit(unitID);
            if (unit == null)
                return new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = "操作失败" } };
            FoundCityResult result = state.FoundCity(unit);
            if (!result.Success)
                return new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = "操作失败" } };

            //更新表现层
            unitView.DestroyUnit(unit);
            cityView.BuildCity(result.City);

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameStateDelta,
                GameStateDelta = new GameStateDeltaMsg
                {
                    turnNumber = state.TurnNumber,
                    curPlayer = state.CurrentPlayer,
                    UnitDatas = new List<UnitData> { Unit2Data(unit) },
                    CityDatas = new List<CityData> { City2Data(result.City) },
                    PlayerDatas = new List<PlayerData> { }
                }
            };
        }
        private ExecuteResult TryBuildUnit(int cityID, UnitType type)
        {
            City city = state.TryGetCity(cityID);
            if (city == null)
                return new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = "操作失败" } };

            BuildUnitResult result = state.BuildUnit(city, type);
            if (!result.Success)
                return new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = "操作失败" } };

            //更新表现层
            unitView.BuildUnit(result.Unit);

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameStateDelta,
                GameStateDelta = new GameStateDeltaMsg
                {
                    turnNumber = state.TurnNumber,
                    curPlayer = state.CurrentPlayer,
                    UnitDatas = new List<UnitData> { Unit2Data(result.Unit) },
                    CityDatas = new List<CityData> { City2Data(city) },
                    PlayerDatas = new List<PlayerData> { }
                }
            };
        }
        private ExecuteResult TryAttackUnit(int attackerID, int defenderID)
        {
            Unit attacker = state.TryGetUnit(attackerID);
            Unit defender = state.TryGetUnit(defenderID);
            if (attacker == null || defender == null)
                return new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = "操作失败" } };

            AttackUnitResult result = state.AttackUnit(attacker, defender);
            if (!result.Success)
                return new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = "操作失败" } };

            //更新表现层


            return new ExecuteResult
            {
                Type = ExecuteResultType.GameStateDelta,
                GameStateDelta = new GameStateDeltaMsg
                {
                    turnNumber = state.TurnNumber,
                    curPlayer = state.CurrentPlayer,
                    UnitDatas = new List<UnitData> { Unit2Data(attacker), Unit2Data(defender) },
                    CityDatas = new List<CityData> { },
                    PlayerDatas = new List<PlayerData> { }
                }
            };

        }
        private ExecuteResult TryAttackCity(int attackerID, int cityID)
        {
            Unit attacker = state.TryGetUnit(attackerID);
            City city = state.TryGetCity(cityID);
            if (attacker == null || city == null)
                return new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = "操作失败" } };

            AttackCityResult result = state.AttackCity(attacker, city);
            if (!result.Success)
                return new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = "操作失败" } };

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameStateDelta,
                GameStateDelta = new GameStateDeltaMsg
                {
                    turnNumber = state.TurnNumber,
                    curPlayer = state.CurrentPlayer,
                    UnitDatas = new List<UnitData> { Unit2Data(attacker) },
                    CityDatas = new List<CityData> { City2Data(city) },
                    PlayerDatas = new List<PlayerData> { }
                }
            };

        }
        private UnitData Unit2Data(Unit unit) => new UnitData
        {
            Id = unit.ID,
            Owner = unit.Owner,
            Type = unit.Type,
            Position = unit.Position,
            Hp = unit.Hp,
            MovementLeft = unit.MovementLeft,
            IsDead = unit.IsDead
        };
        private CityData City2Data(City city) => new CityData
        {
            Id = city.ID,
            Owner = city.Owner,
            Position = city.Position,
            Production = city.Production,
            Hp = city.Hp,
        };
        #endregion

        //=============== UI层接口方法 ================
        public void RequestBuildUnit(int cityId, UnitType unitType)
        {
            SubmitOrder(new BuildUnitOrder(MyPlayerId, cityId, unitType));
        }

        public void RequestFoundCity(int unitId)
        {
            SubmitOrder(new FoundCityOrder(MyPlayerId, unitId));
        }

        public void RequestEndPhase()
        {
            SubmitOrder(new EndPhaseOrder(MyPlayerId));
        }

    }
}
