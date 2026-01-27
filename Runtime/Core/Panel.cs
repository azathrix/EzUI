using System;
using System.Collections.Generic;
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
        /// 自动关闭时调用（可重载以自定义行为）
        /// </summary>
        /// <param name="reason">关闭原因</param>
        /// <param name="useAnimation">是否使用动画</param>
        public virtual void OnAutoClose(AutoCloseReason reason, bool useAnimation = true)
        {
            // 默认使用 mainUIChangeBehavior 的行为
            switch (mainUIChangeBehavior)
            {
                case MainUIChangeBehavior.None:
                    break;
                case MainUIChangeBehavior.Hide:
                    if (IsState(StateEnum.Shown))
                        Hide(useAnimation);
                    break;
                case MainUIChangeBehavior.Close:
                    if (!IsState(StateEnum.Closed))
                        Close(useAnimation);
                    break;
            }
        }

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

        protected void SetState(StateEnum state)
        {
            if (_state == state)
                return;

            var last = _state;
            _state = state;
            OnStateChanged(last, state);
            UISystem?.NotifyPanelStateChanged(this, last, state);
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

            _isHiding = true;

            if (!useAnimation)
            {
                gameObject.SetActive(false);
                SetState(StateEnum.Hidden);
                UISystem?.RefreshUI();
                _isHiding = false;
                return;
            }

            UISystem?.RefreshUI();

            await HideAnimationAsync();
            gameObject.SetActive(false);

            SetState(StateEnum.Hidden);
            _isHiding = false;
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

            _isShowing = true;

            if (!useAnimation)
            {
                gameObject.SetActive(true);
                SetState(StateEnum.Shown);
                UISystem?.RefreshUI();
                _isShowing = false;
                return;
            }

            gameObject.SetActive(true);

            UISystem?.RefreshUI();

            await ShowAnimationAsync();

            SetState(StateEnum.Shown);
            _isShowing = false;
        }

        public virtual void Close(bool useAnimation = true)
        {
            CloseAsync(useAnimation).Forget();
        }

        public virtual async UniTask CloseAsync(bool useAnimation = true)
        {
            if (IsState(StateEnum.Closed))
                return;

            _isHiding = true;

            if (!useAnimation)
            {
                gameObject.SetActive(false);
                SetState(StateEnum.Hidden);
                SetState(StateEnum.Closed);
                UISystem?.RefreshUI();
                UISystem?.Destroy(this);
                _isHiding = false;
                return;
            }

            UISystem?.RefreshUI();

            await HideAnimationAsync();
            gameObject.SetActive(false);

            SetState(StateEnum.Hidden);
            SetState(StateEnum.Closed);

            _isHiding = false;
            UISystem?.Destroy(this);
        }

        protected virtual async UniTask HideAnimationAsync()
        {
            EnsureAnimationComponent();
            if (_animation == null)
                return;

            isAnimationPlaying = true;
            DispatchAnimationStateChanged(true);

            try
            {
                await _animation.PlayHideAsync(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            isAnimationPlaying = false;
            DispatchAnimationStateChanged(false);
        }

        protected virtual async UniTask ShowAnimationAsync()
        {
            EnsureAnimationComponent();
            if (_animation == null)
                return;

            isAnimationPlaying = true;
            DispatchAnimationStateChanged(true);

            try
            {
                await _animation.PlayShowAsync(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            isAnimationPlaying = false;
            DispatchAnimationStateChanged(false);
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