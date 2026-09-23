using System.Collections.Generic;

namespace SparkAge.Model.StaticInfos
{
    public class CharacterInfo
    {
        public int Id;
        public string Name;
        public string Description;
        public List<string> CityNames;
        public int WarriorAtkBonus;
        public int WarriorDefBonus;
        public int WarriorHpBonus;
        public int CityHpBonus;
        public int CityDefBonus;
        public int CityProductionBonus;
        public CharacterInfo(
            int id, string name, string description, List<string> cityNames, 
            int warriorAtkBonurs, int warriorDefBonurs, int warriorHpBonurs,
            int cityHpBonus, int cityDefBonus, int cityProductionBonus)
        {
            Id = id;
            Name = name;
            Description = description;
            CityNames = cityNames;
            WarriorAtkBonus = warriorAtkBonurs;
            WarriorDefBonus = warriorDefBonurs;
            WarriorHpBonus = warriorHpBonurs;
            CityHpBonus = cityHpBonus;
            CityDefBonus = cityDefBonus;
            CityProductionBonus = cityProductionBonus;
        }
    }

}