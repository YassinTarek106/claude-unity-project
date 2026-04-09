using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YassinTarek.SimonSays.Config;
using YassinTarek.SimonSays.Core.Domain;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;
using YassinTarek.SimonSays.Infrastructure;
using YassinTarek.SimonSays.Services;

namespace YassinTarek.Tests.EditMode
{
    public sealed class GameServiceHandlePlayerInputTests
    {
        private EventBus _eventBus;
        private GameStateMachine _stateMachine;
        private RoundManager _roundManager;
        private SpyAudioService _audioService;
        private SpyScoreService _scoreService;
        private SpyInputService _inputService;
        private SpySequenceService _sequenceService;
        private StubCoroutineRunner _coroutineRunner;
        private GameConfig _config;
        private GameService _gameService;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _stateMachine = new GameStateMachine(_eventBus);
            _roundManager = new RoundManager();
            _audioService = new SpyAudioService();
            _scoreService = new SpyScoreService();
            _inputService = new SpyInputService();
            _sequenceService = new SpySequenceService();
            _coroutineRunner = new StubCoroutineRunner();
            _config = ScriptableObject.CreateInstance<GameConfig>();

            _gameService = new GameService(
                _eventBus,
                _stateMachine,
                _roundManager,
                _sequenceService,
                _audioService,
                _scoreService,
                _inputService,
                _coroutineRunner,
                _config);

            _gameService.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _gameService.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void CorrectInput_PlaysCorrectInputSound()
        {
            SetupWaitingForInput(PanelColor.Red);

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Red });

