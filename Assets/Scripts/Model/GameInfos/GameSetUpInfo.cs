using System.Collections.Generic;

namespace SparkAge.Model.GameInfos
{
    public class GameSetUpInfo
    {
        public int Seed;
        public int MapWidth;
        public int MapHeight;

        public List<SlotInfo> Slots;
        public GameSetUpInfo(int seed, int mapWidth, int mapHeight, List<SlotInfo> slots)
        {
            Seed = seed;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            Slots = slots;
        }
    }
}
