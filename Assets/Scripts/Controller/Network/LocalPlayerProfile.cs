using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SparkAge.Controller.Network
{
    public static class LocalPlayerProfile
    {
        public static string NickName { get; private set; } = "";
        public static bool IsFirst => NickName == "";

        public static void Save(string name)
        {
            if(name == "")
            {
                NickName = "玩家" + Random.Range(1000, 9999).ToString();
            }
            else
                NickName = name;
        }
    }
}
