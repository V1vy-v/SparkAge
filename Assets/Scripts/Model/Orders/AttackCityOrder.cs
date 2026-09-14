using SparkAge.Model.Cities;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class AttackCityOrder : BaseOrder
    {
        public int AttackerID;
        public int CityID;

        public AttackCityOrder(int id, int attackerID, int cityID)
        {
            PlayerId = id;
            AttackerID = attackerID;
            CityID = cityID;
        }
    }
}
