using SparkAge.Model.Hex;
using System.Collections.Generic;

namespace SparkAge.Model.Map
{
    /// <summary>
    /// 地图数据类
    /// </summary>
    public class MapData
    {
        //地图宽高
        public int Width;
        public int Height;
        //格子=>数据的映射
        public Dictionary<HexCoord, TileData> Tiles = new Dictionary<HexCoord, TileData>();
        //判断格子在不在地图内
        public bool IsInMap(HexCoord hexCoord) => Tiles.ContainsKey(hexCoord);
        //地图中心地块
        public HexCoord Center => new HexCoord(Width / 2, Height / 2);
        //地图初始化
        public MapData(int width, int height)
        {
            Width = width;
            Height = height;

            HexCoord hexCoord;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    hexCoord = new HexCoord(i, j);
                    Tiles.Add(hexCoord, new TileData { Coord = hexCoord, Type = TerrainType.Plain });
                }
            }
        }
    }
}
