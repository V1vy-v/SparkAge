using SparkAge.Controller.Network;
using SparkAge.Model.Hex;
using SparkAge.Model.StaticInfos;

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
        public int ID;//唯一ID
        public int Owner;//所属玩家
        public HexCoord Position;//位置
        public UnitType Type;//单位类型

        public string Name;//名字
        public int Atk;//攻击力
        public int Def;//防御力
        public int Hp;//当前生命
        public int MaxHp;//最大生命
        public int MaxMovement;//最大移动力
        public int MovementLeft;//剩余移动力
        public bool IsDead;

        public Unit(int id, int own, HexCoord position, UnitInfo info)
        {
            ID = id;
            Owner = own;
            Position = position;
            Type = info.Type;

            Name= info.Name;
            Atk = info.Atk;
            Def = info.Def;
            Hp = MaxHp = info.Hp;
            MovementLeft = MaxMovement = info.Movement;
            IsDead = false;
        }
        public void UpdateProperty(UnitData data)
        {
            Owner= data.Owner;
            Position = data.Position;
            Hp = data.Hp;
            MovementLeft= data.MovementLeft;
            IsDead= data.IsDead;
        }
    }
}
