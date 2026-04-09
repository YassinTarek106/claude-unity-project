using System;
using TMPro;
using UnityEngine;
using VContainer;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;

namespace YassinTarek.SimonSays.UI
{
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _highScoreText;
        [SerializeField] private TMP_Text _roundText;

        private IEventBus _eventBus;

        private Action<GameStartedEvent> _onGameStarted;
        private Action<GameOverEvent> _onGameOver;
        private Action<ScoreChangedEvent> _onScoreChanged;
        private Action<RoundStartedEvent> _onRoundStarted;
        private Action<RoundWonEvent> _onRoundWon;

        [Inject]
        public void Construct(IEventBus eventBus)
        {
            _eventBus = eventBus;

            gameObject.SetActive(false);

            _onGameStarted = HandleGameStarted;
            _onGameOver = HandleGameOver;
            _onScoreChanged = HandleScoreChanged;
            _onRoundStarted = HandleRoundStarted;
            _onRoundWon = HandleRoundWon;

            _eventBus.Subscribe(_onGameStarted);
            _eventBus.Subscribe(_onGameOver);
            _eventBus.Subscribe(_onScoreChanged);
            _eventBus.Subscribe(_onRoundStarted);
            _eventBus.Subscribe(_onRoundWon);
        }

        private void HandleGameStarted(GameStartedEvent _) => gameObject.SetActive(true);

        private void HandleGameOver(GameOverEvent _) => gameObject.SetActive(false);

        private void HandleScoreChanged(ScoreChangedEvent evt)
        {
            _scoreText.text = $"Score: {evt.Score}";
            _highScoreText.text = $"Best: {evt.HighScore}";
        }

        private void HandleRoundStarted(RoundStartedEvent evt) =>
            _roundText.text = $"Round {evt.Round}";

        private void HandleRoundWon(RoundWonEvent evt) =>
            _roundText.text = $"Round {evt.Round} - Clear!";

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe(_onGameStarted);
            _eventBus?.Unsubscribe(_onGameOver);
            _eventBus?.Unsubscribe(_onScoreChanged);
            _eventBus?.Unsubscribe(_onRoundStarted);
            _eventBus?.Unsubscribe(_onRoundWon);
        }
    }
}
