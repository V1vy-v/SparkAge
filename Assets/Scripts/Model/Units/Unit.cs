using SparkAge.Model.GameInfos;
using SparkAge.Model.Hex;

namespace SparkAge.Model.Units
{
    /// <summary>
    /// 单位类型
    /// </summary>
    public enum UnitType
    {
        Settler = 1001,
        Warrior = 1002
    }
    /// <summary>
    /// 单位类
    /// </summary>
    public class Unit
    {
        public int Owner;//所属玩家
        public UnitType Type;//单位类型
        public HexCoord Position;//位置

        public int Atk;//攻击力
        public int Def;//防御力
        public int Hp;//当前生命
        public int MaxHp;//最大生命
        public int MaxMovement;//最大移动力
        public int MovementLeft;//剩余移动力

        public Unit(int own, HexCoord position, UnitInfo info)
        {
            Owner = own;
            Position = position;
            Type = info.Type;

            Atk = info.Atk;
            Def = info.Def;
            Hp = MaxHp = info.Hp;
            MaxMovement = info.Movement;
            MovementLeft = 0;
        }
    }
}
