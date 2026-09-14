using SparkAge.Model.Units;
using System.Collections.Generic;

namespace SparkAge.Model.GameInfos
{
    public class GameInfo
    {
        //角色配置

        //单位配置
        public Dictionary<UnitType, UnitInfo> UnitInfos = new Dictionary<UnitType, UnitInfo>();
        //城市配置
        public List<CityInfo> CityInfos = new List<CityInfo>();
        //局配置
        public GameSetUpInfo GameSetUpInfo;
    }
}
