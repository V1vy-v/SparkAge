

namespace SparkAge.Model.GameInfos
{
    public class CityInfo
    {
        public string Name;

        public int Hp;
        public int Def;
        public int Radius;
        public int Production;
        public CityInfo(string name, int hp, int def, int radius, int production)
        {
            Name = name;
            Def = def;
            Hp = hp;
            Radius = radius;
            Production = production;
        }
    }
}
