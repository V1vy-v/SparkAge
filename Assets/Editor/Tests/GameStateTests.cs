using NUnit.Framework;
using SparkAge.Core;
using SparkAge.Core.Hex;
using SparkAge.Core.Map;

public class GameStateTests
{
    static SparkAge.Core.Units.Unit UnitAt(HexCoord pos, int movement) => new(pos, 0, movement, movement);

    static GameState MakeMap(params (HexCoord c, TerrainType t)[] tiles)
    {
        var map = new MapData(5, 5);
        foreach (var (c, t) in tiles) map.Tiles[c].Type = t;
        return new GameState(map);
    }

    [Test]
    public void Plains_Movement2_ReachesAllWithinDistance2()
    {
        var state = MakeMap();
        HexCoord start = new HexCoord(2, 2);
        var res = state.GetReachableTiles(UnitAt(start, 2));
        Assert.AreEqual(19, res.Count);
    }

    [Test]
    public void Mountain_BlocksReach()
    {
        var state = MakeMap((new HexCoord(3, 2), TerrainType.Mountain));
        var res = state.GetReachableTiles(UnitAt(new HexCoord(2, 2), 2));
        Assert.IsFalse(res.Contains(new HexCoord(3, 2)));   // 山本身不可达
        Assert.IsFalse(res.Contains(new HexCoord(4, 2)));   // 山后面也不可达
    }
}