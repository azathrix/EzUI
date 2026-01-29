using System.Threading;
using Azathrix.EzUI.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azathrix.EzUI.Animations
{
    /// <summary>
    /// UI 动画组件基类
    /// </summary>
    public abstract class UIAnimationComponent : MonoBehaviour
    {
        public virtual UniTask PlayShowAsync(Panel panel)
        {
            return PlayShowAsync(panel, CancellationToken.None);
        }

        public virtual UniTask PlayShowAsync(Panel panel, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask PlayHideAsync(Panel panel)
        {
            return PlayHideAsync(panel, CancellationToken.None);
        }

        public virtual UniTask PlayHideAsync(Panel panel, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        protected Panel ResolvePanel(Panel panel)
        {
            return panel != null ? panel : GetComponent<Panel>();
        }
    }
}
