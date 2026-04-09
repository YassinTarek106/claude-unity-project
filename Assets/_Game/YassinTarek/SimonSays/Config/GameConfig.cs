using UnityEngine;

namespace YassinTarek.SimonSays.Config
{
    [CreateAssetMenu(menuName = "SimonSays/GameConfig", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [field: SerializeField] public int PointsPerRound { get; private set; } = 10;
        [field: SerializeField] public float SequenceStepDuration { get; private set; } = 0.6f;
        [field: SerializeField] public float SequenceStepGap { get; private set; } = 0.15f;
        [field: SerializeField] public float PanelFlashDuration { get; private set; } = 0.3f;
        [field: SerializeField] public int InitialSequenceLength { get; private set; } = 1;

        [Header("Transition Delays")]
        [field: SerializeField] public float GameStartDelay { get; private set; } = 1f;
        [field: SerializeField] public float RoundTransitionDelay { get; private set; } = 1.2f;
        [field: SerializeField] public float GameOverDelay { get; private set; } = 1f;
    }
}
