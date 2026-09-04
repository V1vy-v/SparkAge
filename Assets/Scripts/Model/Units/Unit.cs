using SparkAge.Model.Hex;

namespace SparkAge.Model.Units
{
    /// <summary>
    /// 单位类型
    /// </summary>
    public enum UnitType
    {
        Warrior,
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

        public Unit(UnitType type, HexCoord position, int own)
        {
            this.type = type;
            Position = position;
            this.own = own;
            switch (type)
            {
                case UnitType.Warrior:
                    MovementLeft = MaxMovement = 3;
                    break;
                case UnitType.Settler:
                    MovementLeft = MaxMovement = 4;
                    break;
            }
        }
    }
}
