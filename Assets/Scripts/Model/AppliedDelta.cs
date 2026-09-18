using SparkAge.Model.Cities;
using SparkAge.Model.Players;
using SparkAge.Model.Units;
using System.Collections.Generic;

namespace SparkAge.Model
{
    public class AppliedDelta
    {
        public List<Unit> AddedUnits = new List<Unit>();
        public List<Unit> UpdatedUnits = new List<Unit>();
        public List<Unit> RemovedUnits = new List<Unit>();

        public List<City> AddedCities = new List<City>();
        public List<City> UpdatedCities = new List<City>();

        public List<PlayerState> UpdatedPlayers = new List<PlayerState>();

        public void Clear()
        {
            AddedUnits.Clear();
            UpdatedUnits.Clear();
            RemovedUnits.Clear();
            AddedCities.Clear();
            UpdatedCities.Clear();
            UpdatedPlayers.Clear();
        }
    }
}
