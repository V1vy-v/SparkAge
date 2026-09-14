using SparkAge.Config;
using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.GameInfos;
using SparkAge.Model.Hex;
using SparkAge.Model.Map;
using SparkAge.Model.Orders;
using SparkAge.Model.Units;
using SparkAge.View;
using SparkAge.View.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SparkAge.Framework.EventCenter.EventDefine;
using static SparkAge.Model.GameState;

namespace SparkAge.Controller
{
    public interface IOrderSink
    {
        public int MyPlayerId { get; }
        public bool IsMyTurn { get; }
        public bool IsMine(int own);
        public void RequestMoveUnit(int unitId, HexCoord target);
        public void RequestBuildUnit(int cityId, UnitType unitType);
        public void RequestFoundCity(int unitId);
        public void RequestAttackUnit(int attackerId, int defenderId);
        public void RequestAttackCity(int attackerId, int cityId);
        public void RequestEndPhase();
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
    public class GameController : MonoBehaviour, IOrderSink
    {
        [SerializeField] float hexSize = 1f;//单位大小
        [SerializeField] GameCfg gameCfg;//游戏配置表
        [SerializeField] CameraController CameraController;//相机控制器

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
        public int MyPlayerId => session.MyPlayerId;
        public bool IsMyTurn => session.MyPlayerId == state.CurrentPlayer;
        public bool IsMine(int own) => MyPlayerId == own;

        private void Awake()
        {
            //配置表注入
            InitGameInfo();
            //UI数据端口注入
            UIManager.Instance.SetOrderSink(this);

            state = new GameState(gameInfo);

            ai = new AiOrders();
            ai.Init(state);
            session = new GameSession();
            session.Init(gameCfg.gameSetUpCfg);

            mapView = gameObject.AddComponent<MapView>();
            mapView.Init(state, hexSize);

            unitView = gameObject.AddComponent<UnitView>();
            unitView.Init(state, hexSize, gameCfg.unitCfgs);

            selectionView = gameObject.AddComponent<SelectionView>();
            selectionView.Init(state, hexSize, mapView.HexMesh);

            cityView = gameObject.AddComponent<CityView>();
            cityView.Init(state, hexSize, gameCfg.cityCfgs);
        }
        private void Start()
        {
            //订阅事件
            EventCenter.Instance.AddListener<UnitMoveEvent>(e =>
            {
                isBlockingInput = false;
            });
            EventCenter.Instance.AddListener<AttackUnitEvent>(e =>
            {
                isBlockingInput = false;
            });
            EventCenter.Instance.AddListener<AttackCityEvent>(e =>
            {
                isBlockingInput = false;
                //后续增加为所有其它玩家全失败
                if (e.DefenderIsDead)
                    SubmitOrder(new GameOverOrder(MyPlayerId));
            });

            //构建地图
            mapView.BuildTiles();

            //显示HUD
            UIManager.Instance.ShowPanel<HUD>();

            //初始化摄像机脚本
            (Vector3, Vector3, Vector3) keyPos = mapView.GetMapCenterAndBounds();
            CameraController.Init(keyPos.Item1, keyPos.Item2, keyPos.Item3);

            //初始移民
            foreach(var settler in state.AllUnits)
            {
                GameObject obj = unitView.BuildUnit(settler);
                unitView.UnitObjs[settler] = obj;
            }
        }
        private void Update()
        {
            UIManager.Instance.GetPanel<HUD>().UpdateHUD(0, state.TurnNumber);
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
            UIManager.Instance.SetOrderSink(null);
        }

