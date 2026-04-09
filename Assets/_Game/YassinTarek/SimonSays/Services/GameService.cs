using System;
using System.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;
using VContainer.Unity;
using YassinTarek.SimonSays.Config;
using YassinTarek.SimonSays.Core.Domain;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;
using YassinTarek.SimonSays.Infrastructure;

namespace YassinTarek.SimonSays.Services
{
    public sealed class GameService : IInitializable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly GameStateMachine _stateMachine;
        private readonly RoundManager _roundManager;
        private readonly ISequenceService _sequenceService;
        private readonly IAudioService _audioService;
        private readonly IScoreService _scoreService;
        private readonly IInputService _inputService;
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly GameConfig _config;

        private Action<PlayerInputReceivedEvent> _onPlayerInput;

        public GameService(
            IEventBus eventBus,
            GameStateMachine stateMachine,
            RoundManager roundManager,
            ISequenceService sequenceService,
            IAudioService audioService,
            IScoreService scoreService,
            IInputService inputService,
            ICoroutineRunner coroutineRunner,
            GameConfig config)
        {
            _eventBus = eventBus;
            _stateMachine = stateMachine;
            _roundManager = roundManager;
            _sequenceService = sequenceService;
            _audioService = audioService;
            _scoreService = scoreService;
            _inputService = inputService;
            _coroutineRunner = coroutineRunner;
            _config = config;
        }

        public void Initialize()
        {
            _onPlayerInput = HandlePlayerInput;
            _eventBus.Subscribe(_onPlayerInput);
        }

        public void StartGame()
        {
            _roundManager.Reset();
            _scoreService.ResetScore();
            _inputService.Disable();
            _stateMachine.TransitionTo(GameState.Transitioning);
            _eventBus.Publish(new GameStartedEvent());
            _coroutineRunner.StartRoutine(StartGameRoutine());
        }

        private IEnumerator StartGameRoutine()
        {
            yield return new WaitForSeconds(_config.GameStartDelay);
            StartNextRound();
        }

        private void StartNextRound()
        {
            _roundManager.StartNewRound();
            _stateMachine.TransitionTo(GameState.PlayingSequence);
            _inputService.Disable();
            _eventBus.Publish(new RoundStartedEvent { Round = _roundManager.CurrentRound });
            _sequenceService.PlaySequence(_roundManager.Sequence, OnSequenceComplete);
        }

        private void OnSequenceComplete()
        {
            _stateMachine.TransitionTo(GameState.WaitingForInput);
            _inputService.Enable();
        }

        private void HandlePlayerInput(PlayerInputReceivedEvent evt)
        {
            Debug.Assert(_stateMachine.Is(GameState.WaitingForInput), "Received player input while not waiting for input");
            if (!_stateMachine.Is(GameState.WaitingForInput))
                return;

            if (evt.Color != _roundManager.GetExpectedColor())
            {
                _stateMachine.TransitionTo(GameState.Transitioning);
                _inputService.Disable();
                _audioService.Play(SoundId.ErrorBuzz);
                _eventBus.Publish(new PlayerInputWrongEvent());
                _coroutineRunner.StartRoutine(GameOverRoutine());
                return;
            }

            _audioService.Play(SoundId.CorrectInput);
            _eventBus.Publish(new PlayerInputCorrectEvent { Color = evt.Color });

            if (_roundManager.AdvanceStep())
            {
                _audioService.Play(SoundId.RoundWon);
                _scoreService.AddRoundScore(_roundManager.CurrentRound);
                _eventBus.Publish(new RoundWonEvent { Round = _roundManager.CurrentRound });
                _stateMachine.TransitionTo(GameState.Transitioning);
                _inputService.Disable();
                _coroutineRunner.StartRoutine(RoundTransitionRoutine());
            }
        }

        private IEnumerator RoundTransitionRoutine()
        {
            yield return new WaitForSeconds(_config.RoundTransitionDelay);
            StartNextRound();
        }

        private IEnumerator GameOverRoutine()
        {
            yield return new WaitForSeconds(_config.GameOverDelay);
            _stateMachine.TransitionTo(GameState.GameOver);
            _eventBus.Publish(new GameOverEvent { FinalScore = _scoreService.Score });
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe(_onPlayerInput);
        }
    }
}
