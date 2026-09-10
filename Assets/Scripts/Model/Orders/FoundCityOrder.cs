using SparkAge.Model.Hex;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class FoundCityOrder : BaseOrder
    {
        public Unit Unit;

        public FoundCityOrder(int id, Unit unit)
        {
            PlayerId = id;
            Unit = unit;
        }
    }
}
