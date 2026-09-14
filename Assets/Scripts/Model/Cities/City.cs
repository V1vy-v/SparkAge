using SparkAge.Model.GameInfos;
using SparkAge.Model.Hex;
using System.Xml.Linq;

namespace SparkAge.Model.Cities
{
    /// <summary>
    /// 城市类
    /// </summary>
    public class City
    {
        public int ID;//唯一ID
        public int Owner;//所属玩家
        public HexCoord Position;//位置

        public string Name;//名字
        public int Radius;//半径
        public int Production;//当前生产力
        public int Hp;//当前血量
        public int MaxHp;//血量上限
        public int Def;//防御力

        public City(int owner, HexCoord position, CityInfo info)
        {
            Owner = owner;
            Position = position;

            Name = info.Name;
            Radius = info.Radius;
            Production = info.Production;
            Hp = MaxHp = info.Hp;
            Def = info.Def;
        }
    }
}
