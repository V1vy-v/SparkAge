using System;
using System.Collections.Generic;

namespace SparkAge.Core.Hex
{
    public static class Pathfinding
    {
        /// <summary>
        /// A* 寻路。moveCost 返回移动消耗，返回 -1 表示不可通行。
        /// 返回的路径包含起点之外的所有格子。
        /// </summary>
        public static List<HexCoord> FindPath(HexCoord start, HexCoord goal,
                                             Func<HexCoord, int> moveCost)
        {
            var open = new List<HexCoord> { start };
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            var g = new Dictionary<HexCoord, int> { [start] = 0 };
            var f = new Dictionary<HexCoord, int> { [start] = 0 };

            while (open.Count > 0)
            {
                // 地图小（几百格），线性找 f 最小即可；大图再换二叉堆
                int bestIdx = 0;
                for (int i = 1; i < open.Count; i++)
                    if (f[open[i]] < f[open[bestIdx]]) bestIdx = i;
                var cur = open[bestIdx];
                open.RemoveAt(bestIdx);

                if (cur.Equals(goal)) break;

                foreach (var dir in HexCoord.Directions)
                {
                    var next = cur + dir;
                    int cost = moveCost(next);
                    if (cost < 0) continue; // 不可通行（山、水）

                    int tentative = g[cur] + cost;
                    if (!g.TryGetValue(next, out int gNext)) gNext = int.MaxValue;
                    if (tentative < gNext)
                    {
                        cameFrom[next] = cur;
                        g[next] = tentative;
                        f[next] = tentative + next.DistanceTo(goal);
                        if (!open.Contains(next)) open.Add(next);
                    }
                }
            }

            var path = new List<HexCoord>();
            if (!cameFrom.ContainsKey(goal)) return path; // 无路可达
            for (var c = goal; !c.Equals(start); c = cameFrom[c]) path.Add(c);
            path.Reverse();
            return path;
        }
    }
}