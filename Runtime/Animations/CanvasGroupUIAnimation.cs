using Azathrix.EzUI.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azathrix.EzUI.Animations
{
    /// <summary>
    /// CanvasGroup 淡入淡出动画组件
    /// </summary>
    [AddComponentMenu("EzUI/Animations/CanvasGroup UI Animation")]
    public class CanvasGroupUIAnimation : UIAnimationComponent
    {
        [SerializeField] private float _showDuration = 0.2f;
        [SerializeField] private float _hideDuration = 0.1f;
        [SerializeField] private float _showFrom = 0f;
        [SerializeField] private float _hideTo = 0f;

        public override async UniTask PlayShowAsync(Panel panel)
        {
            var target = ResolvePanel(panel);
            if (target == null) return;

            var group = target.GetOrAddCanvasGroup();
            group.alpha = _showFrom;

            float elapsed = 0f;
            float duration = Mathf.Max(0.0001f, _showDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / duration);
                await UniTask.Yield();
            }

            group.alpha = 1f;
        }

        public override async UniTask PlayHideAsync(Panel panel)
        {
            var target = ResolvePanel(panel);
            if (target == null) return;

            var group = target.GetOrAddCanvasGroup();
            group.alpha = 1f;

            float elapsed = 0f;
            float duration = Mathf.Max(0.0001f, _hideDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(1f, _hideTo, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield();
            }

            group.alpha = _hideTo;
        }
    }
}
