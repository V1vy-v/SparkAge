using SparkAge.Core;
using SparkAge.Core.Hex;
using SparkAge.Core.Map;
using SparkAge.Core.Units;
using UnityEngine;

namespace SparkAge.Game
{
    /// <summary>
    /// 地图的表现层
    /// </summary>
    public class MapView : MonoBehaviour
    {
        [SerializeField] int seed;
        [SerializeField] float hexSize = 1f;//单位大小
        [SerializeField] Color plainColor = new Color(0.45f, 0.75f, 0.45f);          // 平原的绿色（alpha 默认 1）
        [SerializeField] Color forestColor = new Color(0.0f, 0.45f, 0.0f);           // 森林的深绿色
        [SerializeField] Color mountainColor = new Color(0.55f, 0.27f, 0.07f);       // 山峦的褐色
        [SerializeField] Color waterColor = new Color(0.0f, 0.75f, 1.0f);            // 水域的天蓝色

        Sprite _hexSprite;//地块精灵
        GameState _state;//游戏世界状态（含地图与单位数据）
        UnitView _unitView;//单位控制类
        SelectionController _selection;//鼠标选中控制类

        bool _isMoving = false;//为true时屏蔽点击

        private void Awake()
        {
            _hexSprite = HexSpriteFactory.CreateHexSprite(128, Color.white);
            _state = new GameState(MapGenerator.Generate(20, 20, seed));

            _unitView = gameObject.AddComponent<UnitView>();
            _unitView.Init(_state, hexSize);

            _selection = gameObject.AddComponent<SelectionController>();
            _selection.Init(_state, hexSize, _hexSprite);
        }
        private void Start()
        {
            //创建地图和高亮资源，摄像机初始定位，范围对象
            BuildTiles();
            CenterCameraOnMap();

            //创建单位
            HexCoord? spawnPoint = _state.FindSpawnPoint(_state.Map.Center);
            if (spawnPoint != null)
            {
                GameObject obj = _unitView.BuildUnit((HexCoord)spawnPoint);
                Unit unit = new Unit((HexCoord)spawnPoint, 0, 3, 3);
                _state.Units.Add(unit);
                _unitView.UnitObjs[unit] = obj;
            }
            else
                print("创建单位出生点失败！！！");

            spawnPoint = _state.FindSpawnPoint(new HexCoord(0, 0));
            if (spawnPoint != null)
            {
                GameObject obj = _unitView.BuildUnit((HexCoord)spawnPoint);
                Unit unit = new Unit((HexCoord)spawnPoint, 0, 4, 4);
                _state.Units.Add(unit);
                _unitView.UnitObjs[unit] = obj;
            }
            else
                print("创建单位出生点失败！！！");
        }
        private void Update()
        {
            if (_isMoving) return;

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
                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.color = GetTerrainColor(tile.Type);
                sr.sprite = _hexSprite;
                sr.sortingOrder = 0;

                obj.transform.position = HexLayout.HexToPixel(tile.Coord, hexSize);
            }
        }

        /// <summary>
        /// 摄像机位置初始化：地图中央
        /// </summary>
        private void CenterCameraOnMap()
        {
            Vector2 center = HexLayout.HexToPixel(new HexCoord(_state.Map.Width / 2, _state.Map.Height / 2), hexSize);
            Camera.main.transform.position = new Vector3(center.x, center.y, -10f);
        }
        /// <summary>
        /// 获取地图边界：左下和右上端点
        /// </summary>
        /// <returns></returns>
        public (Vector2, Vector2) GetMapBounds()
        {
            return (HexLayout.HexToPixel(new HexCoord(_state.Map.Width - 1, _state.Map.Height - 1), hexSize),
                HexLayout.HexToPixel(new HexCoord(0, 0), hexSize));
        }
        /// <summary>
        /// 根据地形设置颜色，表现层职责
        /// </summary>
        /// <param name="terrainType"></param>
        /// <returns></returns>
        public Color GetTerrainColor(TerrainType terrainType)
        {
            switch(terrainType)
            {
                case TerrainType.Plain:
                    return plainColor;
                case TerrainType.Forest:
                    return forestColor;
                case TerrainType.Mountain:
                    return mountainColor;
                case TerrainType.Water:
                    return waterColor;
                default:
                    return plainColor;
            }
        }
     
    }

}
