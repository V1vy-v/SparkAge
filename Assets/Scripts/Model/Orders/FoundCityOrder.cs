using SparkAge.Model.Hex;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class FoundCityOrder : BaseOrder
    {
        public int UnitID;

        public FoundCityOrder(int unitID)
        {
            UnitID = unitID;
        }
    }
}
