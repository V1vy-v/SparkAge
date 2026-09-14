using SparkAge.Model.Cities;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class BuildUnitOrder : BaseOrder
    {
        public int CityID;
        public UnitType Type;

        public BuildUnitOrder(int id, int cityID, UnitType type)
        {
            PlayerId = id;
            CityID = cityID;
            Type = type;
        }
    }
}
