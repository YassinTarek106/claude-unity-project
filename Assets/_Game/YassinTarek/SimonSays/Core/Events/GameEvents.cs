using YassinTarek.SimonSays.Core.Domain;

namespace YassinTarek.SimonSays.Core.Events
{
    public struct GameStartedEvent { }

    public struct GameOverEvent
    {
        public int FinalScore;
    }

    public struct RoundStartedEvent
    {
        public int Round;
    }

    public struct RoundWonEvent
    {
        public int Round;
    }

    public struct PanelActivatedEvent
    {
        public PanelColor Color;
    }

    public struct PlayerInputReceivedEvent
    {
        public PanelColor Color;
    }

    public struct PlayerInputCorrectEvent
    {
        public PanelColor Color;
    }

    public struct PlayerInputWrongEvent { }

    public struct ScoreChangedEvent
    {
        public int Score;
        public int HighScore;
    }

    public struct InputEnabledEvent { }

    public struct InputDisabledEvent { }

    public struct GameStateChangedEvent
    {
        public GameState Prev;
        public GameState Next;
    }
}
