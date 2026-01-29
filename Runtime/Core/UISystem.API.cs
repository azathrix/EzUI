using System;
using System.Collections.Generic;
using System.Reflection;
using Azathrix.EzUI.Events;
using Azathrix.EzUI.Interfaces;
using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Events.Results;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.Framework.Tools;
using Azathrix.GameKit.Runtime.Builder.PrefabBuilders;
using Azathrix.GameKit.Runtime.Extensions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Azathrix.EzUI.Core
{
    /// <summary>
    /// UISystem 对外 API。
    /// 说明：Show/Hide/Close/ShowOrHide/AutoClose/Destroy/DestroyAll 等操作均为入队执行，返回句柄用于等待或查询结果。
    /// </summary>
    public partial class UISystem
    {
        #region Public Properties
        /// <summary>
        /// UI根节点
        /// </summary>
        public Transform UIRoot => _uiRoot;

        /// <summary>
        /// UI摄像机
        /// </summary>
        public Camera UICamera => _uiCamera;

        /// <summary>
        /// 获取当前MainUI
        /// </summary>
        public Panel CurrentMainUI => _currentMainUI;

        /// <summary>
        /// 当前输入方案
        /// </summary>
        public string CurrentInputScheme { get; private set; }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// EventSystem
        /// </summary>
        public EventSystem EventSystem => _eventSystem;

        /// <summary>
        /// 设置 Loading 处理器（可选）
        /// </summary>
        public void SetLoadingHandler(IUILoadingHandler handler)
        {
            _loadingHandler = handler;
        }

        /// <summary>
        /// 获取 Loading 处理器
        /// </summary>
        public IUILoadingHandler LoadingHandler => _loadingHandler;

        /// <summary>
        /// 系统注册（由框架调用）
        /// </summary>
        public void OnRegister()
        {
            RegisterEventHandlers();
        }

        /// <summary>
        /// 系统注销（由框架调用）
        /// </summary>
        public void OnUnRegister()
        {
            UnregisterEventHandlers();
        }

        #endregion

        #region Manual Initialization

        /// <summary>
        /// 手动设置 UI 根节点
        /// </summary>
        public void SetUIRoot(Transform root)
        {
            if (root == null)
            {
                Log.Warning("[EzUI] SetUIRoot: root 不能为空");
                return;
            }

            _uiRoot = root;
            Object.DontDestroyOnLoad(root.gameObject);

            if (_mask == null)
                InitMask();

            CheckInitialized();
        }

        /// <summary>
        /// 手动设置 UI 摄像机
        /// </summary>
        public void SetUICamera(Camera camera)
        {
            _uiCamera = camera;

            if (_uiCamera != null)
                SetupURPCameraStack();

            UpdateLayerCameras();
            CheckInitialized();
        }

        /// <summary>
        /// 手动设置 EventSystem
        /// </summary>
        public void SetEventSystem(EventSystem eventSystem)
        {
            _eventSystem = eventSystem;
            CheckInitialized();
        }

        #endregion

        #region Query & Refresh

        /// <summary>
        /// 查找已实例化的 UI（不创建，不入队）
        /// </summary>
        public T FindUI<T>() where T : Panel
        {
            return FindUI(GetPath(typeof(T))) as T;
        }

        /// <summary>
        /// 查找已实例化的 UI（不创建，不入队）
        /// </summary>
        public Panel FindUI(string path)
        {
            foreach (var variable in _instanceUIs)
            {
                if (variable && variable.path == path)
                    return variable;
            }

            return null;
        }

        /// <summary>
        /// 刷新 UI 排序/遮罩/焦点（受 _suppressRefreshCount 控制）
        /// </summary>
        public void RefreshUI()
        {
#if UNITY_EDITOR
            if (!AzathrixFramework.IsApplicationStarted) return;
#endif
            if (_suppressRefreshCount > 0) return;
            if (_uiRoot == null || _mask == null) return;

            if (_currentMainUI != null && !_currentMainUI)
                _currentMainUI = null;

            if (_instanceUIs.Count > 0)
                _instanceUIs.RemoveAll(ui => !ui);

            SortUI();
            bool maskFlag = false;
            bool focusFlag = false;

            var prevFocus = _currentUIFocus;
            var prevMaskActive = _maskActive;
            var prevMaskTarget = _maskTarget;
            _maskTarget = null;

            ConfigureMaskRect(_mask, _uiRoot);
            foreach (var ui in _instanceUIs)
            {
                if (!ui.IsState(Panel.StateEnum.Shown) && !ui.IsVisible)
                    continue;

                if (!maskFlag && ui.useMask)
                {
                    Transform tr = ui.transform;
                    int index = tr.GetSiblingIndex();
                    ConfigureMaskRect(_mask, tr.parent);
                    _mask.SetSiblingIndex(index);
                    _mask.gameObject.SetActive(true);
                    maskFlag = true;
                    _maskTarget = ui;

                    // 应用遮罩颜色（优先使用 Panel 的局部设置）
                    if (_maskImg == null)
                        _maskImg = _mask.GetComponent<Image>();
                    if (_maskImg != null)
                    {
                        var color = ui.maskColor ?? Settings?.maskColor ?? new Color(0f, 0f, 0f, 0.95f);
                        _maskImg.color = color;
                    }
                }

                if (!focusFlag)
                {
                    if (ui is IUIFocus focus)
                    {
                        if (_currentUIFocus != focus)
                        {
                            if (_currentUIFocus != null)
                            {
                                _currentUIFocus.isFocused = false;
                            }

                            _currentUIFocus = focus;
                            _currentUIFocus.isFocused = true;
                        }

                        focusFlag = true;
                    }
                }

                if (maskFlag && focusFlag)
                {
                    break;
                }
            }

            if (!maskFlag)
            {
                _mask.gameObject.SetActive(false);
            }

            _maskActive = maskFlag;

            if (prevFocus != _currentUIFocus)
            {
                Dispatch(new UIFocusChanged
                {
                    previous = prevFocus,
                    current = _currentUIFocus
                });
            }

            if (prevMaskActive != _maskActive || prevMaskTarget != _maskTarget)
            {
                Dispatch(new UIMaskStateChanged
                {
                    active = _maskActive,
                    target = _maskTarget
                });
            }
        }

        #endregion

        #region Persistence & Input

        /// <summary>
        /// 加载并标记为持久化 UI（不会被 DestroyAll 关闭）
        /// </summary>
        public Panel LoadPersistenceUI(string path)
        {
            Panel panel = FindUI(path);

            if (panel == null)
            {
                panel = Instantiate(path);
                if (panel == null)
                    return null;
                panel.gameObject.SetActive(false);
                SetPersistenceUI(panel);
            }

            return panel;
        }

        /// <summary>
        /// 设置持久化 UI
        /// </summary>
        public void SetPersistenceUI(Panel panel)
        {
            if (panel == null)
                return;

            if (_persistenceUI.Contains(panel))
                return;

            _persistenceUI.Add(panel);
        }

        /// <summary>
        /// 取消持久化 UI
        /// </summary>
        public void CancelPersistenceUI(Panel panel)
        {
            if (panel == null)
                return;

            if (!_persistenceUI.Contains(panel))
                return;

            _persistenceUI.Remove(panel);
        }

        /// <summary>
        /// 设置输入方案（不依赖具体输入系统）
        /// </summary>
        public void SetInputScheme(Panel owner, string scheme)
        {
            if (owner == null) return;

            for (int i = _inputSchemes.Count - 1; i >= 0; i--)
            {
                if (_inputSchemes[i].owner == owner)
                    _inputSchemes.RemoveAt(i);
            }

            if (!string.IsNullOrWhiteSpace(scheme))
                _inputSchemes.Add((owner, scheme));

            var next = _inputSchemes.Count > 0 ? _inputSchemes[^1].scheme : DefaultGameInputScheme;
            if (next == CurrentInputScheme)
                return;

            var prev = CurrentInputScheme;
            CurrentInputScheme = next;

            Dispatch(new UIInputSchemeChanged
            {
                previous = prev,
                current = next,
                count = _inputSchemes.Count,
                source = owner 
            });
        }

        /// <summary>
        /// 入队销毁所有非持久化 UI（统一在末尾刷新）
        /// </summary>
        public UIOperationHandle DestroyAll(bool force = false)
        {
            var handle = new UIOperationHandle(++_operationId, UIOperationType.DestroyAll, null)
            {
                force = force
            };
            return EnqueueOperation(handle);
        }

        #endregion

        /// <summary>
        /// 加载持久化 UI（泛型便捷重载）
        /// </summary>
        public T LoadPersistenceUI<T>() where T : Panel
        {
            return (T) LoadPersistenceUI(GetPath(typeof(T)));
        }

        #region UI Operations (Queued)

        /// <summary>
        /// 入队显示指定路径的 UI，返回操作句柄
        /// </summary>
        public UIOperationHandle Show(string path, bool useAnimation = true, object userData = null)
        {
            var handle = new UIOperationHandle(++_operationId, UIOperationType.Show, path)
            {
                useAnimation = useAnimation,
                userData = userData
            };
            return EnqueueOperation(handle);
        }

        /// <summary>
        /// 获取当前显示的 PopUI（第一个命中）
        /// </summary>
        public PopUI GetPopUI()
        {
            foreach (var ui in _instanceUIs)
            {
                if (ui.IsState(Panel.StateEnum.Shown) && ui is PopUI popUI)
                    return popUI;
            }

            return null;
        }

        /// <summary>
        /// 是否存在显示中的 PopUI
        /// </summary>
        public bool HasPopUI()
        {
            foreach (var ui in _instanceUIs)
            {
                if (ui.IsState(Panel.StateEnum.Shown) && ui is PopUI)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 立即创建（不入队、不显示）
        /// </summary>
        public T Create<T>() where T : Panel
        {
            var path = GetPath(typeof(T));
            var ui = FindUI(path);

            if (ui == null)
                ui = Instantiate(path);

            return ui as T;
        }

        /// <summary>
        /// 入队显示 UI（泛型便捷重载）
        /// </summary>
        public UIOperationHandle Show<T>(bool useAnimation = true) where T : Panel
        {
            return Show(GetPath(typeof(T)), useAnimation, null);
        }

        /// <summary>
        /// 入队显示 UI（带 userData 的泛型便捷重载）
        /// </summary>
        public UIOperationHandle Show<T>(object userData, bool useAnimation = true) where T : Panel
        {
            return Show(GetPath(typeof(T)), useAnimation, userData);
        }

        /// <summary>
        /// 异步等待显示完成（泛型便捷重载）
        /// </summary>
        public async UniTask<T> ShowAsync<T>(bool useAnimation = true, object userData = null) where T : Panel
        {
            var panel = await ShowAsync(GetPath(typeof(T)), useAnimation, userData);
            return panel as T;
        }

        /// <summary>
        /// 获取 Panel 的资源路径
        /// </summary>
        public string GetPath(Type t)
        {
            var bind = t.GetCustomAttribute<BindUIPathAttribute>();
            if (bind != null && !string.IsNullOrWhiteSpace(bind.path))
                return bind.path;

            return string.Format(DefaultPanelPathFormat, t.Name);
        }

        /// <summary>
        /// 入队显示指定实例的 UI（使用当前 userData）
        /// </summary>
        public UIOperationHandle Show(Panel ui, bool useAnimation = true)
        {
            if (ui == null)
                return null;

            return Show(GetPath(ui.GetType()), useAnimation, ui.userData);
        }

        /// <summary>
        /// 入队显示指定实例的 UI（使用传入 userData）
        /// </summary>
        public UIOperationHandle Show(Panel ui, object userData, bool useAnimation = true)
        {
            if (ui == null)
                return null;

            return Show(GetPath(ui.GetType()), useAnimation, userData);
        }

        /// <summary>
        /// 异步等待显示完成
        /// </summary>
        public UniTask<Panel> ShowAsync(string path, bool useAnimation = true, object userData = null)
        {
            return Show(path, useAnimation, userData).Task;
        }

        /// <summary>
        /// 入队显示 UI（Type 便捷重载）
        /// </summary>
        public UIOperationHandle Show(Type ui, bool useAnimation = true)
        {
            if (ui == null)
                return null;

            return Show(GetPath(ui), useAnimation, null);
        }

        /// <summary>
        /// 入队显示 UI（Type + userData 便捷重载）
        /// </summary>
        public UIOperationHandle Show(Type ui, object userData, bool useAnimation = true)
        {
            if (ui == null)
                return null;

            return Show(GetPath(ui), useAnimation, userData);
        }

        /// <summary>
        /// 入队销毁指定 UI
        /// </summary>
        public UIOperationHandle Destroy(Panel ui, bool force = false)
        {
            if (ui == null)
                return null;

            var handle = new UIOperationHandle(++_operationId, UIOperationType.Destroy, ui.path)
            {
                target = ui,
                force = force
            };
            return EnqueueOperation(handle);
        }

        /// <summary>
        /// 入队销毁指定路径的 UI
        /// </summary>
        public UIOperationHandle Destroy(string path)
        {
            var handle = new UIOperationHandle(++_operationId, UIOperationType.Destroy, path)
            {
                force = false
            };
            return EnqueueOperation(handle);
        }

        /// <summary>
        /// 入队销毁 UI（泛型便捷重载）
        /// </summary>
        public UIOperationHandle Destroy<T>() where T : Panel
        {
            return Destroy(GetPath(typeof(T)));
        }

        /// <summary>
        /// 入队隐藏 UI（泛型便捷重载）
        /// </summary>
        public UIOperationHandle Hide<T>(bool useAnimation = true) where T : Panel
        {
            return Hide(GetPath(typeof(T)), useAnimation);
        }

        /// <summary>
        /// 异步等待隐藏完成（泛型便捷重载）
        /// </summary>
        public UniTask HideAsync<T>(bool useAnimation = true) where T : Panel
        {
            return Hide(GetPath(typeof(T)), useAnimation).Task;
        }

        /// <summary>
        /// 入队隐藏指定实例的 UI
        /// </summary>
        public UIOperationHandle Hide(Panel ui, bool useAnimation = true)
        {
            var handle = new UIOperationHandle(++_operationId, UIOperationType.Hide, ui != null ? ui.path : null)
            {
                useAnimation = useAnimation,
                target = ui
            };
            return EnqueueOperation(handle);
        }

        /// <summary>
        /// 异步等待隐藏完成
        /// </summary>
        public UniTask HideAsync(Panel ui, bool useAnimation = true)
        {
            if (!ui)
                return UniTask.CompletedTask;
            return Hide(ui, useAnimation).Task;
        }

        /// <summary>
        /// 入队隐藏指定路径的 UI
        /// </summary>
        public UIOperationHandle Hide(string path, bool useAnimation = true)
        {
            var handle = new UIOperationHandle(++_operationId, UIOperationType.Hide, path)
            {
                useAnimation = useAnimation
            };
            return EnqueueOperation(handle);
        }

        /// <summary>
        /// 异步等待隐藏完成
        /// </summary>
        public UniTask HideAsync(string path, bool useAnimation = true)
        {
            return Hide(path, useAnimation).Task;
        }

        /// <summary>
        /// 系统初始化（由框架调用）
        /// </summary>
        public UniTask OnInitializeAsync()
        {
            CurrentInputScheme = DefaultGameInputScheme;

            var mode = Settings?.initializeMode ?? EzUISettings.InitializeMode.Auto;
            if (mode == EzUISettings.InitializeMode.Manual)
            {
                // 手动模式下，等待用户调用 SetUIRoot 等方法
                return UniTask.CompletedTask;
            }

            // 自动模式
            InitFromPrefab();
            InitMask();
            IsInitialized = true;
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 入队关闭指定实例的 UI
        /// </summary>
        public UIOperationHandle Close(Panel ui, bool useAnimation = true)
        {
            var handle = new UIOperationHandle(++_operationId, UIOperationType.Close, ui != null ? ui.path : null)
            {
                useAnimation = useAnimation,
                target = ui
            };
            return EnqueueOperation(handle);
        }

        /// <summary>
        /// 异步等待关闭完成
        /// </summary>
        public UniTask CloseAsync(Panel ui, bool useAnimation = true)
        {
            if (!ui)
                return UniTask.CompletedTask;
            return Close(ui, useAnimation).Task;
        }

        /// <summary>
        /// 入队关闭 UI（泛型便捷重载）
        /// </summary>
        public UIOperationHandle Close<T>(bool useAnimation = true) where T : Panel
        {
            return Close(GetPath(typeof(T)), useAnimation);
        }

        /// <summary>
        /// 异步等待关闭完成（泛型便捷重载）
        /// </summary>
        public UniTask CloseAsync<T>(bool useAnimation = true) where T : Panel
        {
            return Close(GetPath(typeof(T)), useAnimation).Task;
        }

        /// <summary>
        /// 入队关闭 UI（Type 便捷重载）
        /// </summary>
        public UIOperationHandle Close(Type type, bool useAnimation = true)
        {
            if (type == null)
                return null;
            return Close(GetPath(type), useAnimation);
        }

        /// <summary>
        /// 入队关闭指定路径的 UI
        /// </summary>
        public UIOperationHandle Close(string path, bool useAnimation = true)
        {
            var handle = new UIOperationHandle(++_operationId, UIOperationType.Close, path)
            {
                useAnimation = useAnimation
            };
            return EnqueueOperation(handle);
        }

        /// <summary>
        /// 异步等待关闭完成
        /// </summary>
        public UniTask CloseAsync(string path, bool useAnimation = true)
        {
            return Close(path, useAnimation).Task;
        }

        /// <summary>
        /// 入队显示/隐藏（Toggle，泛型便捷重载）
        /// </summary>
        public UIOperationHandle ShowOrHide<T>(bool useAnimation = true) where T : Panel
        {
            return ShowOrHide(GetPath(typeof(T)), useAnimation, null);
        }

        /// <summary>
        /// 入队显示/隐藏（Toggle，泛型+userData 便捷重载）
        /// </summary>
        public UIOperationHandle ShowOrHide<T>(object userData, bool useAnimation = true) where T : Panel
        {
            return ShowOrHide(GetPath(typeof(T)), useAnimation, userData);
        }

        /// <summary>
        /// 入队显示/隐藏（Toggle）
        /// </summary>
        public UIOperationHandle ShowOrHide(string path, bool useAnimation = true)
        {
            return ShowOrHide(path, useAnimation, null);
        }

        /// <summary>
        /// 入队显示/隐藏（Toggle，支持 userData）
        /// </summary>
        public UIOperationHandle ShowOrHide(string path, bool useAnimation, object userData)
        {
            var handle = new UIOperationHandle(++_operationId, UIOperationType.Toggle, path)
            {
                useAnimation = useAnimation,
                userData = userData
            };
            return EnqueueOperation(handle);
        }

        /// <summary>
        /// 根据面板策略执行自动关闭（入队）
        /// </summary>
        public void AutoClose(Panel panel, AutoCloseReason reason, bool useAnimation = true)
        {
            if (panel == null) return;
            switch (panel.GetAutoCloseType(reason))
            {
                case AutoCloseBehavior.Hide:
                    Hide(panel, useAnimation);
                    break;
                case AutoCloseBehavior.Close:
                    Close(panel, useAnimation);
                    break;
            }
        }

        /// <summary>
        /// 自动关闭 Panel（泛型版本）
        /// </summary>
        public void AutoClose<T>(AutoCloseReason reason, bool useAnimation = true) where T : Panel
        {
            var panel = FindUI<T>();
            if (panel != null)
                AutoClose(panel, reason, useAnimation);
        }

        /// <summary>
        /// 自动关闭 Panel（路径版本）
        /// </summary>
        public void AutoClose(string path, AutoCloseReason reason, bool useAnimation = true)
        {
            var panel = FindUI(path);
            if (panel != null)
                AutoClose(panel, reason, useAnimation);
        }

        /// <summary>
        /// 自动关闭指定 PopUI（入队）
        /// </summary>
        public void AutoClosePopUI(PopUI pop, bool useAnimation)
        {
            if (pop == null) return;
            if (!pop.IsState(Panel.StateEnum.Shown)) return;

            AutoClose(pop, AutoCloseReason.PopAutoClose, useAnimation);
        }

        /// <summary>
        /// 自动关闭最上层 PopUI（入队）
        /// </summary>
        public void AutoCloseTopPopUI(bool useAnimation = true)
        {
            for (int i = 0; i < _instanceUIs.Count; i++)
            {
                var ui = _instanceUIs[i];
                if (ui.IsState(Panel.StateEnum.Shown) && ui is PopUI pop)
                {
                    if (pop.autoCloseTopType == AutoCloseTopTypeEnum.Ignore)
                        continue;

                    if (pop.autoCloseTopType == AutoCloseTopTypeEnum.None)
                        break;

                    AutoClose(pop, AutoCloseReason.PopAutoClose, useAnimation);
                    break;
                }
            }
        }

        #endregion
    }
}
