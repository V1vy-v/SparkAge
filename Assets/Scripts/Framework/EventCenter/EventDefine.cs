using SparkAge.Controller;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Units;
using System.Collections.Generic;

namespace SparkAge.Framework.EventCenter
{
    public class EventDefine
    {
        public class InitialSettlers
        {
            public List<Unit> Settlers;
            public InitialSettlers(List<Unit> settlers)
            {
                Settlers = settlers;
            }
        }
        public class SelectionClearEvent
        {

        }
        public class MoveUnitStartEvent
        {
            public Unit Unit;
            public List<HexCoord> Path;
            public MoveUnitStartEvent(Unit unit, List<HexCoord> path)
            {
                Unit = unit;
                Path = path;
            }
        }
        public class MoveUnitCompletedEvent
        {
            public Unit Unit;
            public MoveUnitCompletedEvent(Unit unit)
            {
                Unit = unit;
            }
        }
        public class RemoveUnitEvent
        {
            public Unit Unit;
            public RemoveUnitEvent(Unit unit)
            {
                Unit = unit;
            }
        }
        public class UpdateUnitEvent
        {
            public Unit Unit;
            public UpdateUnitEvent(Unit unit)
            {
                Unit = unit;
            }
        }
        public class BuildUnitEvent
        {
            public Unit Unit;
            public BuildUnitEvent(Unit unit)
            {
                Unit = unit;
            }
        }
        public class RemoveCityEvent
        {
            public City City;
            public RemoveCityEvent(City city)
            {
                City = city;
            }
        }
        public class UpdateCityEvent
        {
            public City City;
            public UpdateCityEvent(City city)
            {
                City = city;
            }
        }
        public class BuildCityEvent
        {
            public City City;
            public BuildCityEvent(City city)
            {
                City = city;
            }
        }
        public class AttackUnitStartEvent
        {
            public Unit Attacker;
            public Unit Defender;
            public List<HexCoord> Path;
            public bool CanEnter;
            public AttackUnitStartEvent(Unit attacker, Unit defender, List<HexCoord> path, bool canEnter)
            {
                Attacker = attacker;
                Defender = defender;
                Path = path;
                CanEnter = canEnter;
            }
        }
        public class AttackUnitCompletedEvent
        {
            public Unit Attacker;
            public Unit Defender;
            public AttackUnitCompletedEvent(Unit attacker, Unit defender)
            {
                Attacker = attacker;
                Defender = defender;
            }
        }
        public class AttackCityStartEvent
        {
            public Unit Attacker;
            public City City;
            public List<HexCoord> Path;
            public bool CityIsCapture;
            public List<Unit> DefenderUnits;
            public AttackCityStartEvent(Unit attacker, City city, List<HexCoord> path, bool cityIsCapture, List<Unit> defenderUnits)
            {
                Attacker = attacker;
                City = city;
                Path = path;
                CityIsCapture = cityIsCapture;
                DefenderUnits = defenderUnits;
            }
        }
        public class AttackCityCompletedEvent
        {
            public Unit Attacker;
            public City City;
            public bool CityIsCapture;
            public AttackCityCompletedEvent(Unit attacker, City city, bool cityIsCapture)
            {
                Attacker = attacker;
                City = city;
                CityIsCapture = cityIsCapture;
            }
        }

    }
}