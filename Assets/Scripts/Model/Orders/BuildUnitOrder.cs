using SparkAge.Model.Cities;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class BuildUnitOrder : BaseOrder
    {
        public int CityID;
        public UnitType Type;

        public BuildUnitOrder(int cityID, UnitType type)
        {
            CityID = cityID;
            Type = type;
        }
    }
}
