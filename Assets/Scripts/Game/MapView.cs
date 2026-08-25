using SparkAge.Core;
using SparkAge.Core.Hex;
using SparkAge.Core.Map;
using SparkAge.Core.Units;
using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] Color plainColor = new Color(0.45f, 0.75f, 0.45f);          // 平原的绿色（alpha 默认 1）
        [SerializeField] Color forestColor = new Color(0.0f, 0.45f, 0.0f);           // 森林的深绿色
        [SerializeField] Color mountainColor = new Color(0.55f, 0.27f, 0.07f);       // 山峦的褐色
        [SerializeField] Color waterColor = new Color(0.0f, 0.75f, 1.0f);            // 水域的天蓝色
        [SerializeField] Color highlightColor = new Color(1f, 1f, 0f, 0.5f);         // 高亮的黄色（半透明）
        [SerializeField] Color unitHighlightColor = new Color(1f, 0f, 0f, 0.8f);     // 红色（略微半透明，以便叠加）
        [SerializeField] Color reachableColor = new Color(0f, 0f, 0.6f, 0.7f);       // 深蓝色（半透明）

        GameState _state;//游戏世界状态（含地图与单位数据）
        Sprite _hexSprite;//地块精灵
        Sprite _unitSprite;//单位精灵
        Sprite _unitHighlightSprite;//单位选中框精灵
        Sprite _reachableSprite;//单位可到达地块精灵
        SpriteRenderer _highlight;//地块高亮渲染器
        SpriteRenderer _unitHighlight;//单位选中框渲染器

        Unit preClickUnit = null;//上一次点击的单位
        Dictionary<HexCoord, int> reachableHex;//上一次点击的单位可到达地块及剩余移动力
        List<GameObject> reachableObjs = new List<GameObject>();
        bool isRemoved = false;
        Queue<GameObject> reachableObjPools = new Queue<GameObject>();//可到达地块对象池

        private void Awake()
        {
            _state = new GameState(MapGenerator.Generate(20, 20, seed));
            _hexSprite = HexSpriteFactory.CreateHexSprite(128, Color.white);
            _unitSprite = HexSpriteFactory.CreateHexSprite(32, Color.white);
            _reachableSprite = HexSpriteFactory.CreateHexCircleSprite(122, Color.white);
            _unitHighlightSprite = HexSpriteFactory.CreateHexCircleSprite(122, Color.white);
        }
        private void Start()
        {
            //创建地图和高亮资源，摄像机初始定位
            BuildTiles();
            BuildHighlight();
            CenterCameraOnMap();

            //创建单位
            HexCoord? spawnPoint = _state.FindSpawnPoint(_state.Map.Center);
            if (spawnPoint != null)
            {
                BuildUnit((HexCoord)spawnPoint);
                _state.Units.Add(new Unit((HexCoord)spawnPoint, 0, 4, 4));
            }
            else
                print("创建单位出生点失败！！！");
        }
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                ClickHighlight();
                ClickUnitHighlight();
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
        /// 创建高亮六边形对象和单位选中框
        /// </summary>
        private void BuildHighlight()
        {
            //单块选中高亮
            GameObject obj = new GameObject("highlight");
            _highlight = obj.AddComponent<SpriteRenderer>();
            _highlight.color = highlightColor;
            _highlight.sprite = _hexSprite;
            _highlight.sortingOrder = 10;
            //初始隐藏
            obj.SetActive(false);

            //单位选中框
            obj = new GameObject("unitHighlight");
            _unitHighlight = obj.AddComponent<SpriteRenderer>();
            _unitHighlight.color = unitHighlightColor;
            _unitHighlight.sprite = _unitHighlightSprite;
            _unitHighlight.sortingOrder = 20;
            //初始隐藏
            obj.SetActive(false);
        }
        /// <summary>
        /// 获取点击处地块Hex
        /// </summary>
        /// <returns></returns>
        private HexCoord? GetClickHex()
        {
            Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            HexCoord clickHex = HexLayout.PixelToHex(new Vector2(clickPos.x, clickPos.y), hexSize);

            if (_state.Map.IsInMap(clickHex))
                return clickHex;
            //不在地图内，无高亮
            return null;
        }
        /// <summary>
        /// 实现点击地块高亮：移动高亮对象
        /// </summary>
        private void ClickHighlight()
        {
            HexCoord? clickHex = GetClickHex();
            if (clickHex == null)
                _highlight.gameObject.SetActive(false);
            else
            {
                //在地图内，将高亮对象移至目标六边形
                _highlight.transform.position = HexLayout.HexToPixel((HexCoord)clickHex, hexSize);
                _highlight.gameObject.SetActive(true);
            }
        }
        /// <summary>
        /// 实现点击高亮单位选中框：移动选中框对象
        /// </summary>
        private void ClickUnitHighlight()
        {
            HexCoord? clickHex = GetClickHex();
            if (clickHex == null)
                _unitHighlight.gameObject.SetActive(false);
            else
            {
                Unit clickUnit = _state.GetUnitAt((HexCoord)clickHex);
                //在地图内，将高亮对象移至目标六边形
                if (clickUnit != null)
                {
                    _unitHighlight.transform.position = HexLayout.HexToPixel((HexCoord)clickHex, hexSize);
                    _unitHighlight.gameObject.SetActive(true);
                    
                     //若点击单位与上一次点击不同
                    if (preClickUnit == null || clickUnit != preClickUnit)
                    {
                        reachableHex = _state.GetReachableTiles(clickUnit);
                        preClickUnit = clickUnit;
                    }

                    foreach (var hex in reachableHex.Keys)
                    {
                        BuildReachableTile(hex);
                    }
                    isRemoved = false;
                }
                else
                {
                    _unitHighlight.gameObject.SetActive(false);

                    if (!isRemoved)
                    {
                        foreach (var obj in reachableObjs)
                        {
                            obj.SetActive(false);
                            reachableObjPools.Enqueue(obj);
                        }
                        reachableObjs.Clear();
                        isRemoved = true;
                    }
                }
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
        /// <summary>
        /// 创建单位精灵
        /// </summary>
        public void BuildUnit(HexCoord point)
        {
            GameObject unitObj = new GameObject($"Unit 1");
            SpriteRenderer sr = unitObj.AddComponent<SpriteRenderer>();
            sr.sprite = _unitSprite;
            sr.color = Color.red;
            sr.sortingOrder = 11;

            unitObj.transform.position = HexLayout.HexToPixel(point, hexSize);
        }
        public void BuildReachableTile(HexCoord point)
        {
            GameObject reachableObj;
            if (reachableObjPools.Count == 0)
            {
                Debug.Log("已创建可到达地块对象");
                reachableObj = new GameObject("reachableTile");
                SpriteRenderer sr = reachableObj.AddComponent<SpriteRenderer>();
                sr.sprite = _reachableSprite;
                sr.color = reachableColor;
                sr.sortingOrder = 5;
            }
            else
            {
                Debug.Log("已创建可到达地块对象");
                reachableObj = reachableObjPools.Dequeue();
                reachableObj.SetActive(true);
            }
            reachableObj.transform.position = HexLayout.HexToPixel(point, hexSize);
            reachableObjs.Add(reachableObj);
        }
        
    }
}
