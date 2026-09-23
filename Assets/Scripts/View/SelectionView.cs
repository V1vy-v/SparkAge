using SparkAge.Controller.Network;
using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Units;
using System;
using System.Collections.Generic;
using UnityEngine;
using static SparkAge.Framework.EventCenter.EventDefine;

namespace SparkAge.View
{
    /// <summary>
    /// 处理选中地块/单位的表现层
    /// </summary>
    public class SelectionView : MonoBehaviour
    {
        [SerializeField] Transform highlightRoot;

        //外部提供字段
        GameState state;
        float hexSize;

        //独占字段
        GameObject highlight;//地块高亮
        GameObject outline;//单位选中框
        GameObject rangeBlue;//移动范围对象
        GameObject rangeRed;//攻击范围对象
        List<GameObject> moveObjs = new List<GameObject>(64);//可移动范围对象
        List<GameObject> attackObjs = new List<GameObject>(32);//可攻击范围对象

        Unit selectedUnit;//当前选中的单位
        public Unit SelectedUnit => selectedUnit;//当前选中的单位：外部访问接口
        City selectedCity;//当前选中的城市
        public City SelectedCity => selectedCity;//当前选中的单位：外部访问接口
        HashSet<HexCoord> moveHex = new();//当前单位可移动范围
        HashSet<HexCoord> attackHex = new();//当前单位可移动范围


        public void Init(GameState state, float hexSize)
        {
            this.state = state;
            this.hexSize = hexSize;

            highlightRoot = GameObject.Find("HighlightRoot").transform;

            LoadAndBuildSelections();

            EventCenter.Instance.AddListener<RemoveUnitEvent>(OnRemoveUnit);
            EventCenter.Instance.AddListener<MoveUnitCompletedEvent>(OnMoveUnitCompleted);
            EventCenter.Instance.AddListener<AttackUnitCompletedEvent>(OnAttackUnitCompleted);
            EventCenter.Instance.AddListener<AttackCityCompletedEvent>(OnAttackCityCompleted);
            EventCenter.Instance.AddListener<SelectionClearEvent>(OnSelectionClear);
        }
        private void OnDestroy()
        {
            EventCenter.Instance.RemoveListener<RemoveUnitEvent>(OnRemoveUnit);
            EventCenter.Instance.RemoveListener<MoveUnitCompletedEvent>(OnMoveUnitCompleted);
            EventCenter.Instance.RemoveListener<AttackUnitCompletedEvent>(OnAttackUnitCompleted);
            EventCenter.Instance.RemoveListener<AttackCityCompletedEvent>(OnAttackCityCompleted);
            EventCenter.Instance.RemoveListener<SelectionClearEvent>(OnSelectionClear);
        }

        public void OnRemoveUnit(RemoveUnitEvent e)
        {
            ClearAll();
        }
        private void OnMoveUnitCompleted(MoveUnitCompletedEvent e)
        {
            if (e.Unit == null || e.Unit.Owner != NetworkMgr.Instance.MyPlayerId)
                return;
            SelectUnit(e.Unit);
        }
        private void OnAttackUnitCompleted(AttackUnitCompletedEvent e)
        {
            if (e.Attacker == null || e.Attacker.Owner != NetworkMgr.Instance.MyPlayerId)
                return;

            if (!e.Attacker.IsDead)
                SelectUnit(e.Attacker);
            else
                ClearAll();
        }
        private void OnAttackCityCompleted(AttackCityCompletedEvent e)
        {
            if (e.Attacker == null || e.Attacker.Owner != NetworkMgr.Instance.MyPlayerId)
                return;

            if (!e.Attacker.IsDead)
                SelectUnit(e.Attacker);
            else
                ClearAll();
        }
        private void OnSelectionClear(SelectionClearEvent e)
        {
            ClearAll();
        }


