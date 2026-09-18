using SparkAge.Controller.Network;
using SparkAge.Model.StaticInfos;

namespace SparkAge.Model.Players
{
    public class PlayerState
    {
        public int ID;
        public string Name;
        public CharacterInfo CharacterInfo;
        public bool IsAlive; // 是否存活

        public PlayerState(int id, string name, CharacterInfo characterInfo, bool isAlive = true)
        {
            ID = id;
            Name = name;
            CharacterInfo = characterInfo;
            IsAlive = isAlive;
        }
        public void UpdateState(PlayerData data)
        {
            IsAlive = data.IsAlive;
        } 
    }
}
