using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.Config
{
    [CreateAssetMenu(fileName = "MapCfg", menuName = "Config/Map")]
    public class MapCfg : ScriptableObject
    {
        [Header("基本信息")]
        public int Id;
        public string Name;

        [Header("地图信息")]
        public int MapWidth;
        public int MapHeight;
    }
}
