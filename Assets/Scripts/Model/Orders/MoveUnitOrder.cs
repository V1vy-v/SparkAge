using SparkAge.Model.Hex;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class MoveUnitOrder : BaseOrder
    {
        public Unit Unit;
        public HexCoord Target;

        public MoveUnitOrder(int id, Unit unit, HexCoord target)
        {
            PlayerId = id;
            Unit = unit;
            Target = target;
        }
    }
}
