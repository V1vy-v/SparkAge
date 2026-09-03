using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Hex;
using SparkAge.Model.Units;
using System.Collections.Generic;
using UnityEngine;

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

        //外部提供字段
        GameState _state;
        float hexSize;
        Mesh _hexMesh;//地块网格

        //独占字段
        Mesh _unitHighlightMesh;//单位选中框网格
        Mesh _reachableMesh;//单位可到达地块网格
        MeshRenderer _highlight;//地块高亮渲染器
        MeshRenderer _unitHighlight;//单位选中框渲染器

        Unit _selectedUnit;//当前选中的单位
        public Unit SelectedUnit => _selectedUnit;//当前选中的单位：外部访问接口
        List<HexCoord> reachableHex = new();//当前单位可移动范围
        List<GameObject> reachableObjs = new List<GameObject>(128);//可移动范围对象


        public void Init(GameState state, float hexSize, Mesh hexMesh)
        {
            _state = state;
            this.hexSize = hexSize;
            _hexMesh = hexMesh;

            _unitHighlightMesh = HexMeshFactory.CreateHexMesh(0.8f * hexSize);
            _reachableMesh = HexMeshFactory.CreateHexMesh(0.9f * hexSize);

            BuildHighlight();
            BuildReachableObj();
        }

        /// <summary>
        /// 创建高亮六边形对象和单位选中框
        /// </summary>
        private void BuildHighlight()
        {
            //地块高亮
            GameObject obj = new GameObject("highlight");
            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.mesh = _hexMesh;
            _highlight = obj.AddComponent<MeshRenderer>();
            _highlight.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = highlightColor
            };
            obj.SetActive(false);

            //单位选中框
            obj = new GameObject("unitHighlight");
            mf = obj.AddComponent<MeshFilter>();
            mf.mesh = _unitHighlightMesh;
            _unitHighlight = obj.AddComponent<MeshRenderer>();
            _unitHighlight.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = unitHighlightColor
            };
            obj.SetActive(false);
        }
        /// <summary>
        /// 预创建64个移动范围对象
        /// </summary>
        /// <param name="point"></param>
        public void BuildReachableObj()
        {
            GameObject reachableObj; 
            MeshFilter mf; 
            MeshRenderer mr;
            Material material = new Material(Shader.Find("Sprites/Default"))
            {
                color = reachableColor
            };
            for (int i = 0; i < 64; i++)
            {
                reachableObj = new GameObject("reachableTile");
                mf = reachableObj.AddComponent<MeshFilter>();
                mf.mesh = _reachableMesh;
                mr = reachableObj.AddComponent<MeshRenderer>();
                mr.material = material;
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
            //能被射线检测即在地图内
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float dist))
            {
                HexCoord clickHex = HexLayout.PixelToHex(ray.GetPoint(dist), hexSize);

                if (_state.Map.IsInMap(clickHex))
                    return clickHex;
            }
                
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
            _highlight.transform.position = HexLayout.HexToPixel((HexCoord)clickHex, hexSize, 0.02f);
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
            _unitHighlight.transform.position = HexLayout.HexToPixel(unit.Position, hexSize, 0.06f);
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
                reachableObjs[i].transform.position = HexLayout.HexToPixel(hex, hexSize, 0.04f);
                i++;
            }
        }
    }
}