        /// <summary>
        /// 初始配置表读取与注入
        /// </summary>
        private void InitGameInfo()
        {
            gameInfo = new GameInfo();
            foreach (var cfg in gameCfg.unitCfgs)
            {
                gameInfo.UnitInfos[cfg.Type] = new UnitInfo(cfg.Type, cfg.Name, cfg.Atk, cfg.Def, cfg.Hp, cfg.Movement, cfg.Cost);
            }
            foreach (var cfg in gameCfg.cityCfgs)
            {
                gameInfo.CityInfos.Add(new CityInfo(cfg.Name, cfg.Hp, cfg.Def, cfg.Radius, cfg.Production));
            }
            List<SlotInfo> slots = new List<SlotInfo>();
            foreach (var slot in gameCfg.gameSetUpCfg.Slots)
            {
                slots.Add(new SlotInfo(slot.PlayerId));
            }
            gameInfo.GameSetUpInfo = new GameSetUpInfo(gameCfg.gameSetUpCfg.Seed, gameCfg.gameSetUpCfg.MapWidth, gameCfg.gameSetUpCfg.MapHeight, slots);
        }

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
                if (selectionView.SelectedUnit != null)
                    selectionView.SelectUnit(selectionView.SelectedUnit);
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
                if(selectionView.SelectedUnit != null)
                {
                    var panel = UIManager.Instance.ShowPanel<SelUnitPanel>();
                    panel.UpdatePanel(selectionView.SelectedUnit);
                }
                else
                    UIManager.Instance.HideMe<SelUnitPanel>();
                if (selectionView.SelectedCity != null && selectionView.SelectedCity.Owner == MyPlayerId)
                {
                    var panel = UIManager.Instance.ShowPanel<SelCityPanel>();
                    panel.UpdatePanel(selectionView.SelectedCity);
                }
                else
                    UIManager.Instance.HideMe<SelCityPanel>();
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
            bool needWait;
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

                needWait = SubmitOrder(order);
                if(needWait)
                    yield return wu;
                if (order is EndPhaseOrder)
                    break;
            }
        }

        private void TryEndPhase()
        {
            if (isBlockingInput) return;

            state.EndPhase();
            if (session.IsServer && session.GetControllerType(state.CurrentPlayer) == ControllerType.AI) 
            { 
                phase = GamePhase.AiPhase;
                HandleAiOrders();
            }
            else
                phase = GamePhase.PlayerTurn;
        }
        private bool TryMoveUnit(int unitID, HexCoord tarHex)
        {
            Unit unit = state.TryGetUnit(unitID);
            if (unit == null)
            {
                Debug.Log("非法ID");
                return false;
            }

            MoveResult result = state.MoveUnit(unit, tarHex);
            if (!result.Success)
            {
                switch (result.Reason)
                {
                    case MoveFailReason.InvaildPos:
                        Debug.Log("非法位置");
                        break;
                    case MoveFailReason.Unreachable:
                        Debug.Log("该地块不可到达");
                        break;
                    case MoveFailReason.NoPath:
                        Debug.Log("该地块无可到达路径");
                        break;
                }
                return false;
            }

            isBlockingInput = true;
            //发布单位移动事件
            unitView.MoveUnit(unit, result.Path);
            return true;
        }

        private void TryFoundCity(int unitID)
        {
            Unit unit = state.TryGetUnit(unitID);
            if(unit == null)
            {
                Debug.Log("非法ID");
                return;
            }
            FoundCityResult result = state.FoundCity(unit);
            if (!result.Success)
            {
                switch (result.Reason)
                {
                    case FoundCityFailReason.NotSettler:
                        Debug.Log("当前单位并非移民");
                        break;
                    case FoundCityFailReason.Unbuildable:
                        Debug.Log("该地块不可建城");
                        break;
                    case FoundCityFailReason.OccupiedByUnit:
                        Debug.Log("该地块被单位占据");
                        break;
                    case FoundCityFailReason.OccupiedByCity:
                        Debug.Log("该地块已被城市占据");
                        break;
                    case FoundCityFailReason.Limited:
                        Debug.Log("你的城市数量已达上限");
                        break;
                }
                return;
            }
            //发布建城事件
            EventCenter.Instance.EventTrigger<FoundCityEvent>(new FoundCityEvent(result.City, unit));
        }

