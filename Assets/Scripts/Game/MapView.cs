using UnityEngine;
using SparkAge.Core.Hex;
using SparkAge.Core.Map;
using Unity.VisualScripting;

namespace SparkAge.Game
{
    /// <summary>
    /// 地图的"显示器"：把 Core 里的 MapData 画到屏幕上。
    /// 数据在 MapData，这里只管"显示"和"把点击翻译成门牌号"。
    /// </summary>
    public class MapView : MonoBehaviour
    {
        [SerializeField] float hexSize = 1f;//单位大小
        [SerializeField] int seed;
        [SerializeField] Color plainColor = new Color(0.45f, 0.75f, 0.45f);   // 平原的绿色
        [SerializeField] Color forestColor = new Color(1f, 1f, 0.4f, 0.6f); // 森林的深绿色
        [SerializeField] Color mountainColor = new Color(1f, 1f, 0.4f, 0.6f); // 山峦的褐色
        [SerializeField] Color waterColor = new Color(1f, 1f, 0.4f, 0.6f); // 水域的天蓝色
        [SerializeField] Color highlightColor = new Color(1f, 1f, 0.4f, 0.6f); // 高亮的黄色(半透明)

        MapData _map;
        Sprite _hexSprite;
        SpriteRenderer _highlight;

        private void Awake()
        {
            _map = MapGenerator.Generate(20, 20, seed);
            _hexSprite = HexSpriteFactory.CreateHexSprite(128, Color.white);
        }
        private void Start()
        {
            BuildTiles();
            BuildHighlight();
            CenterCameraOnMap();
        }
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                ClickHighlight();
        }
        /// <summary>
        /// 创建地图中的六边形
        /// </summary>
        private void BuildTiles()
        {
            foreach(var tile in _map.Tiles.Values)
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
        /// 创建高亮六边形对象
        /// </summary>
        private void BuildHighlight()
        {
            GameObject obj = new GameObject("highlight");
            _highlight = obj.AddComponent<SpriteRenderer>();
            _highlight.color = highlightColor;
            _highlight.sprite = _hexSprite;
            _highlight.sortingOrder = 10;
            //初始隐藏
            obj.SetActive(false);
        }
        /// <summary>
        /// 实现点击高亮：移动高亮六边形对象
        /// </summary>
        private void ClickHighlight()
        {
            Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            HexCoord clickHex = HexLayout.PixelToHex(new Vector2(clickPos.x, clickPos.y), hexSize);

            //不在地图内，无高亮
            if (!_map.IsInMap(clickHex))
            {
                _highlight.gameObject.SetActive(false);
                return;
            }
            //在地图内，将高亮对象移至目标六边形
            _highlight.transform.position = HexLayout.HexToPixel(clickHex, hexSize);
            _highlight.gameObject.SetActive(true);
        }
        /// <summary>
        /// 摄像机位置初始化：地图中央
        /// </summary>
        private void CenterCameraOnMap()
        {
            Vector2 center = HexLayout.HexToPixel(new HexCoord(_map.Width / 2, _map.Height / 2), hexSize);
            Camera.main.transform.position = new Vector3(center.x, center.y, -10f);
        }
        /// <summary>
        /// 获取地图边界：左下和右上端点
        /// </summary>
        /// <returns></returns>
        public (Vector2, Vector2) GetMapBounds()
        {
            return (HexLayout.HexToPixel(new HexCoord(_map.Width - 1, _map.Height - 1), 1f),
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
