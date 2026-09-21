using SparkAge.Controller.Network;
using SparkAge.Framework.Hex;
using SparkAge.Model.Cities;
using SparkAge.Model.Hex;
using SparkAge.Model.Map;
using SparkAge.Model.Players;
using SparkAge.Model.StaticInfos;
using SparkAge.Model.Units;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SparkAge.Model
{
    /// <summary>
    /// 游戏世界状态
    /// </summary>
    public class GameState
    {
        StaticInfo staticInfo;
        UnitInfo GetUnitInfo(UnitType type) => staticInfo.UnitInfos[type];//单位配置访问器
        CityInfo GetCityInfo(int i) => staticInfo.CityInfos[i];//城市配置访问器

        GameInfo gameInfo;//本局设置
        MapData map;//地图数据
        public MapData Map => map;
        List<PlayerState> Players;//所有玩家数据
        public List<PlayerState> AllPlayers => Players;
        List<Unit> units;//所有单位数据
        public List<Unit> AllUnits => units;
        List<City> cities;//所有城市数据
        public List<City> AllCities => cities;

        int nxtUnitID = 0;//维护单位ID分配
        int nxtCityID = 0;//维护城市ID分配
        int turnNumber;//当前回合数
        public int TurnNumber => turnNumber;
        int currentPlayer;//当前可操作的玩家
        public int CurrentPlayer => currentPlayer;


        public GameState(GameInfo gameInfo, StaticInfo staticInfo)
        {
            //全局配置
            this.staticInfo = staticInfo;
            //本局配置数据
            this.gameInfo = gameInfo;
        }

        public void Init()
        {
            //地图、玩家数据、单位数据、城市数据
            map = MapGenerator.Generate(gameInfo.MapInfo.MapWidth, gameInfo.MapInfo.MapHeight, gameInfo.MapInfo.Seed);
            Players = new List<PlayerState>();
            units = new List<Unit>(200);
            cities = new List<City>(100);
            //当前回合数、当前玩家、当前单位分配ID、当前城市分配ID
            turnNumber = 1;
            currentPlayer = 1;
            nxtUnitID = 0;
            nxtCityID = 0;
            //初始化玩家信息
            List<HexCoord> points = Map.FindSpawnPointsFirst(4);
            for (int i = 0; i < gameInfo.PlayerInfos.Count; i++)
            {
                var payerInfo = gameInfo.PlayerInfos[i];
                Players.Add(new PlayerState(payerInfo.Id, payerInfo.Name, payerInfo.CharacterInfo));
            }
        }
        public void CreateInitialUnits()
        {
            List<HexCoord> points = Map.FindSpawnPointsFirst(4);
            for (int i = 0; i < gameInfo.PlayerInfos.Count; i++)
            {
                HexCoord? spawnPoint = FindSpawnPoint(points[i]);
                Unit newUnit = new Unit(nxtUnitID++, gameInfo.PlayerInfos[i].Id, (HexCoord)spawnPoint, GetUnitInfo(UnitType.Settler));
                units.Add(newUnit);
            }
        }

        #region 一、查询类方法，随时可调用
        /// <summary>
        /// 根据id获取玩家数据方法
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private PlayerState TryGetPlayer(int id)
        {
            foreach (var player in Players)
                if (player.ID == id)
                    return player;
            return null;
        }
        /// <summary>
        /// 根据id获取单位数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Unit TryGetUnit(int id)
        {
            foreach (var unit in units)
                if (unit.ID == id)
                    return unit;
            return null;
        }
        /// <summary>
        /// 根据id获取城市数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public City TryGetCity(int id)
        {
            foreach (var city in cities)
                if (city.ID == id)
                    return city;
            return null;
        }

        /// <summary>
        /// 查询某个玩家的城市数量
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public int CityCount(int owner)
            => cities.Count(c => c.Owner == owner);
        /// <summary>
        /// 查询除了自己以外的玩家是否还存活
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public bool OtherPlayerAlive(int PlayerId)
            => Players.Count(p => p.IsAlive) > 0;

        /// <summary>
        /// 查询某个格子有没有单位
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <returns></returns>
        public Unit GetUnitAt(HexCoord hexCoord)
        {
            foreach (var unit in units)
            {
                if (unit.Position.Equals(hexCoord))
                    return unit;
            }
            return null;
        }
        /// <summary>
        /// 查询某个格子是否是城市
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <returns></returns>
        public City GetCityAt(HexCoord hexCoord)
        {
            foreach (var city in cities)
            {
                if (city.Position.Equals(hexCoord))
                    return city;
            }
            return null;
        }
        /// <summary>
        /// 查询某个格子是否处于城市
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <returns></returns>
        public City GetCityIn(HexCoord hexCoord)
        {
            foreach (var city in cities)
            {
                if (city.Position.DistanceTo(hexCoord) <= city.Radius)
                    return city;
            }
            return null;
        }

        /// <summary>
        /// 判断某个格子是否能经过
        /// </summary>
        /// <param name="hex"></param>
        /// <param name="mover"></param>
        /// <returns></returns>
        public bool CanPass(HexCoord hex, Unit mover)
            => Map.IsInMap(hex)
            && Map.Tiles[hex].Walkable
            && !(GetUnitAt(hex) is Unit u && u.Owner != mover.Owner)
            && !(GetCityAt(hex) is City c && c.Owner != mover.Owner);
        /// <summary>
        /// 判断某个格子是否能驻留
        /// </summary>
        /// <param name="hex"></param>
        /// <param name="mover"></param>
        /// <returns></returns>
        public bool CanStand(HexCoord hex, Unit mover)
            => Map.IsInMap(hex)
            && Map.Tiles[hex].Walkable
            && GetUnitAt(hex) == null
            && !(GetCityAt(hex) is City c && c.Owner != mover.Owner);

        /// <summary>
        /// 查询地图关键位置
        /// </summary>
        /// <returns></returns>
        public (HexCoord, HexCoord, HexCoord) GetMapKeyPos()
        {
            HexCoord center = new HexCoord(Map.Width / 2, Map.Height / 2);
            HexCoord bound1 = new HexCoord(Map.Width - 1, Map.Height - 1);
            HexCoord bound2 = new HexCoord(0, 0);
            return (center, bound1, bound2);
        }

        /// <summary>
        /// 用于Ai寻找攻击目标
        /// </summary>
        /// <param name="attacker"></param>
        /// <returns></returns>
        public List<HexCoord> AiFindTarget(Unit attacker)
        {
            List<HexCoord> res = new List<HexCoord>();
            foreach (Unit unit in units)
            {
                if (unit.Position.DistanceTo(attacker.Position) < 8.0f  && unit.Owner != attacker.Owner)
                {
                    res.Add(unit.Position);
                }
            }
            foreach (City city in cities)
            {
                if (city.Position.DistanceTo(attacker.Position) < 8.0f && city.Owner != attacker.Owner)
                {
                    res.Add(city.Position);
                }
            }
            return res;
        }


        /// <summary>
        /// 查询周围出生格内可出生单位的格子
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public HexCoord? FindunitspawnNear(HexCoord center)
        {
            if (Map.Tiles[center].Walkable && GetUnitAt(center) == null)
                return center;

            for (int i = 0; i < 6; i++)
            {
                HexCoord point = center.Neighbor(i);
                if (Map.Tiles[point].Walkable && GetUnitAt(point) == null)
                    return point;
            }
            return null;
        }

        /// <summary>
        /// 根据单位位置和移动力使用扩散算法计算可到达点：对接表现层
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public (HashSet<HexCoord>, HashSet<HexCoord>) GetReachableTiles(Unit unit)
        {
            Dictionary<HexCoord, int> movementLeftDic = new Dictionary<HexCoord, int>();

            HashSet<HexCoord> moveTiles = new HashSet<HexCoord>();
            HashSet<HexCoord> attackTiles = new HashSet<HexCoord>();

            movementLeftDic[unit.Position] = unit.MovementLeft;
            Queue<HexCoord> queue = new();
            queue.Enqueue(unit.Position);
            while (queue.Count > 0)
            {
                HexCoord curHex = queue.Dequeue();

                for (int i = 0; i < 6; i++)
                {
                    HexCoord newHex = curHex.Neighbor(i);
                    //地形先不可达
                    if (!(Map.IsInMap(newHex) && Map.Tiles[newHex].Walkable)) continue;
                    //移动力不足
                    int remaining = movementLeftDic[curHex] - Map.Tiles[newHex].MoveCost;
                    if (remaining < 0) continue;
                    //可攻击地块：不可经过
                    if (GetCityAt(newHex) is City c && c.Owner != unit.Owner ||
                        GetUnitAt(newHex) is Unit u && u.Owner != unit.Owner)
                    {
                        attackTiles.Add(newHex);
                        continue;
                    }
                    //可到达地块：包含友方单位
                    if (!movementLeftDic.TryGetValue(newHex, out int old) || remaining > old)
                    {
                        movementLeftDic[newHex] = remaining;
                        queue.Enqueue(newHex);
                        moveTiles.Add(newHex);
                    }
                }
            }
            return (moveTiles, attackTiles);
        }

        /// <summary>
        /// BFS算法搜索初始出生点的地块
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
        #endregion

        #region 二、客户端执行方法，主要用于同步状态
        List<Unit> deadUnits = new List<Unit>();
        public Unit TryGetDeadUnit(int id) => deadUnits.Find(u => u.ID == id);
        public void ApplyGameDelta(GameStateDeltaMsg msg)
        {
            deadUnits.Clear();
            //更新回合数和当前玩家
            turnNumber = msg.turnNumber;
            currentPlayer = msg.curPlayer;
            //更新涉及的单位
            if(msg.UnitDatas != null)
            {
                foreach (var data in msg.UnitDatas)
                {
                    Unit unit = TryGetUnit(data.Id);
                    if (unit != null)
                    {
                        unit.UpdateProperty(data);
                        if (unit.IsDead)
                        {
                            deadUnits.Add(unit);
                            units.Remove(unit);
                        }
                    }
                    else
                    {
                        //创建单位
                        CreatUnit(data);
                    }
                }
            }
            //更新涉及的城市
            if(msg.CityDatas  != null)
            {
                foreach (var data in msg.CityDatas)
                {
                    City city = TryGetCity(data.Id);
                    if (city != null)
                    {
                        city.UpdateProperty(data);
                    }
                    else
                    {
                        //创建城市
                        CreatCity(data);
                    }
                }
            }
            //更新玩家数据
            if(msg.PlayerDatas != null)
            {
                foreach (var data in msg.PlayerDatas)
                {
                    PlayerState player = TryGetPlayer(data.Id);
                    player.UpdateState(data);
                }
            }
        }
        public void CreatUnit(UnitData unitData)
        {
            Unit newUnit = new Unit(unitData.Id, unitData.Owner, unitData.Position, GetUnitInfo(unitData.Type));
            units.Add(newUnit);
        }
        public void CreatCity(CityData cityData)
        {
            City newCity = new City(cityData.Id, cityData.Owner, cityData.Name, cityData.Position, GetCityInfo(0));
            cities.Add(newCity);
        }
        #endregion

        #region 三、主机端执行方法，客户端不允许调用
        Random r = new Random();

        /// <summary>
        /// 数据层：玩家结束回合
        /// </summary>
        public void EndPhase()
        {
            currentPlayer++;
            if (currentPlayer > Players.Count)
                EndTurn();
        }
        /// <summary>
        /// 数据层：回合结束
        /// </summary>
        public void EndTurn()
        {
            //结算每个城市生产力变化
            foreach (var city in cities)
                city.Production += GameRules.CityProductionPerTurn;
            //所有单位恢复移动力
            foreach (var unit in units)
                unit.MovementLeft = unit.MaxMovement;

            //结算回合数
            turnNumber++;
            //重置当前玩家
            currentPlayer = 1;
        }

        public enum BuildUnitFailReason { Success, WrongCityID, NoAccess, NotEnoughProduction, NoUnitSpawnNear }
        public readonly struct BuildUnitResult
        {
            public readonly bool Success;
            public readonly BuildUnitFailReason Reason;
            public readonly Unit Unit;
            public BuildUnitResult(bool success, BuildUnitFailReason reason, Unit unit)
            {
                Success = success;
                Reason = reason;
                Unit = unit;
            }
        }
        /// <summary>
        /// 数据层：在某个城市造单位
        /// </summary>
        /// <param name="city"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public BuildUnitResult BuildUnit(City city, UnitType type)
        {
            if (city.Owner != currentPlayer)
                return new BuildUnitResult(false, BuildUnitFailReason.NoAccess, null);

            int production = (type == UnitType.Warrior) ? GetUnitInfo(UnitType.Warrior).Cost : GetUnitInfo(UnitType.Settler).Cost;
            if (city.Production < production)
                return new BuildUnitResult(false, BuildUnitFailReason.NotEnoughProduction, null);

            HexCoord? spawnHex = FindunitspawnNear(city.Position);
            if (spawnHex == null)
                return new BuildUnitResult(false, BuildUnitFailReason.NoUnitSpawnNear, null);

            Unit unit = new Unit(nxtUnitID++, city.Owner, (HexCoord)spawnHex, GetUnitInfo(type));
            units.Add(unit);
            city.Production -= production;

            return new BuildUnitResult(true, BuildUnitFailReason.Success, unit);
        }

        public enum MoveFailReason { Success, WrongUnitID, NoAccess, InvaildPos, Unreachable, NoPath }
        public readonly struct MoveResult
        {
            public readonly bool Success;
            public readonly MoveFailReason Reason;
            public readonly List<HexCoord> Path;
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
            if (unit.Owner != currentPlayer)
                return new MoveResult(false, MoveFailReason.NoAccess, null);
            if (!CanStand(tarHex, unit))
                return new MoveResult(false, MoveFailReason.InvaildPos, null);
            PathResult pathRes = Pathfinding.FindPath(unit.Position, tarHex,
                hex => CanPass(hex, unit) ? Map.Tiles[hex].MoveCost : -1);
            if (!pathRes.Found)
                return new MoveResult(false, MoveFailReason.NoPath, null);
            if (pathRes.Cost > unit.MovementLeft)
                return new MoveResult(false, MoveFailReason.Unreachable, null);

            unit.MovementLeft -= pathRes.Cost;
            unit.Position = tarHex;
            return new MoveResult(true, MoveFailReason.Success, pathRes.Path);
        }

        public enum FoundCityFailReason { Success, WrongUnitID, NoAccess, NotSettler, Unbuildable, OccupiedByUnit, OccupiedByCity}
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
        /// <summary>
        /// 数据层：移民建城
        /// </summary>
        /// <param name="settler"></param>
        /// <returns></returns>
        public FoundCityResult FoundCity(Unit settler)
        {
            if (settler.Owner != currentPlayer)
                return new FoundCityResult(false, FoundCityFailReason.NoAccess, null);

            if (settler.Type != UnitType.Settler)
                return new FoundCityResult(false, FoundCityFailReason.NotSettler, null);

            if (!Map.Tiles[settler.Position].Walkable)
                return new FoundCityResult(false, FoundCityFailReason.Unbuildable, null);

            foreach (var u in units)
                if (u != settler && u.Position.Equals(settler.Position))
                    return new FoundCityResult(false, FoundCityFailReason.OccupiedByUnit, null);

            if (GetCityIn(settler.Position) != null)
                return new FoundCityResult(false, FoundCityFailReason.OccupiedByCity, null);

            //单位注销
            settler.IsDead = true;
            units.Remove(settler);

            //新建城市
            List<string> cityNames = gameInfo.GetPlayerInfo(settler.Owner).CharacterInfo.CityNames;
            string cityName = cityNames[r.Next(cityNames.Count)];
            City city = new City(nxtCityID++, settler.Owner, cityName, settler.Position, GetCityInfo(0));
            cities.Add(city);

            return new FoundCityResult(true, FoundCityFailReason.Success, city);
        }


        public enum AttackUnitFailReason { Success, WrongUnitID, NoAccess, IsSameOwner, IsSettler, Unreachable }
        public readonly struct AttackUnitResult
        {
            public readonly bool Success;
            public readonly AttackUnitFailReason Reason;
            public readonly bool CanEnter;
            public readonly List<HexCoord> Path;
            public AttackUnitResult(bool success, AttackUnitFailReason reason, bool canEnter, List<HexCoord> path)
            {
                Success = success;
                Reason = reason;
                CanEnter = canEnter;
                Path = path;
            }
        }
        /// <summary>
        /// 数据层：攻击单位
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="defender"></param>
        /// <returns></returns>
        public AttackUnitResult AttackUnit(Unit attacker, Unit defender)
        {
            HexCoord target = defender.Position;
            if (attacker.Owner != currentPlayer)
                return new AttackUnitResult(false, AttackUnitFailReason.NoAccess, false, null);

            if (defender.Owner == attacker.Owner)
                return new AttackUnitResult(false, AttackUnitFailReason.IsSameOwner, false, null);

            if (attacker.Type == UnitType.Settler)
                return new AttackUnitResult(false, AttackUnitFailReason.IsSettler, false, null);

            PathResult pathRes = Pathfinding.FindPath(attacker.Position, target,
                hex => ((hex.DistanceTo(target) > 1 && CanPass(hex, attacker)) ||
                        (hex.DistanceTo(target) == 1 && CanStand(hex, attacker)) ||
                        hex.Equals(target)) ? Map.Tiles[hex].MoveCost : -1);
            if (!pathRes.Found || pathRes.Cost > attacker.MovementLeft)
                return new AttackUnitResult(false, AttackUnitFailReason.Unreachable, false, null);

            defender.Hp -= Math.Max(1, attacker.Atk - defender.Def);
            if (defender.Type != UnitType.Settler)
                attacker.Hp -= Math.Max(1, defender.Atk - attacker.Def);
            bool attackerIsDead = attacker.Hp <= 0;
            bool defenderIsDead = defender.Hp <= 0;
            bool canEnter = false;
            if (defenderIsDead)
            {
                defender.IsDead = true;
                units.Remove(defender);
                attacker.Position = target;
                if (GetCityAt(target) == null)
                    canEnter = true;
            }
            else if (pathRes.Path.Count >= 2)
            {
                attacker.Position = pathRes.Path[pathRes.Path.Count - 2];
            }
            if (attackerIsDead)
            {
                attacker.IsDead = true;
                units.Remove(attacker);
            }
            attacker.MovementLeft = 0;

            return new AttackUnitResult(true, AttackUnitFailReason.Success, canEnter, pathRes.Path);
        }


        public enum AttackCityFailReason { Success, WrongUnitID, WrongCityID, NoAccess, IsSameOwner, IsSettler, Unreachable }
        public readonly struct AttackCityResult
        {
            public readonly bool Success;
            public readonly AttackCityFailReason Reason;
            public readonly bool CityIsCaptured;
            public readonly List<HexCoord> Path;
            public readonly bool DefenderIsDead;
            public AttackCityResult(bool success, AttackCityFailReason reason, bool cityIsCaptured, List<HexCoord> path, bool defenderIsDead)
            {
                Success = success;
                Reason = reason;
                CityIsCaptured = cityIsCaptured;
                Path = path;
                DefenderIsDead = defenderIsDead;
            }
        }
        public AttackCityResult AttackCity(Unit attacker, City city)
        {
            if (attacker.Owner != currentPlayer)
                return new AttackCityResult(false, AttackCityFailReason.NoAccess, false, null, false);

            if (city.Owner == attacker.Owner)
                return new AttackCityResult(false, AttackCityFailReason.IsSameOwner, false, null, false);

            if (attacker.Type == UnitType.Settler)
                return new AttackCityResult(false, AttackCityFailReason.IsSettler, false, null, false);

            PathResult pathRes = Pathfinding.FindPath(attacker.Position, city.Position,
                hex => (hex.DistanceTo(city.Position) > 1 && CanPass(hex, attacker) ||
                        hex.DistanceTo(city.Position) == 1 && CanStand(hex, attacker) ||
                        hex.Equals(city.Position)) ? Map.Tiles[hex].MoveCost : -1);
            if (!pathRes.Found || pathRes.Cost > attacker.MovementLeft)
                return new AttackCityResult(false, AttackCityFailReason.Unreachable, false, null, false);

            city.Hp -= Math.Max(1, attacker.Atk - city.Def);
            bool cityIsDead = city.Hp <= 0;
            bool defenderIsDead = false;
            if (cityIsDead)
            {
                int oldOwner = city.Owner;
                //更换城市所属与血量
                city.Owner = attacker.Owner;
                city.Hp = city.MaxHp;
                //更新攻方单位位置
                attacker.Position = city.Position;
                //更新玩家状态
                PlayerState defender = TryGetPlayer(oldOwner);
                //判断守方玩家是否失败
                if (CityCount(defender.ID) <= 0)
                {
                    defenderIsDead = true;
                    defender.IsAlive = false;
                }
            }
            else if (pathRes.Path.Count >= 2)
            {
                attacker.Position = pathRes.Path[pathRes.Path.Count - 2];
            }
            attacker.MovementLeft = 0;

            return new AttackCityResult(true, AttackCityFailReason.Success, cityIsDead, pathRes.Path, defenderIsDead);
        }
        #endregion
    }
}
