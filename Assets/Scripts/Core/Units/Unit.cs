using SparkAge.Core.Hex;

namespace SparkAge.Core.Units
{
    /// <summary>
    /// 单位数据
    /// </summary>
    public class Unit
    {
        public HexCoord Position;//位置
        public int own;//所属玩家
        public int MaxMovement;//最大移动力
        public int MovementLeft;//剩余移动力

        public Unit(HexCoord position, int own, int maxMovement, int movementLeft)
        {
            Position = position;
            this.own = own;
            MaxMovement = maxMovement;
            MovementLeft = movementLeft;
        }
    }
}
