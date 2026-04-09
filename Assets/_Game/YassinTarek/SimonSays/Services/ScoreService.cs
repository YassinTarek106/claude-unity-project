using VContainer;
using VContainer.Unity;
using YassinTarek.SimonSays.Config;
using YassinTarek.SimonSays.Core.EventBus;
using YassinTarek.SimonSays.Core.Events;
using YassinTarek.SimonSays.Infrastructure;

namespace YassinTarek.SimonSays.Services
{
    public sealed class ScoreService : IScoreService, IInitializable
    {
        private readonly IEventBus _eventBus;
        private readonly IHighScoreRepository _repository;
        private readonly GameConfig _config;

        private int _score;
        private int _highScore;

        public int Score => _score;

        public ScoreService(IEventBus eventBus, IHighScoreRepository repository, GameConfig config)
        {
            _eventBus = eventBus;
            _repository = repository;
            _config = config;
        }

        public void Initialize()
        {
            _highScore = _repository.Load();
        }

        public void AddRoundScore(int round)
        {
            _score += round * _config.PointsPerRound;
            if (_score > _highScore)
            {
                _highScore = _score;
                _repository.Save(_highScore);
            }
            _eventBus.Publish(new ScoreChangedEvent { Score = _score, HighScore = _highScore });
        }

        public void ResetScore()
        {
            _score = 0;
        }
    }
}
