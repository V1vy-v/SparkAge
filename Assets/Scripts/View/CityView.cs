using SparkAge.Framework.EventCenter;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using UnityEngine;
using static SparkAge.Framework.EventCenter.EventDefine;

namespace SparkAge.View
{
    public class CityView : MonoBehaviour
    {
        //外部提供字段
        GameState state;
        float hexSize;

        //独占字段
        

        public void Init(GameState state, float hexSize)
        {
            this.state = state;
            this.hexSize = hexSize;
        }

        private void Start()
        {
            //订阅建城事件
            EventCenter.Instance.AddListener<FoundCityEvent>(e =>
            {
                BuildCity(e.City);
            });
        }

        /// <summary>
        /// 创建城市边界：待定
        /// </summary>
        public void BuildCity(City city)
        {
            Debug.Log("已创建城市");
        }
    }
}

