using NUnit.Framework;
using UnityEngine;

namespace UsagiGame.Tests
{
    public sealed class LocalSaveTests
    {
        [SetUp, TearDown]
        public void ClearTestKeys()
        {
            PlayerPrefs.DeleteKey("usagi.highScore");
            PlayerPrefs.DeleteKey("usagi.bgm");
            PlayerPrefs.DeleteKey("usagi.sfx");
            PlayerPrefs.DeleteKey("usagi.vibration");
            PlayerPrefs.DeleteKey("usagi.currentGame");
        }

        [Test]
        public void HighScore_OnlyIncreases()
        {
            Assert.That(LocalSave.TrySetHighScore(10), Is.True);
            Assert.That(LocalSave.TrySetHighScore(5), Is.False);
            Assert.That(LocalSave.HighScore, Is.EqualTo(10));
        }

        [Test]
        public void Settings_DefaultToEnabledAndPersistChanges()
        {
            Assert.That(LocalSave.BgmEnabled, Is.True);
            LocalSave.BgmEnabled = false;
            Assert.That(LocalSave.BgmEnabled, Is.False);
        }

        [Test]
        public void CurrentGame_RoundTripsAndDeletes()
        {
            var expected = new GameSaveData
            {
                Score = 42,
                CurrentStage = 2,
                NextStage = 3,
                Fruits = new[]
                {
                    new FruitSaveData { Stage = 4, Position = new Vector2(1.5f, 2.5f), Velocity = new Vector2(.5f, -.5f) }
                }
            };

            LocalSave.SaveCurrentGame(expected);

            Assert.That(LocalSave.TryLoadCurrentGame(out GameSaveData actual), Is.True);
            Assert.That(actual.Score, Is.EqualTo(42));
            Assert.That(actual.Fruits[0].Stage, Is.EqualTo(4));
            Assert.That(actual.Fruits[0].Position, Is.EqualTo(new Vector2(1.5f, 2.5f)));

            LocalSave.DeleteCurrentGame();
            Assert.That(LocalSave.HasCurrentGame, Is.False);
        }
    }
}
