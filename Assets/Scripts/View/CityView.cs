using ParrelSync.Update;
using SparkAge.Config;
using SparkAge.Controller.Network;
using SparkAge.Framework.EventCenter;
using SparkAge.Framework.Hex;
using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static SparkAge.Framework.EventCenter.EventDefine;

namespace SparkAge.View
{
    public class CityView : MonoBehaviour
    {
        [SerializeField] Transform mapRoot;
        //外部提供字段
        GameState state;
        float hexSize;

        //独占字段
        Dictionary <int, GameObject> cityObjs = new Dictionary<int, GameObject>();// 单位->游戏对象的映射
        public Dictionary<int, GameObject> CityObjs => cityObjs;
        Dictionary<int, List<GameObject>> cityPrefabs = new Dictionary<int, List<GameObject>>();
        Dictionary<int, GameObject> cityBorderRoots = new Dictionary<int, GameObject>();
        Dictionary<int, GameObject> borderPrefabs = new Dictionary<int, GameObject>();

        public void Init(GameState state, float hexSize)
        {
            this.state = state;
            this.hexSize = hexSize;

            LoadAllPrefabs();
            mapRoot = GameObject.Find("MapRoot").transform;

            EventCenter.Instance.AddListener<BuildCityEvent>(OnBuildCity);
            EventCenter.Instance.AddListener<UpdateCityEvent>(OnUpdateCity);
            EventCenter.Instance.AddListener<AttackCityCompletedEvent>(OnAttackCityCompleted);
        }
        private void OnDestroy()
        {
            EventCenter.Instance.RemoveListener<BuildCityEvent>(OnBuildCity);
            EventCenter.Instance.RemoveListener<UpdateCityEvent>(OnUpdateCity);
            EventCenter.Instance.RemoveListener<AttackCityCompletedEvent>(OnAttackCityCompleted);
        }

        public void OnBuildCity(BuildCityEvent e)
        {
            BuildCity(e.City);
        }
        public void OnUpdateCity(UpdateCityEvent e)
        {
            UpdateCity(e.City);
        }
        public void OnRemoveCity(RemoveCityEvent e)
        {
            RemoveCity(e.City);
        }
        public void OnAttackCityCompleted(AttackCityCompletedEvent e)
        {
            if(e.CityIsCapture)
                UpdateCity(e.City);
        }


        private void LoadAllPrefabs()
        {
            cityPrefabs[1] = new List<GameObject>(Resources.LoadAll<GameObject>("Prefabs/Cities/Red"));
            cityPrefabs[2] = new List<GameObject>(Resources.LoadAll<GameObject>("Prefabs/Cities/Blue"));
            cityPrefabs[3] = new List<GameObject>(Resources.LoadAll<GameObject>("Prefabs/Cities/Green"));
            cityPrefabs[4] = new List<GameObject>(Resources.LoadAll<GameObject>("Prefabs/Cities/Yellow"));

            borderPrefabs[1] = Resources.Load<GameObject>("Prefabs/Cities/Border/Border1");
            borderPrefabs[2] = Resources.Load<GameObject>("Prefabs/Cities/Border/Border2");
            borderPrefabs[3] = Resources.Load<GameObject>("Prefabs/Cities/Border/Border3");
            borderPrefabs[4] = Resources.Load<GameObject>("Prefabs/Cities/Border/Border4");
        }
        /// <summary>
        /// 创建城市与边界
        /// </summary>
        public GameObject BuildCity(City city)
        {
            GameObject obj = Instantiate(cityPrefabs[city.Owner][(state.CityCount(city.Owner) - 1) % 4]);
            obj.transform.position = HexLayout.HexToPixel(city.Position, hexSize, 0.2f);
            cityObjs[city.ID] = obj;

            //显示名字
            TextMeshPro name = obj.GetComponentInChildren<TextMeshPro>();
            name.SetText(city.Name);
            name.color = ViewTools.GetPlayerColor(city.Owner);
            //创建城市边界对象
            CreateCityBorder(city);
            return obj;
        }
        /// <summary>
        /// 更新城市
        /// </summary>
        /// <param name="city"></param>
        public void UpdateCity(City city)
        {
            if (!cityObjs.TryGetValue(city.ID, out GameObject obj))
                return;
            //更新城市名字颜色
            TextMeshPro name = obj.GetComponentInChildren<TextMeshPro>();
            name.color = ViewTools.GetPlayerColor(city.Owner);
            //更新城市边界对象
            if(cityBorderRoots.TryGetValue(city.ID, out GameObject root))
            {
                Destroy(root); 
                CreateCityBorder(city);
            }
        }
        private void CreateCityBorder(City city)
        {
            GameObject obj = new GameObject("BorderRoot");
            obj.transform.SetParent(mapRoot.transform, false);
            cityBorderRoots[city.ID] = obj;
            List<HexCoord> borders = city.Position.GetRange(city.Radius);
            foreach (var hex in borders)
            {
                GameObject border = Instantiate(borderPrefabs[city.Owner], obj.transform);
                border.transform.position = HexLayout.HexToPixel(hex, hexSize, 0.25f);
            }
        }
        public void RemoveCity(City city)
        {

        }
    }
}

