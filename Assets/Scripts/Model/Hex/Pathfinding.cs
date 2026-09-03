using System;
using System.Collections.Generic;

namespace SparkAge.Model.Hex
{
    public readonly struct PathResult
    {
        public readonly bool Found;
        public readonly List<HexCoord> Path; // 不含起点（或约定含，二选一）
        public readonly int Cost;            // 总移动力消耗，顺带能拿
        public PathResult(bool found, List<HexCoord> path, int cost)
        {
            Found = found;
            Path = path;
            Cost = cost;
        }
    }

    public static class Pathfinding
    {
        /// <summary>
        /// A* 寻路 moveCost 返回移动消耗，返回 -1 表示不可通行。
        /// 返回的路径包含起点之外的所有格子。
        /// </summary>
        public static PathResult FindPath(HexCoord start, HexCoord goal, Func<HexCoord, int> moveCost)
        {
            bool found = false; //声明结果

            var cameFrom = new Dictionary<HexCoord, HexCoord>();    // 每个格子的"从哪来"
            var g = new Dictionary<HexCoord, int> { [start] = 0 };  // 累计代价
            var closed = new HashSet<HexCoord>();                   // 已确认的点

            var pq = new PriorityQueue<HexCoord, int>();
            pq.Enqueue(start, start.DistanceTo(goal));      // f[start] = g+h

            while (pq.Count > 0)
            {
                var cur = pq.Dequeue();

                //过期直接弹出
                if (closed.Contains(cur))
                    continue;
                closed.Add(cur);

                //已到达目标的点，结束
                if (cur.Equals(goal))
                {
                    found = true;
                    break;
                }

                //计算当前点能到达地块，若代价更小，则入队列
                for (int i = 0; i < 6; i++) 
                {
                    HexCoord newHex = cur.Neighbor(i);

                    //不可到达地块
                    int cost = moveCost(newHex);
                    if (cost < 0) continue;

                    int newCost = g[cur] + cost;
                    if (g.TryGetValue(newHex, out int oldCost) && newCost >= oldCost)
                        continue;  // 松弛：非改进跳过

                    g[newHex] = newCost;
                    pq.Enqueue(newHex, newCost + newHex.DistanceTo(goal));
                    cameFrom[newHex] = cur;
                }
            }

            if (!found) return new PathResult(false, new List<HexCoord>(), 0);//未找到路径

            //根据cameFrom求路径
            List<HexCoord> path = new List<HexCoord>();
            for (var c = goal; !c.Equals(start); c = cameFrom[c])
                path.Add(c);
            path.Reverse();

            return new PathResult(true, path, g[goal]);
        }
    }
}