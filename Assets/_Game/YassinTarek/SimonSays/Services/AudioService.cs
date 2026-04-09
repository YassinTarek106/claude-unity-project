using System;
using System.Collections.Generic;
using UnityEngine;
using YassinTarek.SimonSays.Core.Domain;

namespace YassinTarek.SimonSays.Services
{
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        [Serializable]
        public struct SoundEntry
        {
            public SoundId Id;
            public AudioClip Clip;
        }

        [SerializeField] private SoundEntry[] _soundMap;

        private readonly List<AudioSource> _pool = new();

        public void Play(SoundId id)
        {
            var clip = FindClip(id);
            if (clip == null)
                return;

            var source = GetIdlePooledSource();
            source.clip = clip;
            source.Play();
        }

        private AudioClip FindClip(SoundId id)
        {
            foreach (var entry in _soundMap)
            {
                if (entry.Id == id)
                    return entry.Clip;
            }
            return null;
        }

        private AudioSource GetIdlePooledSource()
        {
            foreach (var source in _pool)
            {
                if (!source.isPlaying)
                    return source;
            }
            var newSource = gameObject.AddComponent<AudioSource>();
            _pool.Add(newSource);
            return newSource;
        }
    }
}
