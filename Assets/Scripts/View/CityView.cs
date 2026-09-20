using ParrelSync.Update;
using SparkAge.Config;
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
            EventCenter.Instance.AddListener<AttackCityEvent>(e =>
            {
                if (e.CityIsCapture)
                    UpdateCity(e.City);
            });
        }

        /// <summary>
        /// 创建城市与边界
        /// </summary>
        public GameObject BuildCity(City city)
        {
            GameObject obj = Instantiate(ConfigMgr.Instance.cityCfgs[0].Prefab);

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
        /// <summary>
        /// 更新城市
        /// </summary>
        /// <param name="city"></param>
        public void UpdateCity(City city)
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

