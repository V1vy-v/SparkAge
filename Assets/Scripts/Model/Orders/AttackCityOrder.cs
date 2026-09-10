using SparkAge.Model.Cities;
using SparkAge.Model.Units;

namespace SparkAge.Model.Orders
{
    public class AttackCityOrder : BaseOrder
    {
        public Unit Attacker;
        public City City;

        public AttackCityOrder(int id, Unit attacker, City city)
        {
            PlayerId = id;
            Attacker = attacker;
            City = city;
        }
    }
}
