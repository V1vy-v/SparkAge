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
        public class AttackUnitEvent
        {
            public Unit Attacker;
            public bool AttackerIsDead;
            public AttackUnitEvent(Unit attacker, bool attackerIsDead)
            {
                Attacker = attacker;
                AttackerIsDead = attackerIsDead;
            }
        }
        public class AttackCityEvent
        {
            public Unit Attacker;
            public City AttackedCity;
            public bool CityIsCapture;
            public bool DefenderIsDead;
            public AttackCityEvent(Unit attacker, City city, bool cityIsCapture,bool defenderIsDead)
            {
                Attacker= attacker;
                AttackedCity = city;
                CityIsCapture = cityIsCapture;
                DefenderIsDead = defenderIsDead;
            }
        }
    }
}
