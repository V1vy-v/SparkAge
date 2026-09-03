using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Map;
using SparkAge.Model.Units;
using SparkAge.View;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
        [SerializeField] CameraController CameraController;

        GameState state;
        MapView mapView;
        UnitView unitView;
        SelectionView selectionView;
        CityView cityView;
        bool isMoving;

        private void Awake()
        {
            state = new GameState(MapGenerator.Generate(20, 20, seed));

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
            //订阅单位移动事件
            EventCenter.Instance.AddListener<UnitMoveEvent>(e =>
            {
                isMoving = e.isMoving;
            });

            mapView.BuildTiles();

            (Vector3, Vector3, Vector3) keyPos = mapView.GetMapCenterAndBounds();
            CameraController.Init(keyPos.Item1, keyPos.Item2, keyPos.Item3);

            //布置一个勇士和一个移民
            //创建单位
            HexCoord? spawnPoint = state.FindSpawnPoint(state.Map.Center);
            if (spawnPoint != null)
            {
                Unit unit = new Unit(UnitType.warrior, (HexCoord)spawnPoint, 1, 3, 3);
                GameObject obj = unitView.BuildUnit(unit);
                state.Units.Add(unit);
                unitView.UnitObjs[unit] = obj;
            }
            else
                print("创建单位出生点失败！！！");

            spawnPoint = state.FindSpawnPoint(new HexCoord(0, 0));
            if (spawnPoint != null)
            {
                Unit unit = new Unit(UnitType.Settler, (HexCoord)spawnPoint, 1, 4, 4);
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

            if(Input.GetKeyDown(KeyCode.F) && selectionView.SelectedUnit != null && selectionView.SelectedUnit.type == UnitType.Settler)
            {
                TryFoundCity(selectionView.SelectedUnit);
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
                    TryMoveUnit(selectionView.SelectedUnit, (HexCoord)hex);
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
                Debug.Log(result.Reason == MoveFailReason.Unreachable ? "目标不可达" : "该格已有单位");
                return;    // 失败：不移动、不刷新
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
    }
}
