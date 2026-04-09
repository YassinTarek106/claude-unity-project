using UnityEngine;
using VContainer;
using VContainer.Unity;
using YassinTarek.SimonSays.Bootstrap;
using YassinTarek.SimonSays.Config;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Infrastructure;

namespace YassinTarek.SimonSays.DI
{
    public sealed class RootLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameConfig _gameConfig;

        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_gameConfig);
            builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>();
            builder.Register<PlayerPrefsHighScoreRepository>(Lifetime.Singleton).As<IHighScoreRepository>();
            builder.Register<SceneLoaderService>(Lifetime.Singleton).As<ISceneLoaderService>();
            builder.Register<BootstrapEntry>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
