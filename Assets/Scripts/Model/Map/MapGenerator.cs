using SparkAge.Model.Hex;
using System;

namespace SparkAge.Model.Map
{
    /// <summary>
    /// 根据宽高和Seed生成一张带地形的地图，纯C#
    /// </summary>
    public static class MapGenerator
    {

        public static MapData Generate(int width, int height, int seed)
        {
            MapData map = new MapData(width, height);
            ValueNoise.Generate(seed, (int)MathF.Min(width / 3, height / 3));

            //随机地形
            foreach(var tile in map.Tiles)
            {
                //每个格子的随机小数
                var n = ValueNoise.Sample(tile.Key.Q, tile.Key.R, width, height);
                if (n >= 0 && n < 0.25)
                    tile.Value.Type = TerrainType.Water;
                else if (n >= 0.35 && n <= 0.65)
                    tile.Value.Type = TerrainType.Plain;
                else if (n >= 0.25 && n < 0.35 || n > 0.65 && n <= 0.75)
                    tile.Value.Type = TerrainType.Forest;
                else if (n > 0.75 && n <= 1)
                    tile.Value.Type = TerrainType.Mountain;

                //边缘全为水域
                if (tile.Key.Q == 0 || tile.Key.Q == width - 1 || tile.Key.R == 0 || tile.Key.R == height - 1)
                    tile.Value.Type = TerrainType.Water;
            }
            return map;
        }
    }
}
