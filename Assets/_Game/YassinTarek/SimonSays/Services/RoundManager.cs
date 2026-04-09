using System;
using System.Collections.Generic;
using YassinTarek.SimonSays.Core.Domain;

namespace YassinTarek.SimonSays.Services
{
    public sealed class RoundManager
    {
        private readonly Random _random = new();
        private readonly List<PanelColor> _sequence = new();
        private int _expectedStepIndex;

        public int CurrentRound { get; private set; }
        public IReadOnlyList<PanelColor> Sequence => _sequence;

        public void StartNewRound()
        {
            CurrentRound++;
            _sequence.Add((PanelColor)_random.Next(0, 4));
            _expectedStepIndex = 0;
        }

        public PanelColor GetExpectedColor() => _sequence[_expectedStepIndex];

        public bool AdvanceStep()
        {
            _expectedStepIndex++;
            return _expectedStepIndex >= _sequence.Count;
        }

        public void Reset()
        {
            CurrentRound = 0;
            _sequence.Clear();
            _expectedStepIndex = 0;
        }
    }
}
