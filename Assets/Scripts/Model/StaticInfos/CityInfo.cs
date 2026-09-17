

namespace SparkAge.Model.StaticInfos
{
    public class CityInfo
    {
        public int Id;
        public int Hp;
        public int Def;
        public int Radius;
        public int Production;
        public CityInfo(int id, int hp, int def, int radius, int production)
        {
            Id = id;
            Def = def;
            Hp = hp;
            Radius = radius;
            Production = production;
        }
    }
}
