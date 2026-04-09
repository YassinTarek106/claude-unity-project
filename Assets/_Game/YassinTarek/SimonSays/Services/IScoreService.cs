namespace YassinTarek.SimonSays.Services
{
    public interface IScoreService
    {
        int Score { get; }
        void AddRoundScore(int round);
        void ResetScore();
    }
}
