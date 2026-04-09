using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YassinTarek.SimonSays.Config;
using YassinTarek.SimonSays.Core.Domain;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;
using YassinTarek.SimonSays.Infrastructure;

namespace YassinTarek.SimonSays.Services
{
    public sealed class SequenceService : ISequenceService
    {
        private readonly ICoroutineRunner _runner;
        private readonly IAudioService _audioService;
        private readonly IEventBus _eventBus;
        private readonly GameConfig _config;

        private Coroutine _activeCoroutine;

        public SequenceService(
            ICoroutineRunner runner,
            IAudioService audioService,
            IEventBus eventBus,
            GameConfig config)
        {
            _runner = runner;
            _audioService = audioService;
            _eventBus = eventBus;
            _config = config;
        }

        public void PlaySequence(IReadOnlyList<PanelColor> sequence, Action onComplete)
        {
            _runner.StopRoutine(_activeCoroutine);
            _activeCoroutine = _runner.StartRoutine(PlaySequenceRoutine(sequence, onComplete));
        }

        private IEnumerator PlaySequenceRoutine(IReadOnlyList<PanelColor> sequence, Action onComplete)
        {
            foreach (var color in sequence)
            {
                _eventBus.Publish(new PanelActivatedEvent { Color = color });
                _audioService.Play(PanelColorToSoundId(color));
                yield return new WaitForSeconds(_config.SequenceStepDuration);
                yield return new WaitForSeconds(_config.SequenceStepGap);
            }
            _activeCoroutine = null;
            onComplete?.Invoke();
        }

        private static SoundId PanelColorToSoundId(PanelColor color) => color switch
        {
            PanelColor.Red => SoundId.RedTone,
            PanelColor.Green => SoundId.GreenTone,
            PanelColor.Blue => SoundId.BlueTone,
            PanelColor.Yellow => SoundId.YellowTone,
            _ => SoundId.RedTone
        };
    }
}
