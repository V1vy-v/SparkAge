

namespace SparkAge.Model
{
    public static class GameRules
    {
        //public const int MaxCitiesPerPlayer = 5;//每个玩家城市上限
        public const int CityProductionPerTurn = 5;//每回合生产产出


        public const int CityRadius = 2;//城市初始半径
        public const int CityProduction = 0;//城市初始生产力
        public const int CityMaxHp = 30;//城市初始血量
        public const int CityDef = 5;//城市初始防御力


        public const int WarriorCost = 5;//勇士造价
        public const int WarriorAtk = 4;//勇士攻击力
        public const int WarriorDef = 1;//勇士防御力
        public const int WarriorMaxHp = 10;//勇士生命值
        public const int WarriorMaxMovement = 2;//勇士移动力


        public const int SettlerCost = 10;//移民造价
        public const int SettlerAtk = 0;//移民攻击力
        public const int SettlerDef = 0;//移民防御力
        public const int SettlerMaxHp = 1;//移民生命值
        public const int SettlerMaxMovement = 3;//移民移动力
    }
}
