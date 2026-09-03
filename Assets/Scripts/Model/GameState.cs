using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Map;
using SparkAge.Model.Players;
using SparkAge.Model.Units;
using System.Collections.Generic;

namespace SparkAge.Model
{
    /// <summary>
    /// 游戏世界状态
    /// </summary>
    public class GameState
    {
        public MapData Map;//地图数据
        public List<PlayerState> Players;//所有玩家数据
        public List<Unit> Units;//所有单位数据
        public List<City> Cities;//所有城市数据
        int TurnNumber;//当前回合数
        int CurrentPlayer;//当前可操作的玩家

        public GameState(MapData map)
        {
            Map = map;
            Units = new List<Unit>();
        }
        /// <summary>
        /// 根据id获取玩家数据方法
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private PlayerState GetPlayerState(int id)
        {
            foreach(var state in Players) 
                if(state.Id == id)
                    return state;
            return null;
        }

        /// <summary>
        /// 查询某个格子有没有单位
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <returns></returns>
        public Unit GetUnitAt(HexCoord hexCoord)
        {
            foreach (var unit in Units)
            {
                if (unit.Position.Equals(hexCoord))
                    return unit;
            }
            return null;
        }
        /// <summary>
        /// 查询某个格子是否处于城市
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <returns></returns>
        public City GetCityAt(HexCoord hexCoord)
        {
            foreach (var city in Cities)
            {
                if (city.Position.DistanceTo(hexCoord) <= city.Radius)
                    return city;
            }
            return null;
        }

        public enum FoundCityFailReason { Success, NotSettler, Unbuildable, OccupiedByUnit, OccupiedByCity, Limited }
        public readonly struct FoundCityResult
        {
            public readonly bool Success;
            public readonly FoundCityFailReason Reason;
            public readonly City City;
            public FoundCityResult(bool success, FoundCityFailReason reason, City city)
            {
                Success = success;
                Reason = reason;
                City = city;
            }
        }
        public FoundCityResult FoundCity(Unit settler)
        {
            if (settler.type != UnitType.Settler)
                return new FoundCityResult(false, FoundCityFailReason.NotSettler, null);
            if (!Map.Tiles[settler.Position].Walkable)
                return new FoundCityResult(false, FoundCityFailReason.Unbuildable, null);
            if (GetUnitAt(settler.Position) != null)
                return new FoundCityResult(false, FoundCityFailReason.OccupiedByUnit, null);
            if (GetCityAt(settler.Position) != null)
                return new FoundCityResult(false, FoundCityFailReason.OccupiedByCity, null);
            if (GetPlayerState(CurrentPlayer).CityNum >= GameRules.MaxCitiesPerPlayer)
                return new FoundCityResult(false, FoundCityFailReason.Limited, null);
            //单位注销销毁

            //新建城市
            City city = new City(settler.Position, 2, CurrentPlayer);
            Cities.Add(city);

            return new FoundCityResult(true, FoundCityFailReason.Success, city);
        }

        /// <summary>
        /// BFS算法搜索第一个可以作为出生点的地块
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public HexCoord? FindSpawnPoint(HexCoord center)
        {
            Queue<HexCoord> queue = new();
            List<HexCoord> visited = new List<HexCoord> { center };
            queue.Enqueue(center);
            while (queue.Count > 0)
            {
                HexCoord curHex = queue.Dequeue();
                if (Map.Tiles[curHex].Walkable)
                    return curHex;

                for (int i = 0; i < 6; i++)
                {
                    HexCoord newHex = curHex.Neighbor(i);
                    if (Map.IsInMap(newHex) && !visited.Contains(newHex))
                    {
                        queue.Enqueue(newHex);
                        visited.Add(newHex);
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 根据单位位置和移动力使用扩散算法计算可到达点
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public List<HexCoord> GetReachableTiles(Unit unit)
        {
            Dictionary<HexCoord, int> movementLeftDic = new Dictionary<HexCoord, int>();

            movementLeftDic[unit.Position] = unit.MovementLeft;
            Queue<HexCoord> queue = new();
            queue.Enqueue(unit.Position);
            while (queue.Count > 0)
            {
                HexCoord curHex = queue.Dequeue();

                for (int i = 0; i < 6; i++)
                {
                    HexCoord newHex = curHex.Neighbor(i);
                    if (!Map.IsInMap(newHex)) continue;
                    int cost = Map.Tiles[newHex].MoveCost;
                    if (cost <= 0) continue;
                    int remaining = movementLeftDic[curHex] - cost;
                    if (remaining < 0) continue;
                    if (!movementLeftDic.TryGetValue(newHex, out int old) || remaining > old)
                    {
                        movementLeftDic[newHex] = remaining;
                        queue.Enqueue(newHex);
                    }
                }
            }
            return new List<HexCoord>(movementLeftDic.Keys);
        }
        public enum MoveFailReason { Success, TileOccupied, Unreachable }
        public readonly struct MoveResult
        {
            public readonly bool Success;
            public readonly MoveFailReason Reason;     // 枚举：Success / TileOccupied / Unreachable / NoPath
            public readonly List<HexCoord> Path;       // 成功时有效（不含起点）
            public MoveResult(bool success, MoveFailReason reason, List<HexCoord> path)
            {
                Success = success;
                Reason = reason;
                Path = path;
            }
        }
        /// <summary>
        /// 数据层：单位移动
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="tarHex"></param>
        /// <returns></returns>
        public MoveResult MoveUnit(Unit unit, HexCoord tarHex)
        {

            if (!GetReachableTiles(unit).Contains(tarHex)) 
                return new MoveResult(false, MoveFailReason.Unreachable, null);
            if (GetUnitAt(tarHex) != null)
                return new MoveResult(false, MoveFailReason.TileOccupied, null);

            PathResult pathRes = Pathfinding.FindPath(unit.Position, tarHex,
                hex => Map.IsInMap(hex) ? Map.Tiles[hex].MoveCost : -1);

            if (!pathRes.Found)
                return new MoveResult(false, MoveFailReason.Unreachable, null);

            unit.MovementLeft -= pathRes.Cost;
            unit.Position = tarHex;
            return new MoveResult(true, MoveFailReason.Success, pathRes.Path);
        }
        public void EndTurn()
        {
            TurnNumber++;

            //添加UI面板变化


            foreach (var unit in Units)
                unit.MovementLeft = unit.MaxMovement;
        }
    }
}
