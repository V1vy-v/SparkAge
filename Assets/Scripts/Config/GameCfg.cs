using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.Config
{
    [CreateAssetMenu(fileName = "GameInfo", menuName = "Config/GameCfg")]
    public class GameCfg : ScriptableObject
    {
        public List<UnitCfg> unitCfgs = new List<UnitCfg>();
        public List<CityCfg> cityCfgs = new List<CityCfg>();
        public GameSetUpCfg gameSetUpCfg;
    }
}
    
