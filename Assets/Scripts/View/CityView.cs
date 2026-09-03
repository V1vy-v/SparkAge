using SparkAge.Model;
using SparkAge.Model.Hex;
using UnityEngine;

namespace SparkAge.View
{
    public class CityView : MonoBehaviour
    {
        //外部提供字段
        GameState _state;
        float hexSize;

        //独占字段
        //Dictionary<Unit, GameObject> _unitObjs = new Dictionary<Unit, GameObject>();// 单位->游戏对象的映射
        //public Dictionary<Unit, GameObject> UnitObjs => _unitObjs;

        public void Init(GameState state, float hexSize)
        {
            _state = state;
            this.hexSize = hexSize;
        }

        /// <summary>
        /// 创建城市边界：待定
        /// </summary>
        public void BuildCity(HexCoord point)
        {

        }
    }
}

