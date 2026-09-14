using SparkAge.Model.Cities;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class AttackCityOrder : BaseOrder
    {
        public int AttackerID;
        public int CityID;

        public AttackCityOrder(int attackerID, int cityID)
        {
            AttackerID = attackerID;
            CityID = cityID;
        }
    }
}
