using NUnit.Framework;
using YassinTarek.SimonSays.Core.Domain;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;
using YassinTarek.SimonSays.Services;

namespace YassinTarek.Tests.EditMode
{
    public sealed class GameStateMachineTests
    {
        private EventBus _bus;
        private GameStateMachine _machine;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _machine = new GameStateMachine(_bus);
        }

        [Test]
        public void InitialState_IsIdle()
        {
            Assert.AreEqual(GameState.Idle, _machine.Current);
        }

        [Test]
        public void TransitionTo_ChangesCurrentState()
        {
            _machine.TransitionTo(GameState.PlayingSequence);
            Assert.AreEqual(GameState.PlayingSequence, _machine.Current);
        }

        [Test]
        public void TransitionTo_PublishesGameStateChangedEvent()
        {
            GameStateChangedEvent? received = null;
            void Handler(GameStateChangedEvent evt) => received = evt;
            _bus.Subscribe<GameStateChangedEvent>(Handler);

            _machine.TransitionTo(GameState.WaitingForInput);

            _bus.Unsubscribe<GameStateChangedEvent>(Handler);

            Assert.IsNotNull(received);
            Assert.AreEqual(GameState.Idle, received?.Prev);
            Assert.AreEqual(GameState.WaitingForInput, received?.Next);
        }

        [Test]
        public void Is_ReturnsTrueForCurrentState()
        {
            Assert.IsTrue(_machine.Is(GameState.Idle));
        }

        [Test]
        public void Is_ReturnsFalseForOtherState()
        {
            Assert.IsFalse(_machine.Is(GameState.PlayingSequence));
        }

        [Test]
        public void TransitionTo_MultipleSteps_TracksCorrectly()
        {
            _machine.TransitionTo(GameState.PlayingSequence);
            _machine.TransitionTo(GameState.WaitingForInput);
            _machine.TransitionTo(GameState.GameOver);

            Assert.AreEqual(GameState.GameOver, _machine.Current);
            Assert.IsTrue(_machine.Is(GameState.GameOver));
        }

        [Test]
        public void TransitionTo_EventCarriesPreviousState()
        {
            _machine.TransitionTo(GameState.PlayingSequence);

            GameStateChangedEvent? received = null;
            void Handler(GameStateChangedEvent evt) => received = evt;
            _bus.Subscribe<GameStateChangedEvent>(Handler);

            _machine.TransitionTo(GameState.WaitingForInput);

            _bus.Unsubscribe<GameStateChangedEvent>(Handler);

            Assert.AreEqual(GameState.PlayingSequence, received?.Prev);
        }
    }
}
