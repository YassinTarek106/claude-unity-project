// Animation implementation: IEnumerator coroutines + Color.Lerp on MaterialPropertyBlock.
// DOTween is not installed. If added, replace FlashRoutine with a DOTween tween on
// _EmissionColor via SetPropertyBlock for a smoother, GC-free animation path.

using System;
using System.Collections;
using UnityEngine;
using VContainer;
using YassinTarek.SimonSays.Core.Domain;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;

namespace YassinTarek.SimonSays.Views
{
    public sealed class PanelAnimator : MonoBehaviour
    {
        [SerializeField] private PanelColor _color;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private float _flashBrightness = 3f;

        private IEventBus _eventBus;
        private MaterialPropertyBlock _mpb;
        private Coroutine _activeFlash;

        private Action<PanelActivatedEvent> _onPanelActivated;
        private Action<PlayerInputCorrectEvent> _onCorrect;
        private Action<PlayerInputWrongEvent> _onWrong;
        private Action<GameStartedEvent> _onGameStarted;
        private Action<InputEnabledEvent> _onInputEnabled;
        private Action<InputDisabledEvent> _onInputDisabled;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        [Inject]
        public void Construct(IEventBus eventBus)
        {
            _eventBus = eventBus;
            _onPanelActivated = HandlePanelActivated;
            _onCorrect = HandleCorrect;
            _onWrong = HandleWrong;
            _onGameStarted = HandleGameStarted;
            _onInputEnabled = HandleInputEnabled;
            _onInputDisabled = HandleInputDisabled;

            _eventBus.Subscribe(_onPanelActivated);
            _eventBus.Subscribe(_onCorrect);
            _eventBus.Subscribe(_onWrong);
            _eventBus.Subscribe(_onGameStarted);
            _eventBus.Subscribe(_onInputEnabled);
            _eventBus.Subscribe(_onInputDisabled);
        }

        private void HandlePanelActivated(PanelActivatedEvent evt)
        {
            if (evt.Color == _color)
                PlayFlashAnimation();
        }

        private void HandleCorrect(PlayerInputCorrectEvent evt)
        {
            if (evt.Color == _color)
                PlayCorrectAnimation();
        }

        private void HandleWrong(PlayerInputWrongEvent _) => PlayWrongAnimation();

        private void HandleGameStarted(GameStartedEvent _) => ResetVisualState();

        private void HandleInputEnabled(InputEnabledEvent _) { }

        private void HandleInputDisabled(InputDisabledEvent _) { }

        private void PlayFlashAnimation()
        {
            StopActiveFlash();
            _activeFlash = StartCoroutine(FlashRoutine(PanelColorToEmission(_color) * _flashBrightness, 0.4f));
        }

        private void PlayCorrectAnimation()
        {
            StopActiveFlash();
            _activeFlash = StartCoroutine(FlashRoutine(PanelColorToEmission(_color) * _flashBrightness, 0.2f));
        }

        private void PlayWrongAnimation()
        {
            StopActiveFlash();
            _activeFlash = StartCoroutine(FlashRoutine(Color.red * _flashBrightness, 0.4f));
        }

        private void StopActiveFlash()
        {
            if (_activeFlash != null)
            {
                StopCoroutine(_activeFlash);
                _activeFlash = null;
            }
        }

        private IEnumerator FlashRoutine(Color targetEmission, float duration)
        {
            var halfDuration = duration * 0.5f;
            var elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                SetEmission(Color.Lerp(Color.black, targetEmission, elapsed / halfDuration));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                SetEmission(Color.Lerp(targetEmission, Color.black, elapsed / halfDuration));
                yield return null;
            }

            SetEmission(Color.black);
            _activeFlash = null;
        }

        private void ResetVisualState()
        {
            StopActiveFlash();
            SetEmission(Color.black);
        }

        private void SetEmission(Color c)
        {
            if (_renderer == null)
                return;
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_EmissionColor", c);
            _renderer.SetPropertyBlock(_mpb);
        }

        private static Color PanelColorToEmission(PanelColor color) => color switch
        {
            PanelColor.Red => Color.red,
            PanelColor.Green => Color.green,
            PanelColor.Blue => Color.blue,
            PanelColor.Yellow => Color.yellow,
            _ => Color.white
        };

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe(_onPanelActivated);
            _eventBus?.Unsubscribe(_onCorrect);
            _eventBus?.Unsubscribe(_onWrong);
            _eventBus?.Unsubscribe(_onGameStarted);
            _eventBus?.Unsubscribe(_onInputEnabled);
            _eventBus?.Unsubscribe(_onInputDisabled);
        }
    }
}
