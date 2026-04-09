using NUnit.Framework;
using UnityEngine;
using YassinTarek.SimonSays.Config;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;
using YassinTarek.SimonSays.Infrastructure;
using YassinTarek.SimonSays.Services;

namespace YassinTarek.Tests.EditMode
{
    public sealed class ScoreServiceTests
    {
        private EventBus _bus;
        private InMemoryHighScoreRepository _repository;
        private GameConfig _config;
        private ScoreService _service;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _repository = new InMemoryHighScoreRepository();
            _config = ScriptableObject.CreateInstance<GameConfig>();
            _service = new ScoreService(_bus, _repository, _config);
            _service.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AddRoundScore_AddsRoundTimesPointsPerRound()
        {
            _service.AddRoundScore(3);

            Assert.AreEqual(30, _service.Score);
        }

        [Test]
        public void AddRoundScore_Accumulates()
        {
            _service.AddRoundScore(1);
            _service.AddRoundScore(2);

            Assert.AreEqual(30, _service.Score);
        }

        [Test]
        public void ResetScore_ZerosScore()
        {
            _service.AddRoundScore(2);
            _service.ResetScore();

            Assert.AreEqual(0, _service.Score);
        }

        [Test]
        public void AddRoundScore_UpdatesHighScoreWhenExceeded()
        {
            _service.AddRoundScore(5);

            Assert.AreEqual(50, _repository.Load());
        }

        [Test]
        public void AddRoundScore_DoesNotLowerHighScore()
        {
            _service.AddRoundScore(5);
            _service.ResetScore();
            _service.AddRoundScore(1);

            Assert.AreEqual(50, _repository.Load());
        }

        [Test]
        public void Initialize_LoadsHighScoreFromRepository()
        {
            _repository.Save(99);
            var freshService = new ScoreService(_bus, _repository, _config);
            freshService.Initialize();
            freshService.AddRoundScore(1);

            ScoreChangedEvent? received = null;
            void Handler(ScoreChangedEvent evt) => received = evt;
            _bus.Subscribe<ScoreChangedEvent>(Handler);
            freshService.AddRoundScore(0);
            _bus.Unsubscribe<ScoreChangedEvent>(Handler);

            Assert.AreEqual(99, received?.HighScore);
        }

        [Test]
        public void AddRoundScore_PublishesScoreChangedEvent()
        {
            ScoreChangedEvent? received = null;
            void Handler(ScoreChangedEvent evt) => received = evt;
            _bus.Subscribe<ScoreChangedEvent>(Handler);

            _service.AddRoundScore(2);

            _bus.Unsubscribe<ScoreChangedEvent>(Handler);

            Assert.IsNotNull(received);
            Assert.AreEqual(20, received?.Score);
        }
    }
}
