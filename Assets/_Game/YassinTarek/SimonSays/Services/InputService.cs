using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;

namespace YassinTarek.SimonSays.Services
{
    public sealed class InputService : IInputService
    {
        private readonly IEventBus _eventBus;
        private bool _isEnabled;

        public InputService(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Enable()
        {
            if (_isEnabled)
                return;
            _isEnabled = true;
            _eventBus.Publish(new InputEnabledEvent());
        }

        public void Disable()
        {
            if (!_isEnabled)
                return;
            _isEnabled = false;
            _eventBus.Publish(new InputDisabledEvent());
        }
    }
}
