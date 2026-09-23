using SparkAge.Model.StaticInfos;
using System.Collections.Generic;

namespace SparkAge.Model
{
    public class GameInfo
    {
        public List<PlayerInfo> PlayerInfos = new List<PlayerInfo>();
        public MapInfo MapInfo;
        public PlayerInfo GetPlayerInfo(int id)
        {
            foreach(var player in PlayerInfos) 
                if(player.Id == id) 
                    return player;
            return null;
        }
        public string GetPlayerCharacterName(int id) => GetPlayerInfo(id).CharacterInfo.Name;
    }
    public class PlayerInfo
    {
        public int Id;
        public string Name;
        public CharacterInfo CharacterInfo;
        public bool IsAi;
    }
}
