using SparkAge.Model.StaticInfos;
using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.Config
{
    /// <summary>
    /// 配置管理器，所有配置获取入口
    /// </summary>
    public class ConfigMgr : MonoBehaviour
    {
        private static ConfigMgr instance;
        public static ConfigMgr Instance => instance;
        private void Awake()
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            StaticInfo.Init();
        }

        public List<CharacterCfg> characterCfgs = new List<CharacterCfg>();
        public List<UnitCfg> unitCfgs = new List<UnitCfg>();
        public List<CityCfg> cityCfgs = new List<CityCfg>();
        public List<MapCfg> mapCfg = new List<MapCfg>();

        //提供给Model层的静态配置
        public StaticInfo StaticInfo = new StaticInfo();
    }
}
