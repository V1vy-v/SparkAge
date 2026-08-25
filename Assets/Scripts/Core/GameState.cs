using SparkAge.Core.Hex;
using SparkAge.Core.Map;
using SparkAge.Core.Units;
using System.Collections.Generic;

namespace SparkAge.Core
{
    /// <summary>
    /// 游戏世界状态
    /// </summary>
    public class GameState
    {
        public MapData Map;//地图数据
        public List<Unit> Units;//所有单位数据

        public GameState(MapData map)
        {
            Map = map;
            Units = new List<Unit>();
        }

        //查询某个格子有没有单位
        public Unit GetUnitAt(HexCoord hexCoord)
        {
            foreach (var unit in Units)
            {
                if (unit.Position.Equals(hexCoord))
                    return unit;
            }
            return null;
        }
        /// <summary>
        /// BFS算法搜索第一个可以作为出生点的地块
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public HexCoord? FindSpawnPoint(HexCoord center)
        {
            Queue<HexCoord> queue = new();
            List<HexCoord> visited = new List<HexCoord> { center };
            queue.Enqueue(center);
            while (queue.Count > 0)
            {
                HexCoord curHex = queue.Dequeue();
                if (Map.Tiles[curHex].Walkable)
                    return curHex;

                for (int i = 0; i < 6; i++)
                {
                    HexCoord newHex = curHex.Neighbor(i);
                    if (Map.IsInMap(newHex) && !visited.Contains(newHex))
                    {
                        queue.Enqueue(newHex);
                        visited.Add(newHex);
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 根据单位位置和移动力使用扩散算法计算可到达点
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public Dictionary<HexCoord, int> GetReachableTiles(Unit unit)
        {
            Dictionary<HexCoord, int> movementLeftDic = new Dictionary<HexCoord, int>();

            movementLeftDic[unit.Position] = unit.MovementLeft;
            Queue<HexCoord> queue = new();
            queue.Enqueue(unit.Position);
            while (queue.Count > 0)
            {
                HexCoord curHex = queue.Dequeue();

                for (int i = 0; i < 6; i++)
                {
                    HexCoord newHex = curHex.Neighbor(i);
                    if (!Map.IsInMap(newHex)) continue;
                    int cost = Map.Tiles[newHex].MoveCost;
                    if (cost <= 0) continue;
                    int remaining = movementLeftDic[curHex] - cost;
                    if (remaining < 0) continue;
                    if (!movementLeftDic.TryGetValue(newHex, out int old) || remaining > old)
                    {
                        movementLeftDic[newHex] = remaining;
                        queue.Enqueue(newHex);
                    }
                }
            }
            return movementLeftDic;
        }

        public void MoveUnit(Unit unit, HexCoord tarHex)
        {

        }
    }
}
