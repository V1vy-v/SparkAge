using SparkAge.Core;
using SparkAge.Core.Hex;
using SparkAge.Core.Units;
using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.Game
{
    /// <summary>
    /// 处理选中地块/单位的表现层
    /// </summary>
    public class SelectionController : MonoBehaviour
    {
        [SerializeField] Color highlightColor = new Color(1f, 1f, 0f, 0.5f);         // 高亮的黄色（半透明）
        [SerializeField] Color reachableColor = new Color(0f, 0f, 0.6f, 0.7f);       // 深蓝色（半透明）
        [SerializeField] Color unitHighlightColor = new Color(1f, 0f, 0f, 0.8f);     // 红色（略微半透明，以便叠加）

        //外部提供字段
        GameState _state;
        float hexSize;
        Sprite _hexSprite;//地块精灵

        //独占字段
        Sprite _unitHighlightSprite;//单位选中框精灵
        Sprite _reachableSprite;//单位可到达地块精灵
        SpriteRenderer _highlight;//地块高亮渲染器
        SpriteRenderer _unitHighlight;//单位选中框渲染器


        Unit _selectedUnit;//当前选中的单位
        public Unit SelectedUnit => _selectedUnit;//当前选中的单位：外部访问接口
        List<HexCoord> reachableHex = new();//当前单位可移动范围
        List<GameObject> reachableObjs = new List<GameObject>(128);//可移动范围对象


        public void Init(GameState state, float hexSize, Sprite hexSprite)
        {
            _state = state;
            this.hexSize = hexSize;
            _hexSprite = hexSprite;

            _reachableSprite = HexSpriteFactory.CreateHexCircleSprite(122, Color.white);
            _unitHighlightSprite = HexSpriteFactory.CreateHexCircleSprite(122, Color.white);
            BuildHighlight();
            BuildReachableObj();
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
        /// 预创建64个移动范围对象
        /// </summary>
        /// <param name="point"></param>
        public void BuildReachableObj()
        {
            GameObject reachableObj;
            for (int i = 0; i < 64; i++)
            {
                reachableObj = new GameObject("reachableTile");
                SpriteRenderer sr = reachableObj.AddComponent<SpriteRenderer>();
                sr.sprite = _reachableSprite;
                sr.color = reachableColor;
                sr.sortingOrder = 5;
                reachableObj.SetActive(false);

                reachableObjs.Add(reachableObj);
            }
        }


        /// <summary>
        /// 获取点击处地块Hex
        /// </summary>
        /// <returns></returns>
        public HexCoord? GetClickHex()
        {
            Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            HexCoord clickHex = HexLayout.PixelToHex(new Vector2(clickPos.x, clickPos.y), hexSize);

            if (_state.Map.IsInMap(clickHex))
                return clickHex;
            //不在地图内，无高亮
            return null;
        }

        /// <summary>
        /// 鼠标左键点击总入口，关联点击高亮、单位选中、移动范围显示
        /// </summary>
        public void HandleClick()
        {
            //获取点击地块
            HexCoord? clickHex = GetClickHex();
            //在地图外
            if (clickHex == null)
            {
                ClearHighlight();
                ClearSelection();
                return;
            }
            //在地图内:
            //显示地块高亮
            ShowHighlight(clickHex);
            //是否选中单位
            _selectedUnit = _state.GetUnitAt((HexCoord)clickHex);
            if (_selectedUnit != null)
                SelectUnit(_selectedUnit);
            else
            {
                //断开引用
                _selectedUnit = null;
                ClearSelection();
            }
        }
        /// <summary>
        /// 控制地块高亮：移动高亮对象
        /// </summary>
        public void ShowHighlight(HexCoord? clickHex)
        {
            _highlight.transform.position = HexLayout.HexToPixel((HexCoord)clickHex, hexSize);
            _highlight.gameObject.SetActive(true);
        }
        /// <summary>
        /// 隐藏地块高亮
        /// </summary>
        private void ClearHighlight()
        {
            _highlight.gameObject.SetActive(false);
        }
        /// <summary>
        /// 实现点击选中单位和显示可移动范围
        /// </summary>
        public void SelectUnit(Unit unit)
        {
            //高亮选中框
            _unitHighlight.transform.position = HexLayout.HexToPixel(unit.Position, hexSize);
            _unitHighlight.gameObject.SetActive(true);

            //计算可移动范围
            reachableHex = _state.GetReachableTiles(unit);

            //显示移动范围
            ShowRange(reachableHex);

            Debug.Log("当前单位剩余移动力：" + unit.MovementLeft);
        }
        /// <summary>
        /// 隐藏选中框和范围对象
        /// </summary>
        public void ClearSelection()
        {
            //隐藏选中框
            _unitHighlight.gameObject.SetActive(false);
            //隐藏所有范围对象
            for (int i = 0; i < reachableObjs.Count; i++)
                reachableObjs[i].gameObject.SetActive(false);
            //清除选中对象
            _selectedUnit = null;
        }
        /// <summary>
        /// 刷新可到达范围：先隐藏再显示
        /// </summary>
        /// <param name="reachableHex"></param>
        public void ShowRange(List<HexCoord> reachableHex)
        {
            //隐藏所有范围对象
            foreach (var obj in reachableObjs)
                obj.SetActive(false);
            //显示可到达范围对象
            int i = 0;
            foreach (var hex in reachableHex)
            {
                reachableObjs[i].SetActive(true);
                reachableObjs[i].transform.position = HexLayout.HexToPixel(hex, hexSize);
                i++;
            }

        }
    }
}