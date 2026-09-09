using SparkAge.Config;
using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.GameInfos;
using SparkAge.Model.Hex;
using SparkAge.Model.Map;
using SparkAge.Model.Units;
using SparkAge.View;
using System.Collections.Generic;
using UnityEngine;
using static SparkAge.Framework.EventCenter.EventDefine;
using static SparkAge.Model.GameState;

namespace SparkAge.Controller
{
    /// <summary>
    /// 游戏控制层
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] int seed;//地图种子
        [SerializeField] float hexSize = 1f;//单位大小
        [SerializeField] GameCfg gameCfg;//游戏配置表
        [SerializeField] CameraController CameraController;//相机控制器

        //数据层引用
        GameState state;
        GameInfo gameInfo;
        //视图层引用
        MapView mapView;
        UnitView unitView;
        SelectionView selectionView;
        CityView cityView;
        bool isMoving;

        private void Awake()
        {
            //配置表注入
            InitGameInfo();

            state = new GameState(MapGenerator.Generate(20, 20, seed), gameInfo);

            mapView = gameObject.AddComponent<MapView>();
            mapView.Init(state, hexSize);

            unitView = gameObject.AddComponent<UnitView>();
            unitView.Init(state, hexSize);

            selectionView = gameObject.AddComponent<SelectionView>();
            selectionView.Init(state, hexSize, mapView.HexMesh);

            cityView = gameObject.AddComponent<CityView>();
            cityView.Init(state, hexSize);
        }
        private void Start()
        {
            //订阅事件
            EventCenter.Instance.AddListener<UnitMoveEvent>(e =>
            {
                isMoving = false;
            });
            EventCenter.Instance.AddListener<AttackUnitEvent>(e =>
            {
                isMoving = false;
            });
            EventCenter.Instance.AddListener<AttackCityEvent>(e =>
            {
                isMoving = false;
            });

            //构建地图
            mapView.BuildTiles();

            //初始化摄像机脚本
            (Vector3, Vector3, Vector3) keyPos = mapView.GetMapCenterAndBounds();
            CameraController.Init(keyPos.Item1, keyPos.Item2, keyPos.Item3);

            //初始拥有一个移民
            HexCoord? spawnPoint = state.FindSpawnPoint(state.Map.Center);
            UnitInfo info = gameInfo.UnitInfos[UnitType.Settler];
            if (spawnPoint != null)
            {
                Unit unit = new Unit(1, (HexCoord)spawnPoint, info);
                GameObject obj = unitView.BuildUnit(unit);
                state.Units.Add(unit);
                unitView.UnitObjs[unit] = obj;
            }
            else
                print("创建单位出生点失败！！！");

            //初始拥有一个移民
            spawnPoint = state.FindSpawnPoint(new HexCoord(1, 2));
            if (spawnPoint != null)
            {
                Unit unit = new Unit(2, (HexCoord)spawnPoint, info);
                GameObject obj = unitView.BuildUnit(unit);
                state.Units.Add(unit);
                unitView.UnitObjs[unit] = obj;
            }
            else
                print("创建单位出生点失败！！！");
        }

        private void Update()
        {
            if (isMoving) return;

            if(Input.GetKeyDown(KeyCode.F) && selectionView.SelectedUnit != null && selectionView.SelectedUnit.Type == UnitType.Settler)
            {
                TryFoundCity(selectionView.SelectedUnit);
            }

            if(selectionView.SelectedCity != null)
            {
                //后期改为UI交互
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    TryBuildUnit(selectionView.SelectedCity, UnitType.Settler);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    TryBuildUnit(selectionView.SelectedCity, UnitType.Warrior);
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                state.EndTurn();
                if (selectionView.SelectedUnit != null)
                    selectionView.SelectUnit(selectionView.SelectedUnit);
            }

            if (Input.GetMouseButtonDown(0))
            {
                selectionView.HandleClick(GetClickHex());
            }
            if (Input.GetMouseButtonDown(1) && selectionView.SelectedUnit != null)
            {
                HexCoord? hex = GetClickHex();
                if (hex != null)
                {
                    Unit tarUnit = state.GetUnitAt((HexCoord)hex);
                    City tarCity = state.GetCityAt((HexCoord)hex);
                    if (tarUnit == null && (tarCity == null || tarCity.Owner == selectionView.SelectedUnit.Owner))
                        TryMoveUnit(selectionView.SelectedUnit, (HexCoord)hex);
                    else if (tarUnit != null)
                        TryAttackUnit(selectionView.SelectedUnit, tarUnit);
                    else if (tarCity != null)
                        TryAttackCity(selectionView.SelectedUnit, tarCity);
                }
            }
        }

        /// <summary>
        /// 初始配置表读取与注入
        /// </summary>
        private void InitGameInfo()
        {
            gameInfo = new GameInfo();
            foreach(var cfg in gameCfg.unitCfgs)
            {
                gameInfo.UnitInfos[cfg.Type] = new UnitInfo(cfg.Type, cfg.Name, cfg.Atk, cfg.Def, cfg.Hp, cfg.Movement, cfg.Cost);
            }
            foreach (var cfg in gameCfg.cityCfgs)
            {
                gameInfo.CityInfos.Add(new CityInfo(cfg.Name, cfg.Def, cfg.Hp, cfg.Radius, cfg.Production));
            }
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
        /// 接收单位数据和移动路线并驱动单位移动动画
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="tarHex"></param>
        /// <param name="callback1"></param>
        /// <param name="callback2"></param>
        public void TryMoveUnit(Unit unit, HexCoord tarHex)
        {
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
                return;
            }

            isMoving = true;
            //发布单位移动事件
            unitView.MoveUnit(unit, result.Path);
        }

        public void TryFoundCity(Unit unit)
        {
            FoundCityResult result = state.FoundCity(unit);
            if(!result.Success)
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

        public void TryBuildUnit(City city, UnitType type)
        {
            //数据层
            BuildUnitResult result =  state.BuildUnit(city, type);
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

        public void TryAttackUnit(Unit attacker, Unit defender)
        {
            AttackUnitResult result = state.AttackUnit(attacker, defender);
            if (!result.Success)
            {
                switch (result.Reason)
                {
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
                return;
            }

            //调用攻击单位协程
            isMoving = true;
            unitView.AttackUnit(attacker, defender, result.AttackerIsDead, result.DefenderIsDead, result.Path);
        }

        public void TryAttackCity(Unit attacker, City city)
        {
            AttackCityResult result = state.AttackCity(attacker, city);
            if (!result.Success)
            {
                switch (result.Reason)
                {
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
                return;
            }

            //调用攻击单位协程
            isMoving = true;
            unitView.AttackCity(attacker, city, result.CityIsCaptured, result.Path, result.DefenderIsDead);
        }
    }
}
