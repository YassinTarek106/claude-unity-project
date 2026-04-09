using VContainer.Unity;
using YassinTarek.SimonSays.Services;

namespace YassinTarek.SimonSays.Bootstrap
{
    public sealed class GameplayEntryPoint : IStartable
    {
        private readonly GameService _gameService;

        public GameplayEntryPoint(GameService gameService)
        {
            _gameService = gameService;
        }

        public void Start() => _gameService.StartGame();
    }
}
