using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using System.Collections.Generic;
using UnityEditor;
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
        }

        /// <summary>
        /// 创建城市边界：待定
        /// </summary>
        public GameObject BuildCity(City city)
        {
            GameObject obj = new GameObject();
            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.mesh = cityMesh;
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = Color.gray
            };

            cityObjs[city] = obj;
            obj.transform.position = HexLayout.HexToPixel(city.Position, hexSize, 0.3f);
            return obj;
        }
    }
}

