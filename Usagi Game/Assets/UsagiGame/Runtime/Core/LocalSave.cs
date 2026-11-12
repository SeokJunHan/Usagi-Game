using System;
using UnityEngine;

namespace UsagiGame
{
    public static class LocalSave
    {
        private const string HighScoreKey = "usagi.highScore";
        private const string BgmKey = "usagi.bgm";
        private const string SfxKey = "usagi.sfx";
        private const string VibrationKey = "usagi.vibration";
        private const string CurrentGameKey = "usagi.currentGame";

        private static bool resumeRequested;

        public static int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);
        public static bool BgmEnabled { get => GetBool(BgmKey); set => SetBool(BgmKey, value); }
        public static bool SfxEnabled { get => GetBool(SfxKey); set => SetBool(SfxKey, value); }
        public static bool VibrationEnabled { get => GetBool(VibrationKey); set => SetBool(VibrationKey, value); }
        public static bool HasCurrentGame => PlayerPrefs.HasKey(CurrentGameKey);

        public static bool TrySetHighScore(int score)
        {
            if (score <= HighScore) return false;
            PlayerPrefs.SetInt(HighScoreKey, score);
            PlayerPrefs.Save();
            return true;
        }

        public static void SaveSettings() => PlayerPrefs.Save();

        public static void SaveCurrentGame(GameSaveData data)
        {
            if (data == null) return;
            PlayerPrefs.SetString(CurrentGameKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static bool TryLoadCurrentGame(out GameSaveData data)
        {
            data = null;
            if (!HasCurrentGame) return false;

            try
            {
                data = JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString(CurrentGameKey));
                return data != null && data.Fruits != null && data.CurrentStage > 0 && data.NextStage > 0;
            }
            catch (Exception)
            {
                DeleteCurrentGame();
                return false;
            }
        }

        public static void DeleteCurrentGame()
        {
            resumeRequested = false;
            PlayerPrefs.DeleteKey(CurrentGameKey);
            PlayerPrefs.Save();
        }

        public static void RequestResume() => resumeRequested = true;

        public static bool ConsumeResumeRequest()
        {
            bool requested = resumeRequested;
            resumeRequested = false;
            return requested;
        }

        private static bool GetBool(string key) => PlayerPrefs.GetInt(key, 1) != 0;
        private static void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public int Score;
        public int CurrentStage;
        public int NextStage;
        public FruitSaveData[] Fruits = Array.Empty<FruitSaveData>();
    }

    [Serializable]
    public struct FruitSaveData
    {
        public int Stage;
        public Vector2 Position;
        public float Rotation;
        public Vector2 Velocity;
        public float AngularVelocity;
    }
}
