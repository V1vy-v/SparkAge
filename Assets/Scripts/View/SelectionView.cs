using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Units;
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
        [SerializeField] Color highlightColor = new Color(1f, 1f, 0f, 0.5f);         // 高亮的黄色（半透明）
        [SerializeField] Color unitHighlightColor = new Color(1f, 0f, 0f, 0.8f);     // 红色（略微半透明，以便叠加）
        [SerializeField] Color reachableColor = new Color(0f, 0f, 0.6f, 0.7f);       // 深蓝色（半透明）
        [SerializeField] Transform highlightRoot;

        //外部提供字段
        GameState state;
        float hexSize;
        Mesh hexMesh;//地块网格

        //独占字段
        Mesh unitHighlightMesh;//单位选中框网格
        Mesh reachableMesh;//单位可到达地块网格
        MeshRenderer highlight;//地块高亮渲染器
        MeshRenderer unitHighlight;//单位选中框渲染器
        List<GameObject> moveObjs = new List<GameObject>(128);//可移动范围对象
        List<GameObject> attackObjs = new List<GameObject>(32);//可攻击范围对象

        Unit selectedUnit;//当前选中的单位
        public Unit SelectedUnit => selectedUnit;//当前选中的单位：外部访问接口
        HashSet<HexCoord> moveHex = new();//当前单位可移动范围
        HashSet<HexCoord> attackHex = new();//当前单位可移动范围
        City selectedCity;//当前选中的城市
        public City SelectedCity => selectedCity;//当前选中的单位：外部访问接口


        public void Init(GameState state, float hexSize, Mesh hexMesh)
        {
            this.state = state;
            this.hexSize = hexSize;
            this.hexMesh = hexMesh;

            highlightRoot = GameObject.Find("HighlightRoot").transform;

            unitHighlightMesh = HexMeshFactory.CreateHexMesh(0.8f * hexSize);
            reachableMesh = HexMeshFactory.CreateHexMesh(0.9f * hexSize);

            BuildHighlight();
            BuildMoveAndAttackObjs();
        }


        /// <summary>
        /// 创建高亮六边形对象和单位选中框
        /// </summary>
        private void BuildHighlight()
        {
            //地块高亮
            GameObject obj = new GameObject("highlight");
            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.mesh = hexMesh;
            highlight = obj.AddComponent<MeshRenderer>();
            highlight.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = highlightColor
            };
            obj.transform.SetParent(highlightRoot, false);
            obj.SetActive(false);

            //单位选中框
            obj = new GameObject("unitHighlight");
            mf = obj.AddComponent<MeshFilter>();
            mf.mesh = unitHighlightMesh;
            unitHighlight = obj.AddComponent<MeshRenderer>();
            unitHighlight.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = unitHighlightColor
            };
            obj.transform.SetParent(highlightRoot, false);
            obj.SetActive(false);
        }
        /// <summary>
        /// 预创建移动范围和攻击范围对象
        /// </summary>
        /// <param name="point"></param>
        private void BuildMoveAndAttackObjs()
        {
            GameObject moveObj, attackObj;
            MeshFilter mf; 
            MeshRenderer mr;
            Material material1 = new Material(Shader.Find("Sprites/Default"))
            {
                color = reachableColor
            };
            Material material2 = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.red
            };
            for (int i = 0; i < 128; i++)
            {
                moveObj = new GameObject("moveTile");
                mf = moveObj.AddComponent<MeshFilter>();
                mf.mesh = reachableMesh;
                mr = moveObj.AddComponent<MeshRenderer>();
                mr.material = material1;
                moveObj.transform.SetParent(highlightRoot, false);
                moveObj.SetActive(false);

                moveObjs.Add(moveObj);
            }
            for (int i = 0; i < 128; i++)
            {
                attackObj = new GameObject("moveTile");
                mf = attackObj.AddComponent<MeshFilter>();
                mf.mesh = reachableMesh;
                mr = attackObj.AddComponent<MeshRenderer>();
                mr.material = material2;
                attackObj.transform.SetParent(highlightRoot, false);
                attackObj.SetActive(false);

                attackObjs.Add(attackObj);
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
            highlight.transform.position = HexLayout.HexToPixel((HexCoord)clickHex, hexSize, 0.02f);
            highlight.gameObject.SetActive(true);
        }
        /// <summary>
        /// 隐藏地块高亮
        /// </summary>
        private void ClearHighlight()
        {
            highlight.gameObject.SetActive(false);
        }
        /// <summary>
        /// 实现点击选中单位和显示可移动范围
        /// </summary>
        private void SelectUnit(Unit unit)
        {
            //高亮选中框
            unitHighlight.transform.position = HexLayout.HexToPixel(unit.Position, hexSize, 0.06f);
            unitHighlight.gameObject.SetActive(true);

            ClearaRange();

            if (unit.Owner != state.CurrentPlayer)
                return;

            //计算可移动范围
            (moveHex, attackHex) = state.GetReachableTiles(unit);

            //显示移动和攻击范围
            ShowRange();
        }
        /// <summary>
        /// 隐藏选中框和范围对象
        /// </summary>
        private void ClearSelection()
        {
            //隐藏选中框
            unitHighlight.gameObject.SetActive(false);
            //清除移动范围
            ClearaRange();
            //清除选中对象
            selectedUnit = null;
        }
        /// <summary>
        /// 显示可到达范围
        /// </summary>
        /// <param name="reachableHex"></param>
        private void ShowRange()
        {
            //显示可到达范围对象
            int i = 0;
            foreach (var hex in moveHex)
            {
                moveObjs[i].SetActive(true);
                moveObjs[i].transform.position = HexLayout.HexToPixel(hex, hexSize, 0.04f);
                i++;
            }
            i = 0;
            foreach (var hex in attackHex)
            {
                attackObjs[i].SetActive(true);
                attackObjs[i].transform.position = HexLayout.HexToPixel(hex, hexSize, 0.04f);
                i++;
            }
        }
        /// <summary>
        /// 隐藏所有范围对象
        /// </summary>
        private void ClearaRange()
        {
            foreach (var obj in moveObjs)
                obj.SetActive(false);
            foreach (var obj in attackObjs)
                obj.SetActive(false);
        }
    }
}