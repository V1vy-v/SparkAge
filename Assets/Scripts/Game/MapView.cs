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
        [SerializeField] float hexSize = 1f;
        [SerializeField] Color plainColor = new Color(0.45f, 0.75f, 0.45f);   // 平原的绿色
        [SerializeField] Color highlightColor = new Color(1f, 1f, 0.4f, 0.6f); // 高亮的黄色(半透明)
        
        MapData _map;
        Sprite _hexSprite;
        SpriteRenderer _highlight;

        private void Start()
        {
            _map = new MapData(5, 5);
            _hexSprite = HexSpriteFactory.CreateHexSprite(128, Color.white);
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
                sr.color = plainColor;
                sr.sprite = _hexSprite;
                sr.sortingOrder = 0;

                obj.transform.position = HexLayout.HexToPixel(tile.Coord, hexSize);
            }
        }

        private void BuildHighlight()
        {
            //创建高亮六边形对象
            GameObject obj = new GameObject("highlight");
            _highlight = obj.AddComponent<SpriteRenderer>();
            _highlight.color = highlightColor;
            _highlight.sprite = _hexSprite;
            _highlight.sortingOrder = 10;
            //初始隐藏
            obj.SetActive(false);
        }
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
        private void CenterCameraOnMap()
        {
            Vector2 center = HexLayout.HexToPixel(new HexCoord(_map.Width / 2, _map.Height / 2), hexSize);
            Camera.main.transform.position = new Vector3(center.x, center.y, -10f);
            print(Camera.main.transform.position);
        }

    }
}
