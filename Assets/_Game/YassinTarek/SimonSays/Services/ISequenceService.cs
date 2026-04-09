using System;
using System.Collections.Generic;
using YassinTarek.SimonSays.Core.Domain;

namespace YassinTarek.SimonSays.Services
{
    public interface ISequenceService
    {
        void PlaySequence(IReadOnlyList<PanelColor> sequence, Action onComplete);
    }
}
