using SparkAge.Model.Cities;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class BuildUnitOrder : BaseOrder
    {
        public City City;
        public UnitType Type;

        public BuildUnitOrder(int id, City city, UnitType type)
        {
            PlayerId = id;
            City = city;
            Type = type;
        }
    }
}
