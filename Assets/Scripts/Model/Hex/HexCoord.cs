using System;

namespace SparkAge.Model.Hex
{
    /// <summary>轴向坐标 (q, r)，配合 cube 思维：x=q, z=r, y=-q-r</summary>
    [Serializable]
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public readonly int Q;
        public readonly int R;

        public HexCoord(int q, int r) { Q = q; R = r; }

        public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);
        public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);
        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoord o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(Q, R);

        // 六个邻居方向
        public static readonly HexCoord[] Directions =
        {
            new( 1,  0), new( 1, -1), new( 0, -1),
            new(-1,  0), new(-1,  1), new( 0,  1),
        };

        public HexCoord Neighbor(int dir) => this + Directions[((dir % 6) + 6) % 6];

        // 距离公式：max(|Δq|, |Δr|, |Δq+Δr|)
        public int DistanceTo(HexCoord other)
        {
            int dq = Math.Abs(Q - other.Q);
            int dr = Math.Abs(R - other.R);
            int ds = Math.Abs((Q + R) - (other.Q + other.R));
            return Math.Max(dq, Math.Max(dr, ds));
        }
    }
}