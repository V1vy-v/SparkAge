using SparkAge.Model.Cities;
using SparkAge.Model.Units;

namespace SparkAge.Framework.EventCenter
{
    public class EventDefine
    {
        public class FoundCityEvent
        {
            public City City;
            public Unit ConsumedSettler;
            public FoundCityEvent(City city,Unit settler)
            {
                City = city;
                ConsumedSettler = settler;
            }
        }
    }
}
