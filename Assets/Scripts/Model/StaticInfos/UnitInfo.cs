using SparkAge.Model.Units;

namespace SparkAge.Model.StaticInfos
{
    public class UnitInfo
    {
        public UnitType Type;
        public string Name;

        public int Atk;
        public int Def;
        public int Hp;
        public int Movement;
        public int Cost;
        public UnitInfo(UnitType type, string name, int atk, int def, int hp, int movement, int cost)
        {
            Type = type;
            Name = name; 
            Atk = atk;
            Def = def; 
            Hp = hp;
            Movement = movement; 
            Cost = cost;
        }
    }

}