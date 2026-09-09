using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.Config
{
    [CreateAssetMenu(fileName = "GameCfg", menuName = "Config/GameCfg")]
    public class GameCfg : ScriptableObject
    {
        public List<UnitCfg> unitCfgs = new List<UnitCfg>();
        public List<CityCfg> cityCfgs = new List<CityCfg>();
    }
}
    
