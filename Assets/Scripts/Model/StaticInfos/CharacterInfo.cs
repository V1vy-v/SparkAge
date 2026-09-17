using System.Collections.Generic;

namespace SparkAge.Model.StaticInfos
{
    public class CharacterInfo
    {
        public int Id;
        public string Name;
        public string Description;
        public List<string> CityNames;
        public CharacterInfo(int id, string name, string description, List<string> cityNames)
        {
            Id = id;
            Name = name;
            Description = description;
            CityNames = cityNames;
        }
    }

}