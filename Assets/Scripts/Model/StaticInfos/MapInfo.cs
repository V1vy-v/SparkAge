

namespace SparkAge.Model.StaticInfos
{
    public class MapInfo
    {
        public int Id;
        public int Seed;
        public int MapWidth;
        public int MapHeight;

        public MapInfo(int id, int seed, int mapWidth, int mapHeight)
        {
            Id = id;
            Seed = seed;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
        }
    }
}
