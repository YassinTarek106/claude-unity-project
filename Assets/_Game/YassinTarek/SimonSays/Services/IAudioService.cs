using YassinTarek.SimonSays.Core.Domain;

namespace YassinTarek.SimonSays.Services
{
    public interface IAudioService
    {
        void Play(SoundId id);
    }
}
