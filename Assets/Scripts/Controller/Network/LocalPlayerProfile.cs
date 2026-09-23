using UnityEngine;

namespace SparkAge.Controller.Network
{
    public static class LocalPlayerProfile
    {
        private const string NickNameKey = "SparkAge.NickName";

        public static string NickName { get; private set; } = "";

        public static bool IsFirst => string.IsNullOrEmpty(NickName);

        static LocalPlayerProfile()
        {
            Load();
        }

        public static void Load()
        {
            NickName = PlayerPrefs.GetString(NickNameKey, "");
        }

        public static void Save(string name)
        {
            string finalName = name == null ? "" : name.Trim();

            if (string.IsNullOrEmpty(finalName))
            {
                finalName = "玩家" + Random.Range(1000, 9999);
            }

            NickName = finalName;
            PlayerPrefs.SetString(NickNameKey, NickName);
            PlayerPrefs.Save();
        }
    }
}