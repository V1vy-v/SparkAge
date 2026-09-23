using SparkAge.Config;
using SparkAge.Model.Units;
using System.Collections.Generic;

namespace SparkAge.Model.StaticInfos
{
    public class StaticInfo
    {
        //角色配置
        public Dictionary<int, CharacterInfo> CharacterInfos = new Dictionary<int, CharacterInfo>();
        //单位配置
        public Dictionary<UnitType, UnitInfo> UnitInfos = new Dictionary<UnitType, UnitInfo>();
        //城市配置
        public Dictionary<int, CityInfo> CityInfos = new Dictionary<int, CityInfo>();
        //地图配置
        public Dictionary<int, MapInfo> MapInfos = new Dictionary<int, MapInfo>();
        public void Init()
        {
            //装配
            foreach (var cfg in ConfigMgr.Instance.characterCfgs)
            {
                CharacterInfos[cfg.Id] = new CharacterInfo(
                    cfg.Id, cfg.Name, cfg.Description, cfg.CityNames, 
                    cfg.WarriorAtkBonus, cfg.WarriorDefBonus, cfg.WarriorHpBonus, 
                    cfg.CityHpBonus, cfg.CityDefBonus, cfg.CityProductionBonus);
            }
            foreach (var cfg in ConfigMgr.Instance.unitCfgs)
            {
                UnitInfos[cfg.Type] = new UnitInfo(cfg.Type, cfg.Name, cfg.Atk, cfg.Def, cfg.Hp, cfg.Movement, cfg.Cost);
            }
            foreach (var cfg in ConfigMgr.Instance.cityCfgs)
            {
                CityInfos[cfg.Id] = new CityInfo(cfg.Id, cfg.Hp, cfg.Def, cfg.Radius, cfg.Production);
            }
            foreach (var cfg in ConfigMgr.Instance.mapCfg)
            {
                MapInfos[cfg.Id] = new MapInfo(cfg.Id, cfg.MapWidth, cfg.MapHeight);
            }
        }
    }
}
