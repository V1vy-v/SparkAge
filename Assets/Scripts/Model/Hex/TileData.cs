using System;

namespace SparkAge.Model.Hex
{
    public enum TerrainType { Plain, Forest, Mountain, Water }

    [Serializable]
    public class TileData
    {
        public HexCoord Coord;
        public TerrainType Type;

        public int MoveCost => Type switch
        {
            TerrainType.Plain => 1,
            TerrainType.Forest => 2,
            _ => -1, // 山/水不可通行
        };

        public bool Walkable => MoveCost > 0;
    }
}