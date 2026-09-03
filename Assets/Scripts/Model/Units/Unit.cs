using SparkAge.Model.Hex;

namespace SparkAge.Model.Units
{
    /// <summary>
    /// 单位类型
    /// </summary>
    public enum UnitType
    {
        warrior,
        Settler
    }
    /// <summary>
    /// 单位数据
    /// </summary>
    public class Unit
    {
        public UnitType type;//单位类型
        public HexCoord Position;//位置
        public int own;//所属玩家
        public int MaxMovement;//最大移动力
        public int MovementLeft;//剩余移动力

        public Unit(UnitType type, HexCoord position, int own, int maxMovement, int movementLeft)
        {
            this.type = type;
            Position = position;
            this.own = own;
            MaxMovement = maxMovement;
            MovementLeft = movementLeft;
        }

    }
}