        /// <summary>
        /// 预创建高亮、单位选中框、移动范围和攻击范围对象
        /// </summary>
        private void LoadAndBuildSelections()
        {
            //地块高亮
            highlight = Instantiate(Resources.Load<GameObject>("Prefabs/Selection/HexHighlight"), highlightRoot);
            highlight.SetActive(false);

            //单位选中框
            outline = Instantiate(Resources.Load<GameObject>("Prefabs/Selection/HexOutline"), highlightRoot);
            outline.SetActive(false);

            GameObject obj;
            //移动范围对象
            rangeBlue = Resources.Load<GameObject>("Prefabs/Selection/HexRangeBlue");
            for (int i = 0; i < 64; i++)
            {
                obj = Instantiate(rangeBlue, highlightRoot);
                obj.SetActive(false);
                moveObjs.Add(obj);
            }

            //攻击范围对象
            rangeRed = Resources.Load<GameObject>("Prefabs/Selection/HexRangeRed");
            for (int i = 0; i < 32; i++)
            {
                obj = Instantiate(rangeRed, highlightRoot);
                obj.SetActive(false);
                attackObjs.Add(obj);
            }
        }


        /// <summary>
        /// 接收点击地块，关联点击高亮、单位选中、移动范围显示
        /// </summary>
        public void HandleClick(HexCoord? clickHex)
        {
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
            selectedUnit = state.GetUnitAt((HexCoord)clickHex);
            if (selectedUnit != null)
                SelectUnit(selectedUnit);
            else
            {
                //断开引用
                selectedUnit = null;
                ClearSelection();
            }

            //是否选中城市
            selectedCity = state.GetCityAt((HexCoord)clickHex);
        }
        /// <summary>
        /// 控制地块高亮：移动高亮对象
        /// </summary>
        private void ShowHighlight(HexCoord? clickHex)
        {
            highlight.transform.position = HexLayout.HexToPixel((HexCoord)clickHex, hexSize, 0.29f);
            highlight.gameObject.SetActive(true);
        }
        /// <summary>
        /// 实现点击选中单位和显示可移动范围
        /// </summary>
        public void SelectUnit(Unit unit)
        {
            //高亮选中框
            outline.transform.position = HexLayout.HexToPixel(unit.Position, hexSize, 0.3f);
            outline.gameObject.SetActive(true);
            ShowHighlight(unit.Position);

            ClearaRange();

            if (unit.Owner != NetworkMgr.Instance.MyPlayerId)
                return;

            //计算可移动范围
            (moveHex, attackHex) = state.GetReachableTiles(unit);

            //显示移动和攻击范围
            ShowRange();
        }
        /// <summary>
        /// 隐藏选中框和范围对象
        /// </summary>
        public void ClearSelection()
        {
            //隐藏选中框
            outline.gameObject.SetActive(false);
            //清除移动范围
            ClearaRange();
            //清除选中对象
            selectedUnit = null;
        }
        /// <summary>
        /// 显示可到达范围
        /// </summary>
        /// <param name="reachableHex"></param>
        public void ShowRange()
        {
            //显示可到达范围对象
            int i = 0;
            foreach (var hex in moveHex)
            {
                moveObjs[i].SetActive(true);
                moveObjs[i].transform.position = HexLayout.HexToPixel(hex, hexSize, 0.26f);
                i++;
            }
            i = 0;
            foreach (var hex in attackHex)
            {
                attackObjs[i].SetActive(true);
                attackObjs[i].transform.position = HexLayout.HexToPixel(hex, hexSize, 0.26f);
                i++;
            }
        }

        /// <summary>
        /// 隐藏地块高亮
        /// </summary>
        public void ClearHighlight()
        {
            highlight.gameObject.SetActive(false);
        }
        /// <summary>
        /// 隐藏所有范围对象
        /// </summary>
        public void ClearaRange()
        {
            foreach (var obj in moveObjs)
                obj.SetActive(false);
            foreach (var obj in attackObjs)
                obj.SetActive(false);
        }
        public void ClearAll()
        {
            ClearHighlight();
            ClearSelection();
        }
    }
}