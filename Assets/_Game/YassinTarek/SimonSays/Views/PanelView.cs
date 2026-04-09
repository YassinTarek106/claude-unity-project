using System;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using YassinTarek.SimonSays.Core.Domain;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;

namespace YassinTarek.SimonSays.Views
{
    public sealed class PanelView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private PanelColor _color;

        private IEventBus _eventBus;
        private bool _isInputEnabled;

        private Action<InputEnabledEvent> _onInputEnabled;
        private Action<InputDisabledEvent> _onInputDisabled;

        [Inject]
        public void Construct(IEventBus eventBus)
        {
            _eventBus = eventBus;
            _onInputEnabled = HandleInputEnabled;
            _onInputDisabled = HandleInputDisabled;
            _eventBus.Subscribe(_onInputEnabled); 
            _eventBus.Subscribe(_onInputDisabled);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"Panel {name} received click. Input enabled: {_isInputEnabled}");
            if (!_isInputEnabled)
                return;
            _eventBus.Publish(new PlayerInputReceivedEvent { Color = _color });
        }

        private void HandleInputEnabled(InputEnabledEvent _) => _isInputEnabled = true;

        private void HandleInputDisabled(InputDisabledEvent _) => _isInputEnabled = false;

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe(_onInputEnabled);
            _eventBus?.Unsubscribe(_onInputDisabled);
        }
    }
}
