using SparkAge.Model;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Orders;
using SparkAge.Model.Units;
using System.Collections.Generic;
using System.Linq;

public class AiOrders
{
    GameState state;
    HashSet<City> usedCities = new();
    HashSet<Unit> usedUnits = new();
    public void Init(GameState state)
    {
        this.state = state;
    }

    public void BeginAiPhase()
    {
        usedCities.Clear();
        usedUnits.Clear();
    }
    public BaseOrder DecideOrders()
    {
        //城市自动造兵
        foreach (var city in state.Cities)
        {
            if (city.Owner == state.CurrentPlayer && !usedCities.Contains(city))
            {
                usedCities.Add(city);
                return new BuildUnitOrder(state.CurrentPlayer, city, UnitType.Warrior);
            }
        }

        //移民自动建城
        foreach (var settler in state.Units)
        {
            if (settler.Owner == state.CurrentPlayer && settler.Type == UnitType.Settler && !usedUnits.Contains(settler))
            {
                usedUnits.Add(settler);
                return new FoundCityOrder(state.CurrentPlayer, settler);
            }  
        }

        //勇士自动靠近玩家城市
        foreach (var warrior in state.Units)
        {
            if (warrior.Type == UnitType.Settler || 
                warrior.Owner != state.CurrentPlayer || 
                usedUnits.Contains(warrior)) 
                continue;

            usedUnits.Add(warrior);
            var (moveTiles, attackTiles) = state.GetReachableTiles(warrior);
            if(attackTiles.Count > 0)
            {
                HexCoord tarHex = attackTiles.First();
                if (state.GetUnitAt(tarHex) is Unit u)
                {
                    return new AttackUnitOrder(state.CurrentPlayer, warrior, u);
                }
                else if (state.GetCityAt(tarHex) is City c)
                {
                    return new AttackCityOrder(state.CurrentPlayer, warrior, c);
                }
            }
            else
            {
                HexCoord target = state.FindTarget(warrior);
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
                return new MoveUnitOrder(state.CurrentPlayer, warrior, (HexCoord)tarHex);
            }
        }
        //结束ai回合
        return new EndPhaseOrder(state.CurrentPlayer);
    }
}
