using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Hex;
using SparkAge.Model.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static SparkAge.Model.GameState;

namespace SparkAge.View
{
    /// <summary>
    /// 单位表现层
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        [SerializeField] Color warriorColor = Color.red;
        [SerializeField] Color settlerColor = Color.blue;

        //外部提供字段
        GameState _state;
        float hexSize;

        //独占字段
        Dictionary<Unit, GameObject> _unitObjs = new Dictionary<Unit, GameObject>();// 单位->游戏对象的映射
        public Dictionary<Unit, GameObject> UnitObjs => _unitObjs;

        public void Init(GameState state, float hexSize)
        {
            _state = state;
            this.hexSize = hexSize;
        }

        /// <summary>
        /// 创建单位对象
        /// </summary>
        public GameObject BuildUnit(HexCoord point, UnitType type)
        {
            GameObject unitObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);

            switch (type)
            {
                case UnitType.warrior:
                    unitObj.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    {
                        color = warriorColor
                    };
                    unitObj.transform.localScale = Vector3.one * 0.5f;
                    break;
                case UnitType.Settler:
                    unitObj.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    {
                        color = settlerColor
                    };
                    unitObj.transform.localScale = Vector3.one * 0.3f;
                    break;
            }

            unitObj.transform.position = HexLayout.HexToPixel(point, hexSize, 0.5f);
            return unitObj;
        }
        /// <summary>
        /// 销毁单位对象
        /// </summary>
        /// <param name="unit"></param>
        public void DestroyUnit(Unit unit)
        {
            Destroy(_unitObjs[unit]);
            _unitObjs.Remove(unit);
        }

        /// <summary>
        /// 单位移动
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="tarHex"></param>
        public void MoveUnit(Unit unit, HexCoord tarHex, UnityAction<Unit> callback1, UnityAction<bool> callback2)
        {
            MoveResult result = _state.MoveUnit(unit, tarHex);
            if (!result.Success)
            {
                Debug.Log(result.Reason == MoveFailReason.Unreachable ? "目标不可达" : "该格已有单位");
                return;    // 失败：不移动、不刷新
            }

            callback2?.Invoke(true);
            StartCoroutine(MoveSequence(unit, result.Path, callback1, callback2));
        }

        private WaitForSeconds moveDeltaTime = new WaitForSeconds(0.5f);
        /// <summary>
        /// 单位移动协程，移动动画
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        IEnumerator MoveSequence(Unit unit, List<HexCoord> path, UnityAction<Unit> callback1, UnityAction<bool> callback2)
        {
            yield return null;
            foreach (HexCoord hex in path)
            {
                _unitObjs[unit].transform.position = HexLayout.HexToPixel(hex, hexSize, 0.5f);
                yield return moveDeltaTime;
            }
            callback1?.Invoke(unit);
            callback2?.Invoke(false);
        }
    }
}