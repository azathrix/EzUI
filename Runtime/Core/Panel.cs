using System;
using System.Collections.Generic;
using System.Threading;
using Azathrix.EzUI.Animations;
using Azathrix.EzUI.Events;
using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Tools;
using Azathrix.GameKit.Runtime.Behaviours;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Azathrix.EzUI.Core
{
    public enum AutoCloseBehavior
    {
        None,
        Hide,
        Close
    }

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
        [SerializeField]
        private int _layer = 10;

#if ODIN_INSPECTOR
        [LabelText("使用遮罩")]
#endif
        [SerializeField]
        private bool _useMask = true;
        
#if ODIN_INSPECTOR
        [LabelText("动画")]
        [HideInInspector]
#endif
        [SerializeField]
        private UIAnimationComponent _animation;

        /// <summary>
        /// 使用Mask
        /// </summary>
        public virtual bool useMask => _useMask;

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
        /// 遮罩颜色（可重载，返回 null 使用全局设置）
        /// </summary>
        public virtual Color? maskColor => null;

        /// <summary>
        /// 动画组件
        /// </summary>
        public UIAnimationComponent animationComponent => _animation;

        /// <summary>
        /// 动画播放时是否屏蔽输入（可重载，默认使用全局设置）
        /// </summary>
        public virtual bool blockInputDuringAnimation => EzUISettings.Instance?.blockInputDuringAnimation ?? true;

        /// <summary>
        /// 获取自动关闭行为（可重载以自定义行为）
        /// </summary>
        /// <param name="reason">关闭原因</param>
        public virtual AutoCloseBehavior GetAutoCloseType(AutoCloseReason reason)
        {
            return mainUIChangeBehavior switch
            {
                MainUIChangeBehavior.Hide => AutoCloseBehavior.Hide,
                MainUIChangeBehavior.Close => AutoCloseBehavior.Close,
                _ => AutoCloseBehavior.None
            };
        }

        private readonly List<View> _views = new List<View>();
        private bool _initialized;
        private CancellationTokenSource _animationCts;

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

            _initialized = true;
        }

        private StateEnum _state = StateEnum.Hidden;

        public enum StateEnum
        {
            /// <summary>
            /// 隐藏（初始状态）
            /// </summary>
            Hidden,

            /// <summary>
            /// 已显示
            /// </summary>
            Shown,

            /// <summary>
            /// 已关闭（终态）
            /// </summary>
            Closed,
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public StateEnum State => _state;

        /// <summary>
        /// 是否可见（已显示或正在显示动画中）
        /// </summary>
        public bool IsVisible => _state == StateEnum.Shown || (_state == StateEnum.Hidden && _isShowing);

        /// <summary>
        /// 是否正在显示动画
        /// </summary>
        private bool _isShowing;

        /// <summary>
        /// 是否正在隐藏动画
        /// </summary>
        private bool _isHiding;

        internal bool IsShowingInternal
        {
            get => _isShowing;
            set => _isShowing = value;
        }

        internal bool IsHidingInternal
        {
            get => _isHiding;
            set => _isHiding = value;
        }

        protected void SetState(StateEnum state)
        {
            if (_state == state)
                return;

            var last = _state;
            _state = state;
            OnStateChanged(last, state);
            UISystem?.NotifyPanelStateChanged(this, last, state);
        }

        internal void SetStateInternal(StateEnum state)
        {
            SetState(state);
        }

        public bool IsState(StateEnum state)
        {
            return _state == state;
        }

        protected virtual void OnStateChanged(StateEnum last, StateEnum cur)
        {
            switch (cur)
            {
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
                case StateEnum.Closed:
                    break;
            }
        }

        protected virtual void OnClose()
        {
        }

        protected virtual void OnClosed()
        {
        }

        internal void InvokeOnClose()
        {
            try
            {
                OnClose();
                foreach (var view in _views)
                    view.OnClose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal void InvokeOnClosed()
        {
            try
            {
                OnClosed();
                foreach (var view in _views)
                    view.OnClosed();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal void CancelAnimation()
        {
            if (_animationCts != null)
            {
                _animationCts.Cancel();
                _animationCts.Dispose();
                _animationCts = null;
            }

            isAnimationPlaying = false;
            _isShowing = false;
            _isHiding = false;
        }

        internal CancellationToken StartAnimationToken()
        {
            CancelAnimation();
            _animationCts = new CancellationTokenSource();
            return _animationCts.Token;
        }

        private string ResolveSystemPath()
        {
            if (!string.IsNullOrWhiteSpace(path))
                return path;
            return UISystem != null ? UISystem.GetPath(GetType()) : null;
        }

        public void Show(bool useAnimation = true)
        {
            var p = ResolveSystemPath();
            if (UISystem == null || string.IsNullOrWhiteSpace(p))
                return;
            UISystem.Show(p, useAnimation, userData);
        }

        public async UniTask<Panel> ShowAsync(bool useAnimation = true)
        {
            var p = ResolveSystemPath();
            if (UISystem == null || string.IsNullOrWhiteSpace(p))
                return null;
            return await UISystem.ShowAsync(p, useAnimation, userData);
        }

        public void Hide(bool useAnimation = true)
        {
            var p = ResolveSystemPath();
            if (UISystem == null || string.IsNullOrWhiteSpace(p))
                return;
            UISystem.Hide(p, useAnimation);
        }

        public async UniTask HideAsync(bool useAnimation = true)
        {
            var p = ResolveSystemPath();
            if (UISystem == null || string.IsNullOrWhiteSpace(p))
                return;
            await UISystem.HideAsync(p, useAnimation);
        }

        public void Close(bool useAnimation = true)
        {
            var p = ResolveSystemPath();
            if (UISystem == null || string.IsNullOrWhiteSpace(p))
                return;
            UISystem.Close(p, useAnimation);
        }

        public async UniTask CloseAsync(bool useAnimation = true)
        {
            var p = ResolveSystemPath();
            if (UISystem == null || string.IsNullOrWhiteSpace(p))
                return;
            await UISystem.CloseAsync(p, useAnimation);
        }

        public async UniTask WaitEnd()
        {
            var cancel = gameObject.GetCancellationTokenOnDestroy();
            while (IsState(StateEnum.Shown) || _isShowing)
            {
                var flag = await UniTask.Yield(cancellationToken: cancel).SuppressCancellationThrow();
                if (flag)
                    return;
            }
        }

        protected virtual async UniTask HideAnimationAsync(CancellationToken cancellationToken)
        {
            EnsureAnimationComponent();
            if (_animation == null)
                return;

            isAnimationPlaying = true;
            DispatchAnimationStateChanged(true);

            try
            {
                await _animation.PlayHideAsync(this, cancellationToken);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            isAnimationPlaying = false;
            DispatchAnimationStateChanged(false);
        }

        protected virtual async UniTask ShowAnimationAsync(CancellationToken cancellationToken)
        {
            EnsureAnimationComponent();
            if (_animation == null)
                return;

            isAnimationPlaying = true;
            DispatchAnimationStateChanged(true);

            try
            {
                await _animation.PlayShowAsync(this, cancellationToken);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            isAnimationPlaying = false;
            DispatchAnimationStateChanged(false);
        }

        internal UniTask RunHideAnimationAsync(CancellationToken cancellationToken)
        {
            return HideAnimationAsync(cancellationToken);
        }

        internal UniTask RunShowAnimationAsync(CancellationToken cancellationToken)
        {
            return ShowAnimationAsync(cancellationToken);
        }

        private void DispatchAnimationStateChanged(bool isPlaying)
        {
            AzathrixFramework.Dispatcher.Dispatch(new UIAnimationStateChanged
            {
                isPlaying = isPlaying,
                source = this,
                blockInput = blockInputDuringAnimation
            });
        }

        internal CanvasGroup GetOrAddCanvasGroup()
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            return group;
        }

        internal void InvokeOnShow()
        {
            try
            {
                AzathrixFramework.Dispatcher.Dispatch(new UIPanelShow
                {
                    panel = this,
                    path = path
                });
                OnShow();
                foreach (var view in _views)
                    view.OnShow();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal void InvokeOnHide()
        {
            try
            {
                AzathrixFramework.Dispatcher.Dispatch(new UIPanelHide
                {
                    panel = this,
                    path = path
                });
                OnHide();
                foreach (var view in _views)
                    view.OnHide();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
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
            if (view == null || _views.Contains(view))
                return;
            _views.Add(view);
            if (!_initialized)
                return;

            try
            {
                view.OnCreate();
                if (_isShowing && IsState(StateEnum.Hidden))
                    return;
                if (IsVisible)
                    view.OnShow();
                if (IsState(StateEnum.Shown))
                    view.OnShown();
                else if (!IsVisible && IsState(StateEnum.Hidden))
                    view.OnHidden();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
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
