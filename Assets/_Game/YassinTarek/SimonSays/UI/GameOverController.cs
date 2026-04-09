using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;
using YassinTarek.SimonSays.Infrastructure;
using YassinTarek.SimonSays.Services;

namespace YassinTarek.SimonSays.UI
{
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _finalScoreText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _mainMenuButton;

        private IEventBus _eventBus;
        private GameService _gameService;
        private ISceneLoaderService _sceneLoader;

        private Action<GameOverEvent> _onGameOver;
        private Action<GameStartedEvent> _onGameStarted;

        [Inject]
        public void Construct(IEventBus eventBus, GameService gameService, ISceneLoaderService sceneLoader)
        {
            _eventBus = eventBus;
            _gameService = gameService;
            _sceneLoader = sceneLoader;

            _retryButton.onClick.AddListener(OnRetryClicked);
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            gameObject.SetActive(false);

            _onGameOver = HandleGameOver;
            _onGameStarted = HandleGameStarted;
            _eventBus.Subscribe(_onGameOver);
            _eventBus.Subscribe(_onGameStarted);
        }

        private void OnRetryClicked() => _gameService.StartGame();
        private void OnMainMenuClicked() => _sceneLoader.LoadMainMenu();

        private void HandleGameOver(GameOverEvent evt)
        {
            _finalScoreText.text = $"Score: {evt.FinalScore}";
            gameObject.SetActive(true);
        }

        private void HandleGameStarted(GameStartedEvent _) => gameObject.SetActive(false);

        private void OnDestroy()
        {
            _retryButton?.onClick.RemoveListener(OnRetryClicked);
            _mainMenuButton?.onClick.RemoveListener(OnMainMenuClicked);
            _eventBus?.Unsubscribe(_onGameOver);
            _eventBus?.Unsubscribe(_onGameStarted);
        }
    }
}
