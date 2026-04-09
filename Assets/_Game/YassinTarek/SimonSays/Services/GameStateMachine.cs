using YassinTarek.SimonSays.Core.Domain;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;

namespace YassinTarek.SimonSays.Services
{
    public sealed class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.Idle;

        private readonly IEventBus _eventBus;

        public GameStateMachine(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void TransitionTo(GameState next)
        {
            var prev = Current;
            Current = next;
            _eventBus.Publish(new GameStateChangedEvent { Prev = prev, Next = next });
        }

        public bool Is(GameState state) => Current == state;
    }
}
