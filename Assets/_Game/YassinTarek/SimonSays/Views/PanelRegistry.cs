using UnityEngine;
using VContainer;
using YassinTarek.SimonSays.Core.EventBus;

namespace YassinTarek.SimonSays.Views
{
    public sealed class PanelRegistry : MonoBehaviour
    {
        [SerializeField] private PanelView[] _panelViews;
        [SerializeField] private PanelAnimator[] _panelAnimators;

        public PanelView[] PanelViews => _panelViews;
        public PanelAnimator[] PanelAnimators => _panelAnimators;

        [Inject]
        public void Construct(IEventBus eventBus)
        {
            foreach (var view in _panelViews)
                view.Construct(eventBus);
            foreach (var animator in _panelAnimators)
                animator.Construct(eventBus);
        }
    }
}
