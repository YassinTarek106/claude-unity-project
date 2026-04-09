namespace YassinTarek.SimonSays.Infrastructure
{
    public sealed class InMemoryHighScoreRepository : IHighScoreRepository
    {
        private int _value;

        public int Load() => _value;

        public void Save(int value) => _value = value;
    }
}
