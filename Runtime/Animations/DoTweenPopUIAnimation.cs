#if DOTWEEN
using Azathrix.EzUI.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Azathrix.EzUI.Animations
{
    /// <summary>
    /// DoTween 弹窗缩放动画组件
    /// </summary>
    [AddComponentMenu("EzUI/Animations/DoTween Pop UI Animation")]
    public class DoTweenPopUIAnimation : UIAnimationComponent
    {
        [SerializeField] private float _duration = 0.2f;
        [SerializeField] private float _fromScale = 0.5f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        public override async UniTask PlayShowAsync(Panel panel)
        {
            var target = ResolvePanel(panel);
            if (target == null) return;

            target.transform.localScale = Vector3.one * _fromScale;
            target.transform.DOScale(Vector3.one, _duration).SetEase(_showEase).SetUpdate(true);

            await UniTask.WaitForSeconds(_duration, true);
        }

        public override async UniTask PlayHideAsync(Panel panel)
        {
            var target = ResolvePanel(panel);
            if (target == null) return;

            target.transform.DOScale(Vector3.one * _fromScale, _duration).SetEase(_hideEase).SetUpdate(true);
            await UniTask.WaitForSeconds(_duration, true);
        }
    }
}
#endif
