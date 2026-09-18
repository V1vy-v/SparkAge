
using SparkAge.Controller.Network;
using SparkAge.Model.Hex;
using SparkAge.Model.StaticInfos;

namespace SparkAge.Model.Cities
{
    /// <summary>
    /// 城市类
    /// </summary>
    public class City
    {
        public int ID;//唯一ID
        public int Owner;//所属玩家
        public string Name;//名字
        public HexCoord Position;//位置

        public int Radius;//半径
        public int Production;//当前生产力
        public int Hp;//当前血量
        public int MaxHp;//血量上限
        public int Def;//防御力

        public City(int id, int owner, string name, HexCoord position, CityInfo info)
        {
            ID = id;
            Owner = owner;
            Name = name;
            Position = position;

            Radius = info.Radius;
            Production = info.Production;
            Hp = MaxHp = info.Hp;
            Def = info.Def;
        }
        public void UpdateProperty(CityData data)
        {
            Owner = data.Owner;
            Position = data.Position;
            Hp = data.Hp;
            Production = data.Production;
        }
    }
}
