using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Hex;
using SparkAge.Model.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SparkAge.Framework.EventCenter.EventDefine;

namespace SparkAge.View
{
    /// <summary>
    /// 单位表现层
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        [SerializeField] Material warriorMaterial;
        [SerializeField] Material settlerMaterial;

        //外部提供字段
        GameState state;
        float hexSize;

        //独占字段
        Dictionary<Unit, GameObject> unitObjs = new Dictionary<Unit, GameObject>();// 单位->游戏对象的映射
        public Dictionary<Unit, GameObject> UnitObjs => unitObjs;

        public void Init(GameState state, float hexSize)
        {
            this.state = state;
            this.hexSize = hexSize;

            warriorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = Color.red
            };
            settlerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = Color.blue
            };
        }

        private void Start()
        {
            //订阅建城事件
            EventCenter.Instance.AddListener<FoundCityEvent>(e =>
            {
                DestroyUnit(e.ConsumedSettler);
                Debug.Log("城市已建立");
            });
            //订阅造兵事件
            EventCenter.Instance.AddListener<BuildUnitEvent>(e =>
            {
                BuildUnit(e.BuiltUnit);
                Debug.Log("单位已造好");
            });
        }

        /// <summary>
        /// 创建单位对象
        /// </summary>
        public GameObject BuildUnit(Unit unit)
        {
            GameObject unitObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);

            switch (unit.type)
            {
                case UnitType.Warrior:
                    unitObj.GetComponent<MeshRenderer>().material = warriorMaterial;
                    unitObj.transform.localScale = Vector3.one * 0.5f;
                    break;
                case UnitType.Settler:
                    unitObj.GetComponent<MeshRenderer>().material = settlerMaterial;
                    unitObj.transform.localScale = Vector3.one * 0.3f;
                    break;
            }

            unitObjs[unit] = unitObj;
            unitObj.transform.position = HexLayout.HexToPixel(unit.Position, hexSize, 0.5f);
            return unitObj;
        }
        /// <summary>
        /// 销毁单位对象
        /// </summary>
        /// <param name="unit"></param>
        public void DestroyUnit(Unit unit)
        {
            Destroy(unitObjs[unit]);
            unitObjs.Remove(unit);
        }

        /// <summary>
        /// 单位移动
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="tarHex"></param>
        public void MoveUnit(Unit unit, List<HexCoord> path)
        {
            StartCoroutine(MoveSequence(unit, path));
        }

        private WaitForSeconds moveDeltaTime = new WaitForSeconds(0.5f);
        /// <summary>
        /// 单位移动协程，移动动画
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        IEnumerator MoveSequence(Unit unit, List<HexCoord> path)
        {
            yield return null;
            foreach (HexCoord hex in path)
            {
                unitObjs[unit].transform.position = HexLayout.HexToPixel(hex, hexSize, 0.5f);
                yield return moveDeltaTime;
            }
            //发布单位移动事件
            EventCenter.Instance.EventTrigger<UnitMoveEvent>(new UnitMoveEvent(unit, path, false));
        }
    }
}