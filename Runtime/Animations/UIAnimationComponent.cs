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
            return UniTask.CompletedTask;
        }

        public virtual UniTask PlayHideAsync(Panel panel)
        {
            return UniTask.CompletedTask;
        }

        protected Panel ResolvePanel(Panel panel)
        {
            return panel != null ? panel : GetComponent<Panel>();
        }
    }
}
