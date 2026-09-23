using UnityEngine;

namespace SparkAge.Framework
{
    public static class GameSettings
    {
        private const string MusicVolumeKey = "SparkAge.MusicVolume";
        private const string SoundVolumeKey = "SparkAge.SoundVolume";

        public static float MusicVolume { get; private set; } = 0.5f;
        public static float SoundVolume { get; private set; } = 0.5f;

        static GameSettings()
        {
            Load();
        }

        public static void Load()
        {
            MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f);
            SoundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 0.5f);
            Apply();
        }

        public static void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            Apply();
        }

        public static void SetSoundVolume(float value)
        {
            SoundVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SoundVolumeKey, SoundVolume);
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.SetFloat(SoundVolumeKey, SoundVolume);
            PlayerPrefs.Save();
        }

        private static void Apply()
        {
            // 当前项目还没有 AudioMixer，先用 AudioListener 控制全局音量。
            // 音乐和音效分别保存，后续接 AudioMixer 时再分别应用。
            AudioListener.volume = SoundVolume;
        }
    }
}