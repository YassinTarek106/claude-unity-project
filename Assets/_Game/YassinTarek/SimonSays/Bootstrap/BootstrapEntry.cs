using VContainer.Unity;
using YassinTarek.SimonSays.Infrastructure;

namespace YassinTarek.SimonSays.Bootstrap
{
    public sealed class BootstrapEntry : IStartable
    {
        private readonly ISceneLoaderService _sceneLoader;

        public BootstrapEntry(ISceneLoaderService sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public void Start() => _sceneLoader.LoadMainMenu();
    }
}
