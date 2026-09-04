using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Units;
using System.Collections.Generic;

namespace SparkAge.Framework.EventCenter
{
    public class EventDefine
    {
        public class BuildUnitEvent
        {
            public City City;
            public Unit BuiltUnit;
            public BuildUnitEvent(City city, Unit builtUnit)
            {
                City = city;
                BuiltUnit = builtUnit;
            }
        }
        public class UnitMoveEvent
        {
            public Unit unit;
            public List<HexCoord> path;
            public bool isMoving;
            public UnitMoveEvent(Unit unit, List<HexCoord> path, bool isMoving)
            {
                this.unit = unit;
                this.path = path;
                this.isMoving = isMoving;
            }
        }
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
