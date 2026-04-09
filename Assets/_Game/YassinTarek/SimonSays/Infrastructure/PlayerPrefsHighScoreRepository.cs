using UnityEngine;

namespace YassinTarek.SimonSays.Infrastructure
{
    public sealed class PlayerPrefsHighScoreRepository : IHighScoreRepository
    {
        private const string Key = "SimonSays_HighScore";

        public int Load() => PlayerPrefs.GetInt(Key, 0);

        public void Save(int value) => PlayerPrefs.SetInt(Key, value);
    }
}
