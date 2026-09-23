using System;
using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.Config
{
    [CreateAssetMenu(fileName = "CharacterCfg", menuName = "Config/Character")]
    public class CharacterCfg : ScriptableObject
    {
        [Header("基本信息")]
        public int Id;
        public string Name;
        public string Description;

        [Header("特性")]
        public List<string> CityNames;
        public int WarriorAtkBonus;
        public int WarriorDefBonus;
        public int WarriorHpBonus;
        public int CityHpBonus;
        public int CityDefBonus;
        public int CityProductionBonus;

        [Header("引用")]
        public GameObject Prefab;
    }
}