            Assert.Contains(SoundId.CorrectInput, _audioService.PlayedSounds);
        }

        [Test]
        public void CorrectInput_PublishesPlayerInputCorrectEvent()
        {
            SetupWaitingForInput(PanelColor.Blue);
            var received = new List<PlayerInputCorrectEvent>();
            _eventBus.Subscribe<PlayerInputCorrectEvent>(e => received.Add(e));

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Blue });

            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(PanelColor.Blue, received[0].Color);
        }

        [Test]
        public void WrongInput_TransitionsToTransitioning()
        {
            SetupWaitingForInput(PanelColor.Red);

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Green });

            Assert.IsTrue(_stateMachine.Is(GameState.Transitioning));
        }

        [Test]
        public void WrongInput_DisablesInput()
        {
            SetupWaitingForInput(PanelColor.Red);

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Green });

            Assert.IsTrue(_inputService.DisableCalled);
        }

        [Test]
        public void WrongInput_PlaysErrorBuzz()
        {
            SetupWaitingForInput(PanelColor.Red);

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Green });

            Assert.Contains(SoundId.ErrorBuzz, _audioService.PlayedSounds);
        }

        [Test]
        public void WrongInput_PublishesPlayerInputWrongEvent()
        {
            SetupWaitingForInput(PanelColor.Red);
            var fired = false;
            _eventBus.Subscribe<PlayerInputWrongEvent>(_ => fired = true);

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Green });

            Assert.IsTrue(fired);
        }

        [Test]
        public void WrongInput_StartsGameOverCoroutine()
        {
            SetupWaitingForInput(PanelColor.Red);

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Green });

            Assert.AreEqual(1, _coroutineRunner.StartedRoutines.Count);
        }

        [Test]
        public void CorrectInput_LastStep_PlaysRoundWonSound()
        {
            SetupWaitingForInput(PanelColor.Red);

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Red });

            Assert.Contains(SoundId.RoundWon, _audioService.PlayedSounds);
        }

        [Test]
        public void CorrectInput_LastStep_AddsRoundScore()
        {
            SetupWaitingForInput(PanelColor.Red);

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Red });

            Assert.AreEqual(1, _scoreService.AddRoundScoreCalls.Count);
            Assert.AreEqual(1, _scoreService.AddRoundScoreCalls[0]);
        }

        [Test]
        public void CorrectInput_LastStep_PublishesRoundWonEvent()
        {
            SetupWaitingForInput(PanelColor.Red);
            var received = new List<RoundWonEvent>();
            _eventBus.Subscribe<RoundWonEvent>(e => received.Add(e));

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Red });

            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(1, received[0].Round);
        }

        [Test]
        public void CorrectInput_LastStep_TransitionsToTransitioning()
        {
            SetupWaitingForInput(PanelColor.Red);

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Red });

            Assert.IsTrue(_stateMachine.Is(GameState.Transitioning));
        }

        [Test]
        public void CorrectInput_NotLastStep_DoesNotPublishRoundWon()
        {
            SetupWaitingForInputMultiStep(PanelColor.Red, PanelColor.Blue);
            var roundWonFired = false;
            _eventBus.Subscribe<RoundWonEvent>(_ => roundWonFired = true);

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Red });

            Assert.IsFalse(roundWonFired);
        }

        [Test]
        public void IgnoresInput_WhenNotInWaitingForInputState()
        {
            _roundManager.StartNewRound();
            _stateMachine.TransitionTo(GameState.PlayingSequence);
            LogAssert.Expect(LogType.Assert, "Received player input while not waiting for input");

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Red });

            Assert.IsTrue(_stateMachine.Is(GameState.PlayingSequence));
            Assert.IsEmpty(_audioService.PlayedSounds);
        }

        [Test]
        public void Dispose_UnsubscribesFromEvents()
        {
            SetupWaitingForInput(PanelColor.Red);
            _gameService.Dispose();

            _eventBus.Publish(new PlayerInputReceivedEvent { Color = PanelColor.Green });

            Assert.IsTrue(_stateMachine.Is(GameState.WaitingForInput));
            Assert.IsEmpty(_audioService.PlayedSounds);
        }

        private void SetupWaitingForInput(PanelColor expectedColor)
        {
            _roundManager.Reset();
            _roundManager.StartNewRound();
            SetSequenceColor(0, expectedColor);
            _stateMachine.TransitionTo(GameState.WaitingForInput);
        }

        private void SetupWaitingForInputMultiStep(params PanelColor[] colors)
        {
            _roundManager.Reset();
            for (var i = 0; i < colors.Length; i++)
                _roundManager.StartNewRound();

            for (var i = 0; i < colors.Length; i++)
                SetSequenceColor(i, colors[i]);

            _stateMachine.TransitionTo(GameState.WaitingForInput);
        }

        private void SetSequenceColor(int index, PanelColor color)
        {
            var field = typeof(RoundManager)
                .GetField("_sequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (List<PanelColor>)field.GetValue(_roundManager);
            list[index] = color;
        }

        private sealed class SpyAudioService : IAudioService
        {
            public List<SoundId> PlayedSounds { get; } = new();
            public void Play(SoundId id) => PlayedSounds.Add(id);
        }

        private sealed class SpyScoreService : IScoreService
        {
            public int Score { get; private set; }
            public List<int> AddRoundScoreCalls { get; } = new();

            public void SetScore(int score) => Score = score;
            public void AddRoundScore(int round) => AddRoundScoreCalls.Add(round);
            public void ResetScore() => Score = 0;
        }

        private sealed class SpyInputService : IInputService
        {
            public bool EnableCalled { get; private set; }
            public bool DisableCalled { get; private set; }

            public void Enable() => EnableCalled = true;
            public void Disable() => DisableCalled = true;
        }

        private sealed class SpySequenceService : ISequenceService
        {
            public void PlaySequence(IReadOnlyList<PanelColor> sequence, Action onComplete) { }
        }

        private sealed class StubCoroutineRunner : ICoroutineRunner
        {
            public List<IEnumerator> StartedRoutines { get; } = new();

            public Coroutine StartRoutine(IEnumerator routine)
            {
                StartedRoutines.Add(routine);
                return null;
            }

            public void StopRoutine(Coroutine coroutine) { }
        }
    }
}
