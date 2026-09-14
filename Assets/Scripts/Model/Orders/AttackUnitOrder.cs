using SparkAge.Model.Hex;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class AttackUnitOrder : BaseOrder
    {
        public int AttackerID;
        public int DefenderID;

        public AttackUnitOrder(int id, int attackerID, int defenderID)
        {
            PlayerId = id;
            AttackerID = attackerID;
            DefenderID = defenderID;
        }
    }
}
