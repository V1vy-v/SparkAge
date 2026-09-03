using SparkAge.Model.Hex;
using System.Collections.Generic;

public class FindPathTests
{
    // 5x5 全平原(代价1)；forest=代价2；blocked=不可通行(-1)
    static Dictionary<HexCoord, int> BuildMap(HashSet<HexCoord> blocked, HashSet<HexCoord> forest)
    {
        var map = new Dictionary<HexCoord, int>();
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                map[new HexCoord(i, j)] = 1;
        foreach (var h in forest) map[h] = 2;
        foreach (var h in blocked) map[h] = -1;
        return map;
    }

    
}