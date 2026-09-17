using SparkAge.Model.StaticInfos;
using System.Collections.Generic;

namespace SparkAge.Model
{
    public class GameInfo
    {
        public List<PlayerInfo> PlayerInfos = new List<PlayerInfo>();
        public MapInfo MapInfo;
    }
    public class PlayerInfo
    {
        public int Id;
        public string Name;
        public CharacterInfo CharacterInfo;
    }
}
