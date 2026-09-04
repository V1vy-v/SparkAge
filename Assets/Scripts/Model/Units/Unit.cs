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
        public int own;//所属玩家
        public UnitType type;//单位类型
        public HexCoord Position;//位置

        public int Atk;//攻击力
        public int Def;//防御力
        public int Hp;//当前生命
        public int MaxHp;//最大生命
        public int MaxMovement;//最大移动力
        public int MovementLeft;//剩余移动力

        public Unit(int own, UnitType type, HexCoord position)
        {
            this.type = type;
            Position = position;
            this.own = own;
            switch (type)
            {
                case UnitType.Warrior:
                    Atk = GameRules.WarriorAtk;
                    Def = GameRules.WarriorDef;
                    Hp = MaxHp = GameRules.WarriorMaxHp;
                    MovementLeft = MaxMovement = GameRules.WarriorMaxMovement;
                    break;
                case UnitType.Settler:
                    Atk = GameRules.SettlerAtk;
                    Def = GameRules.SettlerDef;
                    Hp = MaxHp = GameRules.SettlerMaxHp;
                    MovementLeft = MaxMovement = GameRules.SettlerMaxMovement;
                    break;
            }
        }
    }
}
