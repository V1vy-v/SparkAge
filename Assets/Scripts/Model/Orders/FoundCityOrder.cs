using SparkAge.Model.Hex;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class FoundCityOrder : BaseOrder
    {
        public int UnitID;

        public FoundCityOrder(int id, int unitID)
        {
            PlayerId = id;
            UnitID = unitID;
        }
    }
}
