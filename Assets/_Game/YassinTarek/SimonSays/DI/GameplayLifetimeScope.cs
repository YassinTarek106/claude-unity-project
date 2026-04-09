using UnityEngine;
using VContainer;
using VContainer.Unity;
using YassinTarek.SimonSays.Bootstrap;
using YassinTarek.SimonSays.Infrastructure;
using YassinTarek.SimonSays.Services;
using YassinTarek.SimonSays.UI;
using YassinTarek.SimonSays.Views;

namespace YassinTarek.SimonSays.DI
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private CoroutineRunner _coroutineRunner;
        [SerializeField] private AudioService _audioService;
        [SerializeField] private PanelRegistry _panelRegistry;
        [SerializeField] private HudController _hudController;
        [SerializeField] private GameOverController _gameOverController;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_coroutineRunner).As<ICoroutineRunner>();
            builder.RegisterComponent(_audioService).As<IAudioService>();

            builder.Register<GameStateMachine>(Lifetime.Singleton);
            builder.Register<RoundManager>(Lifetime.Singleton);
            builder.Register<GameService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SequenceService>(Lifetime.Singleton).As<ISequenceService>();
            builder.Register<InputService>(Lifetime.Singleton).As<IInputService>();
            builder.Register<ScoreService>(Lifetime.Singleton).AsImplementedInterfaces();

            builder.RegisterComponent(_panelRegistry);

            builder.RegisterComponent(_hudController);
            builder.RegisterComponent(_gameOverController);

            builder.Register<GameplayEntryPoint>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
