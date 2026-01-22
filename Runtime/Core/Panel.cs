using System;
using System.Collections.Generic;
using Azathrix.EzUI.Animations;
using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Tools;
using Azathrix.GameKit.Runtime.Behaviours;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Azathrix.EzUI.Core
{
    public class Panel : GameScript
    {
        /// <summary>
        /// 自动注入
        /// </summary>
        [Inject]
        public UISystem UISystem { set; get; }

#if ODIN_INSPECTOR
        [LabelText("层级")]
#endif
        [SerializeField] private int _layer = 10;

#if ODIN_INSPECTOR
        [LabelText("动画组件")]
#endif
        [SerializeField] private UIAnimationComponent _animation;

        /// <summary>
        /// 使用Mask
        /// </summary>
        public virtual bool useMask => true;

        /// <summary>
        /// 当前Panel的路径,创建后初始化赋予
        /// </summary>
        public string path { private set; get; }

        public bool isAnimationPlaying { protected set; get; }

        public object userData { set; get; }

        /// <summary>
        /// 层级
        /// </summary>
        public virtual int layer => _layer;

        /// <summary>
        /// MainUI切换时的行为
        /// </summary>
        public virtual MainUIChangeBehavior mainUIChangeBehavior => MainUIChangeBehavior.Close;

        /// <summary>
        /// 动画组件
        /// </summary>
        public UIAnimationComponent animationComponent => _animation;

        private readonly List<View> _views = new List<View>();

        public void Initialize(string panelPath)
        {
            path = panelPath;
            EnsureAnimationComponent();
            try
            {
                OnCreate();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                foreach (var view in _views)
                    view.OnCreate();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private StateEnum _state = StateEnum.Hidden;

        [Flags]
        public enum StateEnum
        {
            None = 0,
            Show = 1 << 1,
            Shown = 1 << 2,
            Hide = 1 << 3,
            Hidden = 1 << 4,
            Close = 1 << 5,
        }

        protected void SetState(StateEnum state)
        {
            if (_state != state)
            {
                var last = _state;
                _state = state;
                OnStateChanged(last, state);
                UISystem?.NotifyPanelStateChanged(this, last, state);
            }
        }

        public bool IsState(StateEnum state)
        {
            return (_state & state) > 0;
        }

        protected virtual void OnStateChanged(StateEnum last, StateEnum cur)
        {
            switch (cur)
            {
                case StateEnum.None:
                    break;
                case StateEnum.Show:
                {
                    try
                    {
                        OnShow();
                        foreach (var view in _views) view.OnShow();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                    break;
                case StateEnum.Shown:
                {
                    try
                    {
                        OnShown();
                        foreach (var view in _views) view.OnShown();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                    break;
                case StateEnum.Hide:
                {
                    try
                    {
                        OnHide();
                        foreach (var view in _views) view.OnHide();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                    break;
                case StateEnum.Hidden:
                {
                    try
                    {
                        OnHidden();
                        foreach (var view in _views) view.OnHidden();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                    break;
                case StateEnum.Close:
                    try
                    {
                        OnClose();
                        foreach (var view in _views) view.OnClose();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cur), cur, null);
            }
        }

        protected virtual void OnClose()
        {
        }

        public virtual void Hide(bool useAnimation = true)
        {
            if (!IsState(StateEnum.Shown))
                return;
            HideAsync(useAnimation).Forget();
        }

        public virtual async UniTask HideAsync(bool useAnimation = true)
        {
            if (!IsState(StateEnum.Shown))
                return;

            if (!useAnimation)
            {
                SetState(StateEnum.Hide);
                gameObject.SetActive(false);
                SetState(StateEnum.Hidden);
                UISystem?.RefreshUI();
                return;
            }

            SetState(StateEnum.Hide);
            UISystem?.RefreshUI();

            await HideAnimationAsync();
            gameObject.SetActive(false);

            SetState(StateEnum.Hidden);
        }

        public async UniTask WaitEnd()
        {
            var cancel = gameObject.GetCancellationTokenOnDestroy();
            while (IsState(StateEnum.Show | StateEnum.Shown))
            {
                var flag = await UniTask.Yield(cancellationToken: cancel).SuppressCancellationThrow();
                if (flag)
                    return;
            }
        }

        public virtual void Show(bool useAnimation = true)
        {
            if (!IsState(StateEnum.Hidden))
                return;
            ShowAsync(useAnimation).Forget();
        }

        public virtual async UniTask ShowAsync(bool useAnimation = true)
        {
            if (!IsState(StateEnum.Hidden))
                return;
            if (!useAnimation)
            {
                SetState(StateEnum.Show);
                gameObject.SetActive(true);
                SetState(StateEnum.Shown);
                UISystem?.RefreshUI();
                return;
            }

            SetState(StateEnum.Show);
            gameObject.SetActive(true);

            UISystem?.RefreshUI();

            await ShowAnimationAsync();

            SetState(StateEnum.Shown);
        }

        public virtual void Close(bool useAnimation = true)
        {
            CloseAsync(useAnimation).Forget();
        }

        public virtual async UniTask CloseAsync(bool useAnimation = true)
        {
            if (!useAnimation)
            {
                SetState(StateEnum.Hide);
                OnHide();
                gameObject.SetActive(false);
                SetState(StateEnum.Hidden);
                OnHidden();
                SetState(StateEnum.Close);
                UISystem?.RefreshUI();
                UISystem?.Destroy(this);
                return;
            }

            if (!IsState(StateEnum.Hidden | StateEnum.Shown | StateEnum.Close))
                return;

            SetState(StateEnum.Hide);
            UISystem?.RefreshUI();

            await HideAnimationAsync();
            gameObject.SetActive(false);

            SetState(StateEnum.Hidden);

            SetState(StateEnum.Close);

            UISystem?.Destroy(this);
        }

        protected virtual async UniTask HideAnimationAsync()
        {
            EnsureAnimationComponent();
            if (_animation == null)
                return;

            isAnimationPlaying = true;

            try
            {
                await _animation.PlayHideAsync(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            isAnimationPlaying = false;
        }

        protected virtual async UniTask ShowAnimationAsync()
        {
            EnsureAnimationComponent();
            if (_animation == null)
                return;

            isAnimationPlaying = true;

            try
            {
                await _animation.PlayShowAsync(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            isAnimationPlaying = false;
        }

        internal CanvasGroup GetOrAddCanvasGroup()
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            return group;
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnHidden()
        {
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnCreate()
        {
        }

        public void RegisterView(View view)
        {
            _views.Add(view);
        }

        private void EnsureAnimationComponent()
        {
            if (_animation == null)
                _animation = GetComponent<UIAnimationComponent>();

            if (_animation != null)
            {
                AzathrixFramework.InjectTo(_animation);
            }
        }

    }
}
