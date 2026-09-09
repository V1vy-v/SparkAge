using UnityEngine;

namespace SparkAge.Config
{
    [CreateAssetMenu(fileName = "CityInfo", menuName = "Config/City")]
    public class CityCfg : ScriptableObject
    {
        [Header("基本信息")]
        public string Name;

        [Header("属性")]
        public int Def;
        public int Hp;
        public int Radius;
        public int Production;

        [Header("引用")]
        public GameObject Prefab;
    }
}
