using SparkAge.Model.Hex;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class MoveUnitOrder : BaseOrder
    {
        public int UnitID;
        public HexCoord Target;

        public MoveUnitOrder(int unitID, HexCoord target)
        {
            UnitID = unitID;
            Target = target;
        }
    }
}
