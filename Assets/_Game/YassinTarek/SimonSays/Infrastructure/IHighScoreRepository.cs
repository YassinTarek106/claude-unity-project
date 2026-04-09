namespace YassinTarek.SimonSays.Infrastructure
{
    public interface IHighScoreRepository
    {
        int Load();
        void Save(int value);
    }
}
