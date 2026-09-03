using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Hex;
using SparkAge.Model.Map;
using SparkAge.Model.Units;
using UnityEngine;

namespace SparkAge.View
{
    /// <summary>
    /// 地图的表现层
    /// </summary>
    public class MapView : MonoBehaviour
    {
        [SerializeField] int seed;//地图种子
        [SerializeField] float hexSize = 1f;//单位大小

        [SerializeField] Material plainMaterial;
        [SerializeField] Material forestMaterial;
        [SerializeField] Material mountainMaterial;
        [SerializeField] Material waterMaterial;
        public Material GetMaterial(TerrainType type) => type switch
        {
            TerrainType.Plain => plainMaterial,
            TerrainType.Forest => forestMaterial,
            TerrainType.Mountain => mountainMaterial,
            TerrainType.Water => waterMaterial,
            _ => plainMaterial
        };

        Mesh _hexMesh;//地块网格
        GameState _state;//游戏世界状态（含地图与单位数据）
        UnitView _unitView;//单位表现层
        CityView _cityView;//城市表现层
        SelectionView _selection;//鼠标选中表现层

        bool _isMoving = false;//为true时屏蔽点击

        private void Awake()
        {
            _state = new GameState(MapGenerator.Generate(20, 20, seed));
            _hexMesh = HexMeshFactory.CreateHexMesh(hexSize);
            EnsureMaterials();

            _unitView = gameObject.AddComponent<UnitView>();
            _unitView.Init(_state, hexSize);

            _selection = gameObject.AddComponent<SelectionView>();
            _selection.Init(_state, hexSize, _hexMesh);
        }
        private void Start()
        {
            //创建地图和高亮资源，范围对象
            BuildTiles();

            //创建单位
            HexCoord? spawnPoint = _state.FindSpawnPoint(_state.Map.Center);
            if (spawnPoint != null)
            {
                GameObject obj = _unitView.BuildUnit((HexCoord)spawnPoint, UnitType.warrior);
                Unit unit = new Unit(UnitType.warrior, (HexCoord)spawnPoint, 0, 3, 3);
                _state.Units.Add(unit);
                _unitView.UnitObjs[unit] = obj;
            }
            else
                print("创建单位出生点失败！！！");

            spawnPoint = _state.FindSpawnPoint(new HexCoord(0, 0));
            if (spawnPoint != null)
            {
                GameObject obj = _unitView.BuildUnit((HexCoord)spawnPoint, UnitType.Settler);
                Unit unit = new Unit(UnitType.Settler, (HexCoord)spawnPoint, 0, 4, 4);
                _state.Units.Add(unit);
                _unitView.UnitObjs[unit] = obj;
            }
            else
                print("创建单位出生点失败！！！");
        }
        private void Update()
        {
            if (_isMoving) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _state.EndTurn();
                if(_selection.SelectedUnit != null)
                    _selection.SelectUnit(_selection.SelectedUnit);
            }
            if (Input.GetMouseButtonDown(0))
            {
                _selection.HandleClick();
            }
            if (Input.GetMouseButtonDown(1) && _selection.SelectedUnit != null)
            {
                HexCoord? hex = _selection.GetClickHex();
                if (hex != null)
                    _unitView.MoveUnit(_selection.SelectedUnit, (HexCoord)hex, _selection.SelectUnit, (isMoving) => _isMoving = isMoving);
            }
        }
        /// <summary>
        /// 创建地图中的六边形
        /// </summary>
        private void BuildTiles()
        {
            foreach(var tile in _state.Map.Tiles.Values)
            {
                GameObject obj = new GameObject($"tile {tile.Coord.R}, {tile.Coord.Q}");
                MeshFilter mf = obj.AddComponent<MeshFilter>();
                mf.mesh = _hexMesh;
                MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                mr.material = GetMaterial(tile.Type);

                obj.transform.position = HexLayout.HexToPixel(tile.Coord, hexSize, 0);
            }
        }
        /// <summary>
        /// 确认材质已创建
        /// </summary>
        private void EnsureMaterials()
        {
            if (plainMaterial == null)
                plainMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new UnityEngine.Color(0.45f, 0.75f, 0.45f)
                };
            if (forestMaterial == null)
                forestMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new UnityEngine.Color(0f, 0.45f, 0f)
                };
            if (mountainMaterial == null)
                mountainMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new UnityEngine.Color(0.55f, 0.27f, 0.07f)
                };
            if (waterMaterial == null)
                waterMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new UnityEngine.Color(0f, 0.75f, 1f)
                };
        }

        /// <summary>
        /// 获取地图边界：左下和右上端点
        /// </summary>
        /// <returns></returns>
        public (Vector3, Vector3, Vector3) GetMapCenterAndBounds()
        {
            Vector3 center = HexLayout.HexToPixel(new HexCoord(_state.Map.Width / 2, _state.Map.Height / 2), hexSize, 0);
            Vector3 bound1 = HexLayout.HexToPixel(new HexCoord(_state.Map.Width - 1, _state.Map.Height - 1), hexSize, 0);
            Vector3 bound2 = HexLayout.HexToPixel(new HexCoord(0, 0), hexSize, 0);
            return (center, bound1, bound2);
        }

    }

}
