using SparkAge.Model.Hex;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class AttackUnitOrder : BaseOrder
    {
        public int AttackerID;
        public int DefenderID;

        public AttackUnitOrder(int attackerID, int defenderID)
        {
            AttackerID = attackerID;
            DefenderID = defenderID;
        }
    }
}
