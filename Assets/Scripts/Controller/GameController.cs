using Mirror;
using SparkAge.Config;
using SparkAge.Controller.Ai;
using SparkAge.Controller.Network;
using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Ai;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Orders;
using SparkAge.Model.Players;
using SparkAge.Model.Units;
using SparkAge.View;
using SparkAge.View.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SparkAge.Controller.GameController;
using static SparkAge.Framework.EventCenter.EventDefine;
using static SparkAge.Model.GameState;
using static UnityEngine.Rendering.DebugUI;

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
        public void ApplyGameUpdate(GameUpdateMsg msg);
        public void InitClientWorldState(GameUpdateMsg msg);
    }
    public enum GamePhase
    {
        PlayerTurn,     //等待玩家输入
        OtherPhase,     //其他玩家操作中
        Spectator,      //观战模式
        WaitingServer,  //等待服务器消息
        Animating,      //动画中
        GameOver
    }
    /// <summary>
    /// 游戏控制层
    /// </summary>
    public class GameController : MonoBehaviour, IUIInput, INetworkInput
    {
        [SerializeField] float hexSize = 1f;                //单位大小
        [SerializeField] CameraController CameraController; //相机控制器

        //控制层引用
        GameSession session;
        AiDriver aiDriver;
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
        bool isAiTurn = false;

        //游戏结束相关
        bool pendingGameOver;
        int pendingWinnerId;
        bool pendingSpectator;

        int MyPlayerId => NetworkMgr.Instance.MyPlayerId;
        bool IsMyTurn => NetworkMgr.Instance.MyPlayerId == state.CurrentPlayer;
        bool IsMine(int own) => MyPlayerId == own;
        string curPlayerName => gameInfo.GetPlayerInfo(state.CurrentPlayer).Name;
        string curCharacterName => gameInfo.GetPlayerInfo(state.CurrentPlayer).CharacterInfo.Name;
        Unit selectedUnit => selectionView.SelectedUnit;
        City selectedCity => selectionView.SelectedCity;

        private void Awake()
        {
            //配置装配
            BuildGameInfo();
            //构建项目结构
            BuildModelAndView();
            //为网络层和UI层注入接口
            RegisterRuntimeEndpoints();

            //音乐控制
            AudioMgr.Instance.SetBGM(Resources.Load<AudioClip>("Audio/GameSceneMusic"));
        }
        private void Start()
        {
            InitView();
            AddEventListener();
            if (NetworkServer.active)
                InitHostWorldState();
            NetworkMgr.Instance.SendGameReady();
        }
        private void Update()
        {
            switch (phase)
            {
                case GamePhase.PlayerTurn:
                    HandlePlayerInput();
                    break;
                case GamePhase.OtherPhase:
                case GamePhase.Spectator:
                case GamePhase.GameOver:
                case GamePhase.WaitingServer:
                case GamePhase.Animating:
                    return;
            }
        }
        private void OnDestroy()
        {
            UnregisterRuntimeEndpoints();
        }

        //================ 初始化相关 ==================
        private void BuildGameInfo()
        {
            //装配游戏房间配置（玩家选择角色和地图）
            gameInfo = new GameInfo();
            gameInfo.MapInfo = ConfigMgr.Instance.StaticInfo.MapInfos[NetworkMgr.Instance.MapId];
            foreach(var slot in NetworkMgr.Instance.Slots)
            {
                gameInfo.PlayerInfos.Add(new PlayerInfo
                {
                    Id = slot.PlayerId,
                    Name = slot.Name,
                    CharacterInfo = ConfigMgr.Instance.StaticInfo.CharacterInfos[slot.CharacterId],
                    IsAi = slot.isAI
                });
            }
        }
        private void BuildModelAndView()
        {
            state = new GameState(gameInfo, ConfigMgr.Instance.StaticInfo);
            state.Init();

            session = new GameSession();
            session.Init(NetworkMgr.Instance.Slots);

            aiDriver = new AiDriver(state, session, new AiDecider(state));

            mapView = gameObject.AddComponent<MapView>();
            mapView.Init(state, hexSize);

            unitView = gameObject.AddComponent<UnitView>();
            unitView.Init(state, hexSize);

            selectionView = gameObject.AddComponent<SelectionView>();
            selectionView.Init(state, hexSize);

            cityView = gameObject.AddComponent<CityView>();
            cityView.Init(state, hexSize);

            gameObject.GetComponent<HealthBarMgr>().Init(state, unitView, cityView, Camera.main, UIManager.Instance.CanvasRect);
        }
        private void RegisterRuntimeEndpoints()
        {
            NetworkMgr.Instance.SetNetworkInput(this);
            UIManager.Instance.SetUIInput(this);
        }

        private void InitView()
        {
            //构建地图
            mapView.BuildTiles();

            //显示HUD
            var panel = UIManager.Instance.ShowPanel<HUD>();
            panel.InitMyInfo(gameInfo.GetPlayerInfo(MyPlayerId));

            //初始化摄像机脚本
            (HexCoord, HexCoord, HexCoord) keyPos = state.GetMapKeyPos();
            CameraController.Init(keyPos.Item1, keyPos.Item2, keyPos.Item3);
        }
        private void InitHostWorldState()
        {
            state.CreateInitialUnits();

            phase = GamePhase.PlayerTurn;
            UIManager.Instance.ShowPanel<HUD>().UpdateHUD(curPlayerName, curCharacterName, state.TurnNumber);

            List<Unit> initUnits = new();
            List<UnitData> initDeltaUnits = new List<UnitData>();
            List<int> initHintUnits = new List<int>();
            foreach (var unit in state.AllUnits)
            {
                initUnits.Add(unit);
                //获取初始世界状态并打包进msg
                initDeltaUnits.Add(new UnitData
                {
                    Id = unit.ID,
                    Owner = unit.Owner,
                    Type = unit.Type,
                    Position = unit.Position,
                    Hp = unit.Hp,
                    MovementLeft = unit.MovementLeft,
                    IsDead = unit.IsDead
                });
                initHintUnits.Add(unit.ID);
            }

            //更新本地表现层
            EventCenter.Instance.EventTrigger<InitialSettlers>(new InitialSettlers(initUnits));

            //打包网络层数据
            GameUpdateMsg msg = new GameUpdateMsg
            {
                Delta = new GameStateDeltaMsg
                {
                    turnNumber = state.TurnNumber,
                    curPlayer = state.CurrentPlayer,
                    UnitDatas = initDeltaUnits
                },
                Hint = new HintMsg
                {
                    Type = HintType.InitialGameUpdate,
                    InitialUnits = initHintUnits
                }
            };
            NetworkMgr.Instance.SetGameInitMsg(msg);
        }
        private void AddEventListener()
        {
            EventCenter.Instance.AddListener<MoveUnitCompletedEvent>(e => 
            {
                if (!IsMyTurn) return;
                RecoverPhase();
            });
            EventCenter.Instance.AddListener<AttackUnitCompletedEvent>(e =>
            {
                if (!IsMyTurn) return;
                RecoverPhase();
                UIManager.Instance.GetPanel<SelCityPanel>().HideMe();
                if (e.Attacker.IsDead)
                {
                    UIManager.Instance.GetPanel<SelUnitPanel>().HideMe();
                }
                else
                {
                    UIManager.Instance.ShowPanel<SelUnitPanel>().UpdatePanel(e.Attacker, gameInfo.GetPlayerCharacterName(e.Attacker.Owner));
                }
            });
            EventCenter.Instance.AddListener<AttackCityCompletedEvent>(e =>
            {
                if (pendingGameOver)
                {
                    pendingGameOver = false;
                    pendingSpectator = false;

                    phase = GamePhase.GameOver;

                    UIManager.Instance.GetPanel<SelCityPanel>().HideMe();
                    UIManager.Instance.GetPanel<SelUnitPanel>().HideMe();

                    UIManager.Instance.ShowPanel<GameOverPanel>().SetGameRes(pendingWinnerId == MyPlayerId);

                    return;
                }

                if (pendingSpectator)
                {
                    pendingSpectator = false;

                    phase = GamePhase.Spectator;

                    UIManager.Instance.GetPanel<SelCityPanel>().HideMe();
                    UIManager.Instance.GetPanel<SelUnitPanel>().HideMe();
                    UIManager.Instance.ShowPanel<SpectatorPanel>();

                    return;
                }

                if (!IsMyTurn)
                    return;

                RecoverPhase();

                UIManager.Instance.GetPanel<SelCityPanel>().HideMe();

                if (e.Attacker.IsDead)
                    UIManager.Instance.GetPanel<SelUnitPanel>().HideMe();
                else
                    UIManager.Instance.ShowPanel<SelUnitPanel>().UpdatePanel(e.Attacker, gameInfo.GetPlayerCharacterName(e.Attacker.Owner));
            });
        }
        public void InitClientWorldState(GameUpdateMsg msg)
        {
            ApplyGameUpdate(msg);

            UIManager.Instance.ShowPanel<HUD>().UpdateHUD(curPlayerName, curCharacterName, state.TurnNumber);
        }

        private void UnregisterRuntimeEndpoints()
        {
            if (NetworkMgr.Instance != null)
                NetworkMgr.Instance.SetNetworkInput(null);

            if (UIManager.Instance != null)
                UIManager.Instance.SetUIInput(null);
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
            //输入锁定：动画锁定+UI锁定
            if (phase == GamePhase.Animating || UIManager.Instance.IsPointerOverUI)
                return;

            //============= 键盘输入 ==============
            //回合结束
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SubmitOrder(new EndPhaseOrder(MyPlayerId));
            }
            //F键建城
            if (Input.GetKeyDown(KeyCode.F) && selectedUnit != null && selectedUnit.Type == UnitType.Settler)
            {
                SubmitOrder(new FoundCityOrder(MyPlayerId, selectedUnit.ID));
            }
            //1 2键造兵
            if (selectedCity != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    SubmitOrder(new BuildUnitOrder(MyPlayerId, selectedCity.ID, UnitType.Settler));
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    SubmitOrder(new BuildUnitOrder(MyPlayerId, selectedCity.ID, UnitType.Warrior));
                }
            }

            //============= 鼠标输入 ==============
            //鼠标左键点击
            if (Input.GetMouseButtonDown(0))
            {
                //高亮
                selectionView.HandleClick(GetClickHex());
                //UI显示
                if(selectedUnit != null)
                {
                    var panel = UIManager.Instance.ShowPanel<SelUnitPanel>();
                    panel.UpdatePanel(selectedUnit, gameInfo.GetPlayerCharacterName(selectedUnit.Owner));
                }
                else
                    UIManager.Instance.HidePanel<SelUnitPanel>();

                if (selectedCity != null && IsMine(selectedCity.Owner))
                {
                    var panel = UIManager.Instance.ShowPanel<SelCityPanel>();
                    panel.UpdatePanel(selectedCity);
                }
                else
                    UIManager.Instance.HidePanel<SelCityPanel>();
            }
            //鼠标右键点击
            if (Input.GetMouseButtonDown(1) && selectedUnit != null)
            {
                HexCoord? hex = GetClickHex();
                if (hex != null)
                {
                    Unit tarUnit = state.GetUnitAt((HexCoord)hex);
                    City tarCity = state.GetCityAt((HexCoord)hex);
                    if (tarUnit == null && (tarCity == null || IsMine(tarCity.Owner)))
                        SubmitOrder(new MoveUnitOrder(MyPlayerId, selectedUnit.ID, (HexCoord)hex));
                    else if (tarUnit != null)
                        SubmitOrder(new AttackUnitOrder(MyPlayerId, selectedUnit.ID, tarUnit.ID));
                    else if (tarCity != null)
                        SubmitOrder(new AttackCityOrder(MyPlayerId, selectedUnit.ID, tarCity.ID));
                }
            }
        }
        /// <summary>
        /// 结束等待消息和动画
        /// </summary>
        private void RecoverPhase()
        {
            if (state.IsGameOver)
            {
                phase = GamePhase.GameOver;
                return;
            }

            if (!state.IsPlayerAlive(MyPlayerId))
            {
                phase = GamePhase.Spectator;
                return;
            }

            if (state.CurrentPlayer == MyPlayerId)
                phase = GamePhase.PlayerTurn;
            else
                phase = GamePhase.OtherPhase;
        }
        private void SetPendingEndState(GameUpdateMsg msg)
        {
            if (msg.IsGameOver)
            {
                pendingGameOver = true;
                pendingWinnerId = msg.WinnerId;
                pendingSpectator = false;
            }
            else if (!state.IsPlayerAlive(MyPlayerId))
            {
                pendingSpectator = true;
            }
        }

        //================== Ai相关 ===================
        /// <summary>
        /// 处理Ai决策
        /// </summary>
        private void TryStartAi()
        {
            if (isAiTurn) return;

            if (!NetworkServer.active) return;

            if (session.GetControllerType(state.CurrentPlayer) != ControllerType.AI) return;

            isAiTurn = true;
            StartCoroutine(RunAiSequence());
        }
        private IEnumerator RunAiSequence()
        {
            yield return null;

            yield return StartCoroutine(aiDriver.Run(state.CurrentPlayer));

            isAiTurn = false;
            RecoverPhase();
        }
        //================== 网络交互相关 ===================
        #region 一、非主机客户端侧

        /// <summary>
        /// 提交Order：主机直接本地执行；非主机将order翻译成msg发送给服务端
        /// </summary>
        /// <param name="order"></param>
        private void SubmitOrder(BaseOrder order)
        {
            if (state.IsGameOver || !state.IsPlayerAlive(MyPlayerId) ||  phase == GamePhase.Spectator || phase == GamePhase.GameOver)
            {
                return;
            }

            if (!NetworkServer.active)
                phase = GamePhase.WaitingServer;
            NetworkMgr.Instance.SendOrder(order);
        }
        /// <summary>
        /// order操作被拒绝，返回Tips
        /// </summary>
        /// <param name="msg"></param>
        public void ApplyTip(TipMsg msg)
        {
            UIManager.Instance.GetPanel<HUD>().UpdateTips(msg.Tip);

            RecoverPhase();
        }
        /// <summary>
        /// order操作成功，同步返回的世界状态
        /// </summary>
        /// <param name="msg"></param>
        public void ApplyGameUpdate(GameUpdateMsg msg)
        {
            ApplyGameStateDelta(msg.Delta);
            RecoverPhase();
            ApplyHint(msg.Hint);

            SetPendingEndState(msg);
        }
        private void ApplyGameStateDelta(GameStateDeltaMsg msg)
        {
            //更新GameState
            state.ApplyGameDelta(msg);
        }
        private void ApplyHint(HintMsg msg)
        {
            UnitHintData unitData = msg.UnitHintData;
            CityHintData cityData = msg.CityHintData;
            Unit unit, attacker, defender;
            City city;
            switch (msg.Type)
            {
                case HintType.NoHint:
                    return;
                case HintType.InitialGameUpdate:
                    List<Unit> settlers = new();
                    foreach(var id in msg.InitialUnits)
                    {
                        unit = state.TryGetUnit(id);
                        settlers.Add(unit);
                    }
                    EventCenter.Instance.EventTrigger<InitialSettlers>(new(settlers));
                    break;
                case HintType.BuildUnit:
                    unit = state.TryGetUnit(cityData.UnitId);
                    EventCenter.Instance.EventTrigger<BuildUnitEvent>(new(unit));
                    break;
                case HintType.FoundCity:
                    unit = state.TryGetDeadUnit(unitData.UnitId);
                    city = state.TryGetCity(unitData.TargetCityId);
                    EventCenter.Instance.EventTrigger<RemoveUnitEvent>(new(unit));
                    EventCenter.Instance.EventTrigger<BuildCityEvent>(new(city));
                    break;
                case HintType.MoveUnit:
                    unit = state.TryGetUnit(unitData.UnitId);
                    if (IsMyTurn)
                        phase = GamePhase.Animating;
                    EventCenter.Instance.EventTrigger<MoveUnitStartEvent>(new(unit, unitData.Path));
                    break;
                case HintType.AttackUnit:
                    if (unitData.AttackerIsDead)
                        attacker = state.TryGetDeadUnit(unitData.UnitId);
                    else
                        attacker = state.TryGetUnit(unitData.UnitId);
                    if (unitData.DefenderIsDead)
                        defender = state.TryGetDeadUnit(unitData.TargetUnitId);
                    else
                        defender = state.TryGetUnit(unitData.TargetUnitId);
                    if (IsMyTurn)
                        phase = GamePhase.Animating;
                    EventCenter.Instance.EventTrigger<AttackUnitStartEvent>(new(attacker, defender, unitData.Path, unitData.CanEnter));
                    break;
                case HintType.AttackCity:
                    {
                        attacker = state.TryGetUnit(unitData.UnitId);
                        city = state.TryGetCity(unitData.TargetCityId);

                        List<Unit> defeatedUnits = new List<Unit>();
                        if (unitData.DeadUnitIds != null)
                        {
                            foreach (int unitId in unitData.DeadUnitIds)
                            {
                                Unit deadUnit = state.TryGetDeadUnit(unitId);
                                if (deadUnit != null)
                                    defeatedUnits.Add(deadUnit);
                            }
                        }
                        if (IsMyTurn)
                            phase = GamePhase.Animating;
                        EventCenter.Instance.EventTrigger<AttackCityStartEvent>(new(attacker, city, unitData.Path, unitData.CityIsCaptured, defeatedUnits));
                    }
                    break;
                case HintType.EndPhase:
                    EventCenter.Instance.EventTrigger<SelectionClearEvent>(new());
                    UIManager.Instance.ShowPanel<HUD>().UpdateHUD(curPlayerName, curCharacterName, state.TurnNumber);
                    break;
            }
        }
        #endregion

        #region 二、主机端侧
        public enum ExecuteResultType { Tip, GameUpdate }
        public struct ExecuteResult
        {
            public ExecuteResultType Type;
            public TipMsg Tip;
            public GameUpdateMsg GameUpdateMsg;
        }
        /// <summary>
        /// 执行order：接收来自自身或其他客户端的order，分发给GameState执行
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public ExecuteResult ExecuteOrder(BaseOrder order)
        {
            if (state.IsGameOver)
                return Fail("游戏已结束");

            if (!state.IsPlayerAlive(order.PlayerId) || order.PlayerId != state.CurrentPlayer)
            {
                return Fail("当前无法操作");
            }

            ExecuteResult result = new ExecuteResult();

            switch (order)
            {
                case MoveUnitOrder o:
                    result = TryMoveUnit(o.UnitID, o.Target);
                    break;
                case AttackUnitOrder o:
                    result = TryAttackUnit(o.AttackerID, o.DefenderID);
                    break;
                case AttackCityOrder o:
                    result = TryAttackCity(o.AttackerID, o.CityID);
                    break;
                case FoundCityOrder o:
                    result = TryFoundCity(o.UnitID);
                    break;
                case BuildUnitOrder o:
                    result = TryBuildUnit(o.CityID, o.Type);
                    break;
                case EndPhaseOrder o:
                    result = TryEndPhase();
                    break;
            }

            if (result.Type == ExecuteResultType.GameUpdate)
            {
                result.GameUpdateMsg.IsGameOver = state.IsGameOver;
                result.GameUpdateMsg.WinnerId = state.WinnerId;

                SetPendingEndState(result.GameUpdateMsg);
            }

            return result;
        }
        private ExecuteResult TryEndPhase()
        {
            state.EndPhase();

            //更新回合数和当前玩家
            EventCenter.Instance.EventTrigger<SelectionClearEvent>(new());
            UIManager.Instance.ShowPanel<HUD>().UpdateHUD(curPlayerName, curCharacterName, state.TurnNumber);

            TryStartAi();

            //全量更新
            List<UnitData> unitDatas = new List<UnitData>();
            foreach (var unit in state.AllUnits)
                unitDatas.Add(Unit2Data(unit));
            List<CityData> cityDatas = new List<CityData>();
            foreach (var city in state.AllCities)
                cityDatas.Add(City2Data(city));
            List<PlayerData> playerDatas = new List<PlayerData>();
            foreach (var player in state.AllPlayers)
                playerDatas.Add(Player2Data(player));

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameUpdate,
                GameUpdateMsg = new GameUpdateMsg
                {
                    Delta = new GameStateDeltaMsg
                    {
                        turnNumber = state.TurnNumber,
                        curPlayer = state.CurrentPlayer,
                        UnitDatas = unitDatas,
                        CityDatas = cityDatas,
                        PlayerDatas = playerDatas
                    },
                    Hint = new HintMsg
                    {
                        Type = HintType.EndPhase
                    }
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
                return Fail("移动失败：单位不存在");

            MoveResult result = state.MoveUnit(unit, tarHex);
            if (!result.Success)
            {
                return Fail(GetMoveFailTip(result.Reason));
            }

            //更新表现层
            if (IsMyTurn)
                phase = GamePhase.Animating;
            EventCenter.Instance.EventTrigger<MoveUnitStartEvent>(new(unit, result.Path));

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameUpdate,
                GameUpdateMsg = new GameUpdateMsg
                {
                    Delta = new GameStateDeltaMsg
                    {
                        turnNumber = state.TurnNumber,
                        curPlayer = state.CurrentPlayer,
                        UnitDatas = new List<UnitData> { Unit2Data(unit) }
                    },
                    Hint = new HintMsg
                    {
                        Type = HintType.MoveUnit,
                        UnitHintData = new UnitHintData
                        {
                            UnitId = unitId,
                            Path = result.Path
                        }
                    }
                }
            };
        }
        private ExecuteResult TryFoundCity(int unitID)
        {
            Unit unit = state.TryGetUnit(unitID);
            if (unit == null)
                return Fail("建城失败：单位不存在");
            FoundCityResult result = state.FoundCity(unit);
            if (!result.Success)
                return Fail(GetFoundCityFailTip(result.Reason));

            //更新表现层
            EventCenter.Instance.EventTrigger<RemoveUnitEvent>(new(unit));
            EventCenter.Instance.EventTrigger<BuildCityEvent>(new(result.City));

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameUpdate,
                GameUpdateMsg = new GameUpdateMsg
                {
                    Delta = new GameStateDeltaMsg
                    {
                        turnNumber = state.TurnNumber,
                        curPlayer = state.CurrentPlayer,
                        UnitDatas = new List<UnitData> { Unit2Data(unit) },
                        CityDatas = new List<CityData> { City2Data(result.City) }
                    },
                    Hint = new HintMsg
                    {
                        Type = HintType.FoundCity,
                        UnitHintData = new UnitHintData
                        {
                            UnitId = unit.ID,
                            TargetCityId = result.City.ID
                        }
                    }
                }
            };
        }
        private ExecuteResult TryBuildUnit(int cityID, UnitType type)
        {
            City city = state.TryGetCity(cityID);
            if (city == null)
                return Fail("建造失败：城市不存在");

            BuildUnitResult result = state.BuildUnit(city, type);
            if (!result.Success)
                return Fail(GetBuildUnitFailTip(result.Reason));

            //更新表现层
            EventCenter.Instance.EventTrigger<BuildUnitEvent>(new(result.Unit));

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameUpdate,
                GameUpdateMsg = new GameUpdateMsg
                {
                    Delta = new GameStateDeltaMsg
                    {
                        turnNumber = state.TurnNumber,
                        curPlayer = state.CurrentPlayer,
                        UnitDatas = new List<UnitData> { Unit2Data(result.Unit) },
                        CityDatas = new List<CityData> { City2Data(city) }
                    },
                    Hint = new HintMsg
                    {
                        Type = HintType.BuildUnit,
                        CityHintData = new CityHintData
                        {
                            CityId = city.ID,
                            UnitId = result.Unit.ID
                        }
                    }
                }
            };
        }
        private ExecuteResult TryAttackUnit(int attackerID, int defenderID)
        {
            Unit attacker = state.TryGetUnit(attackerID);
            Unit defender = state.TryGetUnit(defenderID);
            if (attacker == null || defender == null)
                return Fail("攻击失败：攻击单位或目标单位不存在");

            AttackUnitResult result = state.AttackUnit(attacker, defender);
            if (!result.Success)
                return Fail(GetAttackUnitFailTip(result.Reason));

            //更新表现层
            if (IsMyTurn)
                phase = GamePhase.Animating;
            EventCenter.Instance.EventTrigger<AttackUnitStartEvent>(new(attacker, defender, result.Path, result.CanEnter));

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameUpdate,
                GameUpdateMsg = new GameUpdateMsg
                {
                    Delta = new GameStateDeltaMsg
                    {
                        turnNumber = state.TurnNumber,
                        curPlayer = state.CurrentPlayer,
                        UnitDatas = new List<UnitData> { Unit2Data(attacker), Unit2Data(defender) }
                    },
                    Hint = new HintMsg
                    {
                        Type = HintType.AttackUnit,
                        UnitHintData = new UnitHintData
                        {
                            UnitId = attackerID,
                            Path = result.Path,
                            TargetUnitId = defenderID,
                            CanEnter = result.CanEnter,
                            AttackerIsDead = attacker.IsDead,
                            DefenderIsDead = defender.IsDead
                        }
                    }
                }
            };

        }
        private ExecuteResult TryAttackCity(int attackerID, int cityID)
        {
            Unit attacker = state.TryGetUnit(attackerID);
            City city = state.TryGetCity(cityID);
            if (attacker == null || city == null)
                return Fail("攻击失败：攻击单位或目标城市不存在");

            AttackCityResult result = state.AttackCity(attacker, city);
            if (!result.Success)
                return Fail(GetAttackCityFailTip(result.Reason));

            //更新表现层
            if (IsMyTurn)
                phase = GamePhase.Animating;
            EventCenter.Instance.EventTrigger<AttackCityStartEvent>(new(attacker, city, result.Path, result.CityIsCaptured, result.DefenderUnits));

            //构建上传消息
            List<UnitData> unitDatas = new List<UnitData>();
            unitDatas.Add(Unit2Data(attacker));

            List<int> deadUnitIds = new List<int>();

            foreach (var unit in result.DefenderUnits)
            {
                unitDatas.Add(Unit2Data(unit));
                deadUnitIds.Add(unit.ID);
            }

            List<PlayerData> playerDatas = new List<PlayerData>();
            foreach (var player in state.AllPlayers)
            {
                playerDatas.Add(Player2Data(player));
            }

            return new ExecuteResult
            {
                Type = ExecuteResultType.GameUpdate,
                GameUpdateMsg = new GameUpdateMsg
                {
                    Delta = new GameStateDeltaMsg
                    {
                        turnNumber = state.TurnNumber,
                        curPlayer = state.CurrentPlayer,
                        UnitDatas = unitDatas,
                        CityDatas = new List<CityData> { City2Data(city) },
                        PlayerDatas = playerDatas
                    },
                    Hint = new HintMsg
                    {
                        Type = HintType.AttackCity,
                        UnitHintData = new UnitHintData
                        {
                            UnitId = attackerID,
                            Path = result.Path,
                            TargetCityId = cityID,
                            CityIsCaptured = result.CityIsCaptured,
                            DeadUnitIds = deadUnitIds
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 工具类方法
        /// </summary>
        private static ExecuteResult Fail(string tip)
            => new ExecuteResult { Type = ExecuteResultType.Tip, Tip = new TipMsg { Tip = tip } };
        private static string GetMoveFailTip(MoveFailReason reason) => reason switch
        {
            MoveFailReason.WrongUnitID => "移动失败：单位不存在",
            MoveFailReason.NoAccess => "移动失败：不是当前玩家的单位",
            MoveFailReason.InvaildPos => "移动失败：目标格无法停留",
            MoveFailReason.Unreachable => "移动失败：移动力不足",
            MoveFailReason.NoPath => "移动失败：无法到达目标",
            _ => "移动失败"
        };
        private static string GetFoundCityFailTip(FoundCityFailReason reason) => reason switch
        {
            FoundCityFailReason.WrongUnitID => "建城失败：单位不存在",
            FoundCityFailReason.NoAccess => "建城失败：不是当前玩家的单位",
            FoundCityFailReason.NotSettler => "建城失败：只有移民可以建城",
            FoundCityFailReason.Unbuildable => "建城失败：该地块不可建城",
            FoundCityFailReason.OccupiedByUnit => "建城失败：该地块已有单位",
            FoundCityFailReason.OccupiedByCity => "建城失败：该地块已属于城市",
            _ => "建城失败"
        };
        private static string GetBuildUnitFailTip(BuildUnitFailReason reason) => reason switch
        {
            BuildUnitFailReason.WrongCityID => "建造失败：城市不存在",
            BuildUnitFailReason.NoAccess => "建造失败：不是当前玩家的城市",
            BuildUnitFailReason.NotEnoughProduction => "建造失败：生产力不足",
            BuildUnitFailReason.NoUnitSpawnNear => "建造失败：城市周围没有可用地块",
            _ => "建造失败"
        };
        private static string GetAttackUnitFailTip(AttackUnitFailReason reason) => reason switch
        {
            AttackUnitFailReason.WrongUnitID => "攻击失败：单位不存在",
            AttackUnitFailReason.NoAccess => "攻击失败：不是当前玩家的单位",
            AttackUnitFailReason.IsSameOwner => "攻击失败：不能攻击己方单位",
            AttackUnitFailReason.IsSettler => "攻击失败：移民不能攻击",
            AttackUnitFailReason.Unreachable => "攻击失败：目标不在攻击范围内",
            _ => "攻击失败"
        };
        private static string GetAttackCityFailTip(AttackCityFailReason reason) => reason switch
        {
            AttackCityFailReason.WrongUnitID => "攻击失败：单位不存在",
            AttackCityFailReason.WrongCityID => "攻击失败：城市不存在",
            AttackCityFailReason.NoAccess => "攻击失败：不是当前玩家的单位",
            AttackCityFailReason.IsSameOwner => "攻击失败：不能攻击己方城市",
            AttackCityFailReason.IsSettler => "攻击失败：移民不能攻击",
            AttackCityFailReason.Unreachable => "攻击失败：目标城市不在攻击范围内",
            _ => "攻击失败"
        };
        private static UnitData Unit2Data(Unit unit) => new UnitData
        {
            Id = unit.ID,
            Owner = unit.Owner,
            Type = unit.Type,
            Position = unit.Position,
            Hp = unit.Hp,
            MovementLeft = unit.MovementLeft,
            IsDead = unit.IsDead
        };
        private static CityData City2Data(City city) => new CityData
        {
            Id = city.ID,
            Owner = city.Owner,
            Name = city.Name,
            Position = city.Position,
            Production = city.Production,
            Hp = city.Hp,
        };
        private static PlayerData Player2Data(PlayerState player) => new PlayerData
        {
            Id = player.ID,
            IsAlive = player.IsAlive
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
