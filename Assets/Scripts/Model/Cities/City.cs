using SparkAge.Model.Hex;

namespace SparkAge.Model.Cities
{
    /// <summary>
    /// 城市类
    /// </summary>
    public class City
    {
        public int Owner;//所属玩家
        public HexCoord Position;//位置

        public int Radius;//半径
        public int Production;//当前生产力
        public int Hp;//当前血量
        public int MaxHp;//血量上限
        public int Def;//防御力

        public City(int owner, HexCoord position)
        {
            Owner = owner;
            Position = position;
            Radius = GameRules.CityRadius;
            Production = GameRules.CityProduction;
            Hp = MaxHp = GameRules.CityMaxHp;
            Def = GameRules.CityDef;
        }
    }
}