        private void TryBuildUnit(int cityID, UnitType type)
        {
            City city = state.TryGetCity(cityID);
            if(city == null)
            {
                Debug.Log("非法ID");
                return;
            }

            BuildUnitResult result = state.BuildUnit(city, type);
            if (!result.Success)
            {
                switch (result.Reason)
                {
                    case BuildUnitFailReason.NotEnoughProduction:
                        Debug.Log("生产力不足");
                        break;
                    case BuildUnitFailReason.NoUnitSpawnNear:
                        Debug.Log("无可用单位出生点");
                        break;
                }
                return;
            }

            //表现层
            //发布造兵事件
            EventCenter.Instance.EventTrigger<BuildUnitEvent>(new BuildUnitEvent(city, result.Unit));
        }

        private bool TryAttackUnit(int attackerID, int defenderID)
        {
            Unit attacker = state.TryGetUnit(attackerID);
            Unit defender = state.TryGetUnit(defenderID);
            if (attacker == null || defender == null)
            {
                Debug.Log("非法ID");
                return false;
            }

            AttackUnitResult result = state.AttackUnit(attacker, defender);
            if (!result.Success)
            {
                switch (result.Reason)
                {
                    case AttackUnitFailReason.NoAccess:
                        Debug.Log("玩家无权限");
                        break;
                    case AttackUnitFailReason.IsSameOwner:
                        Debug.Log("目标单位为己方单位，不可攻击");
                        break;
                    case AttackUnitFailReason.IsSettler:
                        Debug.Log("当前单位为移民，不可攻击");
                        break;
                    case AttackUnitFailReason.Unreachable:
                        Debug.Log("该地块不可到达");
                        break;
                }
                return false;
            }

            //调用攻击单位协程
            isBlockingInput = true;
            unitView.AttackUnit(attacker, defender, result.AttackerIsDead, result.DefenderIsDead, result.CanEnter, result.Path);
            return true;
        }

        private bool TryAttackCity(int attackerID, int cityID)
        {
            Unit attacker = state.TryGetUnit(attackerID);
            City city = state.TryGetCity(cityID);
            if (attacker == null || city == null)
            {
                Debug.Log("非法ID");
                return false;
            }

            AttackCityResult result = state.AttackCity(attacker, city);
            if (!result.Success)
            {
                switch (result.Reason)
                {
                    case AttackCityFailReason.NoAccess:
                        Debug.Log("玩家无权限");
                        break;
                    case AttackCityFailReason.IsSameOwner:
                        Debug.Log("目标城市为己方单位，不可攻击");
                        break;
                    case AttackCityFailReason.IsSettler:
                        Debug.Log("当前单位为移民，不可攻击");
                        break;
                    case AttackCityFailReason.Unreachable:
                        Debug.Log("该地块不可到达");
                        break;
                }
                return false;
            }

            //调用攻击单位协程
            isBlockingInput = true;
            unitView.AttackCity(attacker, city, result.CityIsCaptured, result.Path, result.DefenderIsDead);
            return true;
        }

        private void GameOver()
        {
            UIManager.Instance.ShowPanel<GameOverPanel>();
        }

        private bool SubmitOrder(BaseOrder order)
        {
            if(order.PlayerId != state.CurrentPlayer)
            {
                Debug.Log("非当前玩家命令");
                return false;
            }
            switch (order)
            {
                case MoveUnitOrder o: 
                    return TryMoveUnit(o.UnitID, o.Target);
                case AttackUnitOrder o: 
                    return TryAttackUnit(o.AttackerID, o.DefenderID);
                case AttackCityOrder o: 
                    return TryAttackCity(o.AttackerID, o.CityID);
                case FoundCityOrder o: 
                    TryFoundCity(o.UnitID);
                    return false;
                case BuildUnitOrder o: 
                    TryBuildUnit(o.CityID, o.Type);
                    return false;
                case EndPhaseOrder o:
                    TryEndPhase();
                    return false;
                default:
                    Debug.LogError($"未知命令类型：{order.GetType().Name}");
                    return false;
            }
        }

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

        public void RequestMoveUnit(int unitId, HexCoord target)
        {
            throw new System.NotImplementedException();
        }

        public void RequestAttackUnit(int attackerId, int defenderId)
        {
            throw new System.NotImplementedException();
        }

        public void RequestAttackCity(int attackerId, int cityId)
        {
            throw new System.NotImplementedException();
        }

    }
}
