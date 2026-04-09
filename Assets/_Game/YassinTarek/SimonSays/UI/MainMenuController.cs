using UnityEngine;
using UnityEngine.UI;
using VContainer;
using YassinTarek.SimonSays.Infrastructure;

namespace YassinTarek.SimonSays.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _startButton;

        private ISceneLoaderService _sceneLoader;

        [Inject]
        public void Construct(ISceneLoaderService sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        private void Start()
        {
            _startButton.onClick.AddListener(OnStartClicked);
            gameObject.SetActive(true);
        }

        private void OnStartClicked() => _sceneLoader.LoadGameplay();

        private void OnDestroy()
        {
            _startButton?.onClick.RemoveListener(OnStartClicked);
        }
    }
}
