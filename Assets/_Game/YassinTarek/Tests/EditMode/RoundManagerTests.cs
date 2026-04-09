using NUnit.Framework;
using YassinTarek.SimonSays.Services;

namespace YassinTarek.Tests.EditMode
{
    public sealed class RoundManagerTests
    {
        private RoundManager _manager;

        [SetUp]
        public void SetUp() => _manager = new RoundManager();

        [Test]
        public void InitialState_RoundIsZeroAndSequenceIsEmpty()
        {
            Assert.AreEqual(0, _manager.CurrentRound);
            Assert.AreEqual(0, _manager.Sequence.Count);
        }

        [Test]
        public void StartNewRound_IncrementsCurrentRound()
        {
            _manager.StartNewRound();
            Assert.AreEqual(1, _manager.CurrentRound);

            _manager.StartNewRound();
            Assert.AreEqual(2, _manager.CurrentRound);
        }

        [Test]
        public void StartNewRound_GrowsSequenceByOne()
        {
            _manager.StartNewRound();
            Assert.AreEqual(1, _manager.Sequence.Count);

            _manager.StartNewRound();
            Assert.AreEqual(2, _manager.Sequence.Count);
        }

        [Test]
        public void GetExpectedColor_AfterStartNewRound_ReturnsFirstElement()
        {
            _manager.StartNewRound();
            Assert.AreEqual(_manager.Sequence[0], _manager.GetExpectedColor());
        }

        [Test]
        public void AdvanceStep_MovesToNextExpectedColor()
        {
            _manager.StartNewRound();
            _manager.StartNewRound();
            var firstColor = _manager.GetExpectedColor();
            Assert.AreEqual(_manager.Sequence[0], firstColor);

            _manager.AdvanceStep();
            Assert.AreEqual(_manager.Sequence[1], _manager.GetExpectedColor());
        }

        [Test]
        public void AdvanceStep_ReturnsFalse_WhenMoreStepsRemain()
        {
            _manager.StartNewRound();
            _manager.StartNewRound();

            var roundComplete = _manager.AdvanceStep();
            Assert.IsFalse(roundComplete);
        }

        [Test]
        public void AdvanceStep_ReturnsTrue_WhenLastStepReached()
        {
            _manager.StartNewRound();

            var roundComplete = _manager.AdvanceStep();
            Assert.IsTrue(roundComplete);
        }

        [Test]
        public void Reset_ClearsRoundAndSequence()
        {
            _manager.StartNewRound();
            _manager.StartNewRound();

            _manager.Reset();

            Assert.AreEqual(0, _manager.CurrentRound);
            Assert.AreEqual(0, _manager.Sequence.Count);
        }

        [Test]
        public void Reset_AllowsStartingFresh()
        {
            _manager.StartNewRound();
            _manager.Reset();
            _manager.StartNewRound();

            Assert.AreEqual(1, _manager.CurrentRound);
            Assert.AreEqual(1, _manager.Sequence.Count);
        }
    }
}
