using SparkAge.Controller;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Units;
using System.Collections.Generic;

namespace SparkAge.Framework.EventCenter
{
    public class EventDefine
    {
        public class MoveUnitEvent
        {
            public Unit Unit;
            public MoveUnitEvent(Unit unit)
            {
                Unit = unit;
            }
        }
        public class BuildUnitEvent
        {
            public City City;
            public BuildUnitEvent(City city)
            {
                City = city;
            }
        }
        public class FoundCityEvent
        {
            public City City;
            public FoundCityEvent(City city)
            {
                City = city;
            }
        }
        public class AttackUnitEvent
        {
            public Unit Attacker;
            public Unit Defender;
            public AttackUnitEvent(Unit attacker, Unit defender)
            {
                Attacker = attacker;
                Defender = defender;
            }
        }
        public class AttackCityEvent
        {
            public Unit Attacker;
            public City City;
            public bool CityIsCapture;
            public AttackCityEvent(Unit attacker, City city, bool cityIsCapture)
            {
                Attacker = attacker;
                City = city;
                CityIsCapture = cityIsCapture;
            }
        }

    }
}