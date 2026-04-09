using UnityEngine;
using VContainer;
using VContainer.Unity;
using YassinTarek.SimonSays.UI;

namespace YassinTarek.SimonSays.DI
{
    public sealed class MainMenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private MainMenuController _mainMenuController;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_mainMenuController);
        }
    }
}
