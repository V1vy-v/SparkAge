using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using System.Collections.Generic;
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
        Mesh cityMesh;
        Dictionary <City, GameObject> cityObjs = new Dictionary<City, GameObject>();// 单位->游戏对象的映射
        public Dictionary<City, GameObject> CityObjs => cityObjs;


        public void Init(GameState state, float hexSize)
        {
            this.state = state;
            this.hexSize = hexSize;

            cityMesh = HexMeshFactory.CreateHexMesh(0.7f * hexSize);
        }

        private void Start()
        {
            //订阅建城事件
            EventCenter.Instance.AddListener<FoundCityEvent>(e =>
            {
                BuildCity(e.City);
            });
            EventCenter.Instance.AddListener<AttackCityEvent>(e =>
            {
                if (e.CityIsCapture)
                {
                    //更新城市边界和标识颜色
                    UpadateCityColor(e.AttackedCity);
                }
            });
        }

        /// <summary>
        /// 创建城市边界：待定
        /// </summary>
        public GameObject BuildCity(City city)
        {
            GameObject obj = Instantiate(Resources.Load<GameObject>("Prefabs/City"));

            obj.transform.Find("Marker").GetComponent<MeshRenderer>().material =
                new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = ViewTools.GetPlayerColor(city.Owner)
            };
            obj.transform.position = HexLayout.HexToPixel(city.Position, hexSize, 0.3f);

            //创建城市边界对象

            cityObjs[city] = obj;
            return obj;
        }
        public void DestroyCity(City city)
        {
            //销毁城市及边界对象

        }
        /// <summary>
        /// 更新城市颜色
        /// </summary>
        /// <param name="city"></param>
        public void UpadateCityColor(City city)
        {
            cityObjs[city].transform.Find("Marker").GetComponent<MeshRenderer>().material =
                new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = ViewTools.GetPlayerColor(city.Owner)
                };

            //更新城市边界对象

        }
    }
}

