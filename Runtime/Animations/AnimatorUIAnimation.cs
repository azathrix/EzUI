using System.Threading;
using Azathrix.EzUI.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azathrix.EzUI.Animations
{
    /// <summary>
    /// Animator 动画组件
    /// </summary>
    [AddComponentMenu("EzUI/Animations/Animator UI Animation")]
    public class AnimatorUIAnimation : UIAnimationComponent
    {
        [SerializeField] private string _showStateName = "show";
        [SerializeField] private string _hideStateName = "hide";

        public override async UniTask PlayShowAsync(Panel panel, CancellationToken cancellationToken)
        {
            var target = ResolvePanel(panel);
            if (target == null) return;

            var animator = target.GetComponent<Animator>();
            if (animator == null) return;

            animator.Play(_showStateName);
            await UniTask.Yield(cancellationToken);
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            await UniTask.WaitForSeconds(stateInfo.length, true, cancellationToken: cancellationToken);
        }

        public override async UniTask PlayHideAsync(Panel panel, CancellationToken cancellationToken)
        {
            var target = ResolvePanel(panel);
            if (target == null) return;

            var animator = target.GetComponent<Animator>();
            if (animator == null) return;

            animator.Play(_hideStateName);
            await UniTask.Yield(cancellationToken);
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            await UniTask.WaitForSeconds(stateInfo.length, true, cancellationToken: cancellationToken);
        }
    }
}
