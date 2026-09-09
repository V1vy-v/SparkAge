using SparkAge.Model.Units;
using UnityEngine;

namespace SparkAge.Config
{
    [CreateAssetMenu(fileName = "UnitInfo", menuName = "Config/Unit")]
    public class UnitCfg : ScriptableObject
    {
        [Header("基本信息")]
        public UnitType Type;
        public string Name;
        //public string Description;

        [Header("属性")]
        public int Atk;
        public int Def;
        public int Hp;
        public int Movement;
        public int Cost;

        [Header("引用")]
        public GameObject Prefab;
    }
}

