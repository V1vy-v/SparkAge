using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Orders;
using SparkAge.Model.Units;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SparkAge.Model.Ai
{
    public class AiDecider
    {
        GameState state;
        int playerId;
        HashSet<int> usedCities = new HashSet<int>();
        HashSet<int> usedUnits = new HashSet<int>();
        Random r = new Random();
        public AiDecider(GameState state)
        {
            this.state = state;
        }
        public void Reset(int playerId)
        {
            this.playerId = playerId;
            usedCities.Clear();
            usedUnits.Clear();
        }
        public BaseOrder Decide()
        {
            if (state.CurrentPlayer != playerId)
                return new EndPhaseOrder(playerId);

            BaseOrder order = TryBuildUnit();
            if(order != null) return order;

            order = TryFoundCity();
            if (order != null) return order;

            order = TryAttack();
            if (order != null) return order;

            order = TryMove();
            if (order != null) return order;

            return new EndPhaseOrder(playerId);
        }
        private BaseOrder TryBuildUnit()
        {
            //城市自动造兵
            foreach (var city in state.AllCities)
            {
                if (city.Owner == playerId && !usedCities.Contains(city.ID))
                {
                    usedCities.Add(city.ID);
                    return new BuildUnitOrder(state.CurrentPlayer, city.ID, UnitType.Warrior);
                }
            }
            return null;
        }
        private BaseOrder TryFoundCity()
        {
            //移民自动建城
            foreach (var settler in state.AllUnits)
            {
                if (settler.Owner == playerId && settler.Type == UnitType.Settler && !usedUnits.Contains(settler.ID))
                {
                    usedUnits.Add(settler.ID);
                    return new FoundCityOrder(state.CurrentPlayer, settler.ID);
                }
            }
            return null;
        }
        private BaseOrder TryAttack()
        {
            foreach (var warrior in state.AllUnits)
            {
                if (warrior.Type == UnitType.Settler || warrior.Owner != playerId || usedUnits.Contains(warrior.ID))
                    continue;

                var (moveTiles, attackTiles) = state.GetReachableTiles(warrior);
                if (attackTiles.Count > 0)
                {
                    HexCoord tarHex = attackTiles.First();//first自带随机属性
                    Unit targetUnit = state.GetUnitAt(tarHex);
                    if (targetUnit != null)
                    {
                        usedUnits.Add(warrior.ID);
                        return new AttackUnitOrder(playerId, warrior.ID, targetUnit.ID);
                    }

                    City targetCity = state.GetCityAt(tarHex);
                    if (targetCity != null)
                    {
                        usedUnits.Add(warrior.ID);
                        return new AttackCityOrder(playerId, warrior.ID, targetCity.ID);
                    }
                }
            }
            return null;
        }
        private BaseOrder TryMove()
        {
            foreach (var warrior in state.AllUnits)
            {
                if (warrior.Type == UnitType.Settler || warrior.Owner != playerId || usedUnits.Contains(warrior.ID))
                    continue;

                usedUnits.Add(warrior.ID);
                var (moveTiles, attackTiles) = state.GetReachableTiles(warrior);

                List<HexCoord> tarTiles = state.AiFindTarget(warrior);

                if (tarTiles.Count == 0) continue;
                HexCoord target = tarTiles[r.Next(tarTiles.Count)];
                int best = target.DistanceTo(warrior.Position);
                HexCoord? tarHex = null;
                foreach (var hex in moveTiles)
                {
                    if (!state.CanStand(hex, warrior))
                        continue;

                    int d = target.DistanceTo(hex);
                    if (d < best)
                    {
                        best = d;
                        tarHex = hex;
                    }
                }
                if (tarHex == null) continue;
                return new MoveUnitOrder(state.CurrentPlayer, warrior.ID, (HexCoord)tarHex);
            }
            return null;
        }
    }
}
