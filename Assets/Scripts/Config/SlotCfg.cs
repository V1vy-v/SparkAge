using SparkAge.Controller;
using UnityEngine;

namespace SparkAge.Config
{
    [CreateAssetMenu(fileName = "SlotInfo", menuName = "Config/Slot")]
    public class SlotCfg : ScriptableObject
    {
        [Header("网络角色")]
        public ControllerType Type;
        public int PlayerId;

        [Header("角色信息")]
        public int CharacterId;
        public string Name;
        public string Description;

        [Header("引用")]
        public GameObject Prefab;
    }
}
