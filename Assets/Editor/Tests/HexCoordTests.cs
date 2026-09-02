using NUnit.Framework;
using SparkAge.Core.Hex;

public class HexCoordTests
{
    [Test]
    public void Distance_IsSymmetric()
    {
        var a = new HexCoord(3, -2);
        var b = new HexCoord(-1, 4);
        Assert.AreEqual(a.DistanceTo(b), b.DistanceTo(a));
    }

    [Test]
    public void SixNeighbors_AllDistanceOne()
    {
        var center = new HexCoord(0, 0);
        for (int i = 0; i < 6; i++)
            Assert.AreEqual(1, center.DistanceTo(center.Neighbor(i)));
    }

    [Test]
    public void PixelRoundTrip_ReturnsSameHex()
    {
        var h = new HexCoord(2, -3);
        var p = HexLayout.HexToPixel(h, 1f, 0.5f);
        Assert.AreEqual(h, HexLayout.PixelToHex(p, 1f));
    }
}