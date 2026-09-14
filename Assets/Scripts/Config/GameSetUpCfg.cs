using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.Config
{
    [CreateAssetMenu(fileName = "GameSetUpInfo", menuName = "Config/GameSetUp")]
    public class GameSetUpCfg : ScriptableObject
    {
        [Header("地图信息")]
        public int Seed;
        public int MapWidth;
        public int MapHeight;

        [Header("槽位设置")]
        public List<SlotCfg> Slots;
    }
}
