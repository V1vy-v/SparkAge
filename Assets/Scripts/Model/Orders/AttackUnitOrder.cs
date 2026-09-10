using SparkAge.Model.Hex;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class AttackUnitOrder : BaseOrder
    {
        public Unit Attacker;
        public Unit Defender;

        public AttackUnitOrder(int id, Unit attacker, Unit defender)
        {
            PlayerId = id;
            Attacker = attacker;
            Defender = defender;
        }
    }
}
