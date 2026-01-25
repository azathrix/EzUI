using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Azathrix.EzUI.Events;
using Azathrix.EzUI.Interfaces;
using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Events.Results;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.Framework.Tools;
using Azathrix.GameKit.Runtime.Builder.PrefabBuilders;
using Azathrix.GameKit.Runtime.Extensions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Azathrix.EzUI.Core
{
    [SystemAlias("UI系统")]
    public class UISystem : ISystem, ISystemInitialize, ISystemRegister
    {
        /// <summary>
        /// UI根节点
        /// </summary>
        private Transform _uiRoot;

        /// <summary>
        /// UI摄像机
        /// </summary>
        private Camera _uiCamera;

        /// <summary>
        /// UI根节点
        /// </summary>
        public Transform UIRoot => _uiRoot;

        /// <summary>
        /// UI摄像机
        /// </summary>
        public Camera UICamera => _uiCamera;

        /// <summary>
        /// 实例化的所有UI
        /// </summary>
        private readonly List<Panel> _instanceUIs = new List<Panel>();

        /// <summary>
        /// 所有层级
        /// </summary>
        private readonly Dictionary<int, Transform> _layers = new Dictionary<int, Transform>();

        /// <summary>
        /// 默认Mask
        /// </summary>
        private RectTransform _mask;

        private bool _maskActive;
        private Panel _maskTarget;

        /// <summary>
        /// 当前焦点UI,每次更新会选择优先级最高的UI
        /// </summary>
        private IUIFocus _currentUIFocus;

        /// <summary>
        /// 持久化UI
        /// </summary>
        private readonly HashSet<Panel> _persistenceUI = new HashSet<Panel>();

        /// <summary>
        /// 所有加载的Panel,如果在某个目录下,则需在Panel上面配置指定目录
        /// </summary>
        private readonly Dictionary<string, Panel> _loadedUI = new Dictionary<string, Panel>();

        /// <summary>
        /// 当前MainUI
        /// </summary>
        private Panel _currentMainUI;

        /// <summary>
        /// 输入方案栈（后入先出）
        /// </summary>
        private readonly List<(object owner, string scheme)> _inputSchemes = new List<(object, string)>();

        /// <summary>
        /// 获取当前MainUI
        /// </summary>
        public Panel CurrentMainUI => _currentMainUI;

        /// <summary>
        /// 当前输入方案
        /// </summary>
        public string CurrentInputScheme { get; private set; } = "Game";

        private readonly List<SubscriptionResult> _subscriptions = new List<SubscriptionResult>();
        
        private EzUISettings Settings => EzUISettings.Instance;

        private string DefaultGameInputScheme =>
            string.IsNullOrWhiteSpace(Settings?.defaultGameInputScheme) ? "Game" : Settings.defaultGameInputScheme;

        private string DefaultPanelPathFormat =>
            string.IsNullOrWhiteSpace(Settings?.panelPathFormat) ? "UI/{0}" : Settings.panelPathFormat;

        private string DefaultUIRootPath =>
            string.IsNullOrWhiteSpace(Settings?.uiRootPath) ? "UI/UIRoot" : Settings.uiRootPath;

        public void OnRegister()
        {
            RegisterEventHandlers();
        }

        public void OnUnRegister()
        {
            UnregisterEventHandlers();
        }

        public T FindUI<T>() where T : Panel
        {
            return FindUI(GetPath(typeof(T))) as T;
        }

        public Panel FindUI(string path)
        {
            foreach (var variable in _instanceUIs)
            {
                if (variable.path == path)
                    return variable;
            }

            return null;
        }

        private Transform GetLayer(int layer)
        {
            if (_layers.TryGetValue(layer, out var p))
                return p;

            return CreateLayer(layer);
        }

        private Transform CreateLayer(int layer)
        {
#if UNITY_EDITOR
            if (!AzathrixFramework.IsApplicationStarted) return null;
#endif
            if (_uiRoot == null) return null;

            GameObject go = new GameObject("Layer - " + layer, typeof(Canvas), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            go.layer = 5;
            RectTransform trans = (RectTransform) go.transform;
            trans.SetParent(_uiRoot);
            trans.localScale = Vector3.one;
            trans.localPosition = Vector3.zero;
            trans.anchorMax = Vector2.one;
            trans.anchorMin = Vector2.zero;
            trans.offsetMax = Vector2.zero;
            trans.offsetMin = Vector2.zero;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = layer;
            canvas.worldCamera = _uiCamera;
            _layers.Add(layer, trans);
            return trans;
        }

        private void SortUI()
        {
            _instanceUIs.Sort((x, y) =>
            {
                if (x.IsState(Panel.StateEnum.Shown | Panel.StateEnum.Show) &&
                    !y.IsState(Panel.StateEnum.Show | Panel.StateEnum.Shown))
                    return -1;

                if (y.IsState(Panel.StateEnum.Shown | Panel.StateEnum.Show) &&
                    !x.IsState(Panel.StateEnum.Show | Panel.StateEnum.Shown))
                    return 1;

                if (y.layer != x.layer)
                    return y.layer.CompareTo(x.layer);

                return y.transform.GetSiblingIndex().CompareTo(x.transform.GetSiblingIndex());
            });
        }

        private void OnMaskClick()
        {
            foreach (var ui in _instanceUIs)
            {
                if (ui.useMask && ui.IsState(Panel.StateEnum.Shown))
                {
                    if (ui is IMaskClickable mask)
                        mask.OnMaskClick();
                    break;
                }
            }
        }

        private void InitMask()
        {
            if (_uiRoot == null) return;

            GameObject go = new GameObject("Mask", typeof(Image), typeof(Button));
            go.layer = 5;
            RectTransform rt = (RectTransform) go.transform;
            ConfigureMaskRect(rt, _uiRoot);

            var img = go.GetComponent<Image>();
            var btn = go.GetComponent<Button>();

            var maskColor = Settings?.maskColor ?? new Color(0f, 0f, 0f, 0.95f);
            img.color = maskColor;

            var clickable = Settings?.maskClickable ?? true;
            if (clickable)
            {
                btn.onClick.AddListener(OnMaskClick);
            }
            else
            {
                btn.enabled = false;
            }

            btn.navigation = new Navigation() {mode = Navigation.Mode.None};
            btn.transition = Selectable.Transition.None;
            _mask = rt;
            go.SetActive(false);
        }

        private static void ConfigureMaskRect(RectTransform rt, Transform parent)
        {
            rt.SetParent(parent, false);
            rt.anchorMax = Vector2.one;
            rt.anchorMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.anchoredPosition3D = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        public void RefreshUI()
        {
#if UNITY_EDITOR
            if (!AzathrixFramework.IsApplicationStarted) return;
#endif
            if (_uiRoot == null || _mask == null) return;

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
                if (!ui.IsState(Panel.StateEnum.Shown | Panel.StateEnum.Show))
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

            if (!focusFlag && _currentUIFocus != null)
            {
                _currentUIFocus.isFocused = false;
                _currentUIFocus = null;
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

        private Panel Instantiate(string path)
        {
            var ui = LoadUI(path);
            if (ui == null)
                return null;

            Transform layer = GetLayer(ui.layer);

            var go = PrefabBuilder.Get().SetDefaultActive(false).SetPrefab(ui.gameObject).SetParent(layer).Build();

            if (go == null)
                return null;
            ui = go.GetComponent<Panel>();

            go.name = path;

            _instanceUIs.Add(ui);

            // 注入依赖（Panel及其所有子View）
            InjectPanel(ui);

            ui.Initialize(path);

            Dispatch(new UIPanelCreated
            {
                panel = ui,
                path = path
            });

            return ui;
        }

        /// <summary>
        /// 向Panel及其子组件注入依赖
        /// </summary>
        private void InjectPanel(Panel panel)
        {
            // 注入Panel本身
            panel.UISystem = this;
            AzathrixFramework.InjectTo(panel);

            // 注入所有子View
            var views = panel.GetComponentsInChildren<View>(true);
            foreach (var view in views)
            {
                AzathrixFramework.InjectTo(view);
            }
        }

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
        /// 设置持久化UI
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
        /// 取消持久化UI
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
        public void SetInputScheme(object owner, string scheme)
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

            var mode = Settings?.inputSchemeSwitchMode ?? EzUISettings.InputSchemeSwitchMode.EventOnly;
            if (mode != EzUISettings.InputSchemeSwitchMode.None)
            {
                Dispatch(new UIInputSchemeChanged
                {
                    previous = prev,
                    current = next,
                    count = _inputSchemes.Count,
                    source = owner as Panel
                });
            }
        }

        public void DestroyAll(bool force = false)
        {
            if (force)
                _persistenceUI.Clear();

            var list = new List<Panel>();
            foreach (var ui in _instanceUIs)
            {
                if (!ui)
                {
                    list.Add(ui);
                    continue;
                }
                if (_persistenceUI.Contains(ui))
                    continue;
                list.Add(ui);
            }

            foreach (var ui in list)
            {
                _instanceUIs.Remove(ui);
                if (!ui)
                    continue;
                if (ui.IsState(Panel.StateEnum.Show | Panel.StateEnum.Shown))
                {
                    try
                    {
                        ui.Hide(false);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                Dispatch(new UIPanelDestroyed
                {
                    panel = ui,
                    path = ui.path
                });

                Object.Destroy(ui.gameObject);
                SetInputScheme(ui, null);
            }

            RefreshUI();
        }

        /// <summary>
        /// 加载持久化UI,用此方法加载的UI不会被DestroyAll销毁
        /// </summary>
        public T LoadPersistenceUI<T>() where T : Panel
        {
            return (T) LoadPersistenceUI(GetPath(typeof(T)));
        }

        public Panel Show(string path, bool useAnimation = true, object userData = null)
        {
            var ui = FindUI(path);

            if (ui == null)
                ui = Instantiate(path);

            if (ui == null)
                return null;

            ui.transform.SetAsLastSibling();
            ui.userData = userData;
            ui.Show(useAnimation);

            return ui;
        }

        public PopUI GetPopUI()
        {
            foreach (var ui in _instanceUIs)
            {
                if (ui.IsState(Panel.StateEnum.Show | Panel.StateEnum.Shown) && ui is PopUI popUI)
                    return popUI;
            }

            return null;
        }

        public bool HasPopUI()
        {
            foreach (var ui in _instanceUIs)
            {
                if (ui.IsState(Panel.StateEnum.Show | Panel.StateEnum.Shown) && ui is PopUI)
                    return true;
            }

            return false;
        }

        public T Create<T>() where T : Panel
        {
            var path = GetPath(typeof(T));
            var ui = FindUI(path);

            if (ui == null)
                ui = Instantiate(path);

            return ui as T;
        }

        public T Show<T>(bool useAnimation = true) where T : Panel
        {
            return Show(GetPath(typeof(T)), useAnimation) as T;
        }

        public string GetPath(Type t)
        {
            var bind = t.GetCustomAttribute<BindUIPathAttribute>();
            if (bind != null && !string.IsNullOrWhiteSpace(bind.path))
                return bind.path;

            return string.Format(DefaultPanelPathFormat, t.Name);
        }

        private bool IsMainUIPath(string path)
        {
            var prefab = LoadUI(path);
            if (prefab == null)
            {
                Log.Warning($"[EzUI] 未找到UI预设: {path}");
                return false;
            }

            if (prefab is not IMainUI)
            {
                Log.Warning($"[EzUI] {prefab.GetType().Name} 未实现 IMainUI，无法作为主UI");
                return false;
            }

            return true;
        }

        private string ResolvePath(string path, Type panelType)
        {
            if (!string.IsNullOrWhiteSpace(path))
                return path;
            if (panelType != null)
                return GetPath(panelType);
            return null;
        }

        public Panel Show(Panel ui)
        {
            if (ui == null)
                return null;

            return Show(GetPath(ui.GetType()));
        }

        public Panel Show(Type ui)
        {
            if (ui == null)
                return null;

            return Show(GetPath(ui));
        }

        public void Destroy(Panel ui, bool force = false)
        {
            if (ui == null)
                return;

            if (_persistenceUI.Contains(ui) && !force)
                return;

            _instanceUIs.Remove(ui);
            _persistenceUI.Remove(ui);
            SetInputScheme(ui, null);

            Dispatch(new UIPanelDestroyed
            {
                panel = ui,
                path = ui.path
            });

            Object.Destroy(ui.gameObject);
            RefreshUI();
        }

        public void Destroy(string path)
        {
            Panel panel = FindUI(path);
            Destroy(panel);
        }

        public void Destroy<T>() where T : Panel
        {
            Destroy(GetPath(typeof(T)));
        }

        public void Hide<T>(bool useAnimation = true) where T : Panel
        {
            Hide(GetPath(typeof(T)), useAnimation);
        }

        public void Hide(Panel ui, bool useAnimation = true)
        {
            if (ui && ui.IsState(Panel.StateEnum.Shown))
                ui.Hide(useAnimation);
        }

        public void Hide(string path, bool useAnimation = true)
        {
            var ui = FindUI(path);
            if (ui && ui.IsState(Panel.StateEnum.Shown))
                ui.Hide(useAnimation);
        }

        private Panel LoadUI(string path)
        {
            if (_loadedUI.TryGetValue(path, out var p))
                return p;

            var prefab = path.LoadAsset<GameObject>();
            if (prefab == null)
            {
                Log.Warning("加载UI失败: " + path);
                return null;
            }

            if (!prefab.TryGetComponent<Panel>(out p))
                return null;

            _loadedUI.Add(path, p);
            return p;
        }

        public UniTask OnInitializeAsync()
        {
            CurrentInputScheme = DefaultGameInputScheme;
            InitFromPrefab();
            InitMask();
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 从预设初始化 UI 系统
        /// </summary>
        private void InitFromPrefab()
        {
            var prefab = AzathrixFramework.ResourcesLoader.Load<GameObject>(DefaultUIRootPath);
            if (prefab == null)
            {
                if (Settings?.autoCreateUIRoot ?? false)
                {
                    CreateDefaultUIRoot();
                    return;
                }

                Log.Error("UIRoot 预设未找到: " + DefaultUIRootPath);
                return;
            }

            var rootGo = Object.Instantiate(prefab);
            rootGo.name = Settings?.autoCreatedUIRootName ?? "[UIRoot]";
            Object.DontDestroyOnLoad(rootGo);
            _uiRoot = rootGo.transform;
            _uiCamera = rootGo.GetComponentInChildren<Camera>();

            // URP: 将 UI Camera 添加到主摄像机堆叠
            SetupURPCameraStack();

            Dispatch(new UIRootCreated
            {
                root = _uiRoot,
                uiCamera = _uiCamera
            });
        }

        private void CreateDefaultUIRoot()
        {
            var rootName = Settings?.autoCreatedUIRootName ?? "[UIRoot]";
            var rootGo = new GameObject(rootName);
            Object.DontDestroyOnLoad(rootGo);
            _uiRoot = rootGo.transform;

            if (Settings?.autoCreateUICamera ?? true)
            {
                var camGo = new GameObject("UICamera");
                camGo.transform.SetParent(_uiRoot);
                camGo.transform.localPosition = Vector3.zero;
                camGo.transform.localRotation = Quaternion.identity;
                camGo.transform.localScale = Vector3.one;
                camGo.layer = 5;

                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Depth;
                cam.orthographic = true;
                cam.cullingMask = 1 << 5;
                _uiCamera = cam;
                SetupURPCameraStack();
            }

            if (Settings?.autoCreateEventSystem ?? true)
            {
                if (Object.FindObjectOfType<EventSystem>() == null)
                {
                    var esGo = new GameObject("EventSystem");
                    esGo.transform.SetParent(_uiRoot);
                    esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                    esGo.AddComponent<InputSystemUIInputModule>();
#else
                    Log.Warning("[EzUI] 新输入系统未启用，EventSystem 未添加输入模块");
#endif
                }
            }

            Dispatch(new UIRootCreated
            {
                root = _uiRoot,
                uiCamera = _uiCamera
            });
        }

        /// <summary>
        /// 设置 URP 摄像机堆叠
        /// </summary>
        private void SetupURPCameraStack()
        {
            if (_uiCamera == null) return;

            var mainCamera = Camera.main;
            if (mainCamera == null) return;

            var uiCameraData = _uiCamera.GetUniversalAdditionalCameraData();
            if (uiCameraData != null)
                uiCameraData.renderType = CameraRenderType.Overlay;

            var mainCameraData = mainCamera.GetUniversalAdditionalCameraData();
            if (mainCameraData != null && !mainCameraData.cameraStack.Contains(_uiCamera))
                mainCameraData.cameraStack.Add(_uiCamera);
        }

        public void Close<T>(bool useAnimation = true) where T : Panel
        {
            var ui = FindUI(GetPath(typeof(T)));
            if (ui)
                ui.Close(useAnimation);
        }

        public void Close(Type type, bool useAnimation = true)
        {
            var ui = FindUI(GetPath(type));
            if (ui)
                ui.Close(useAnimation);
        }

        public void Close(string path, bool useAnimation = true)
        {
            var ui = FindUI(path);
            if (ui)
                ui.Close(useAnimation);
        }

        public T ShowOrHide<T>() where T : Panel
        {
            var f = FindUI<T>();
            if (f == null)
                return Show<T>();

            if (f.IsState(Panel.StateEnum.Hidden))
            {
                Show(f);
                return f;
            }

            if (f.IsState(Panel.StateEnum.Shown))
            {
                Hide(f);
                return f;
            }

            return f;
        }

        public Panel ShowOrHide(string path, bool useAnimation = true)
        {
            var f = FindUI(path);
            if (f == null)
                return Show(path, useAnimation);

            if (f.IsState(Panel.StateEnum.Hidden))
            {
                Show(f);
                return f;
            }

            if (f.IsState(Panel.StateEnum.Shown))
            {
                Hide(f, useAnimation);
                return f;
            }

            return f;
        }

        public void AutoClosePopUI(PopUI pop, bool useAnimation)
        {
            if (pop.IsState(Panel.StateEnum.Show | Panel.StateEnum.Shown))
            {
                if (pop.autoCloseTopType == AutoCloseTopTypeEnum.Close)
                {
                    pop.Close(useAnimation);
                    return;
                }
                else if (pop.autoCloseTopType == AutoCloseTopTypeEnum.Hide)
                {
                    pop.Hide(useAnimation);
                    return;
                }
                else if (pop.autoCloseTopType == AutoCloseTopTypeEnum.None)
                {
                    return;
                }
            }
        }

        public void AutoCloseTopPopUI(bool useAnimation = true)
        {
            SortUI();
            for (int i = 0; i < _instanceUIs.Count; i++)
            {
                var ui = _instanceUIs[i];
                if (ui.IsState(Panel.StateEnum.Show | Panel.StateEnum.Shown) && ui is PopUI pop)
                {
                    if (pop.autoCloseTopType == AutoCloseTopTypeEnum.Close)
                    {
                        pop.Close(useAnimation);
                        break;
                    }
                    else if (pop.autoCloseTopType == AutoCloseTopTypeEnum.Hide)
                    {
                        pop.Hide(useAnimation);
                        break;
                    }
                    else if (pop.autoCloseTopType == AutoCloseTopTypeEnum.None)
                    {
                        break;
                    }
                }
            }
        }

        #region MainUI Management

        /// <summary>
        /// 显示MainUI（保留历史栈）
        /// </summary>
        public T ShowMainUI<T>(bool useAnimation = true, object userData = null) where T : Panel, IMainUI
        {
            return ShowMainUI(GetPath(typeof(T)), useAnimation, userData) as T;
        }

        public Panel ShowMainUI(string path, bool useAnimation = true, object userData = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var existing = FindUI(path);
            if (existing != null && existing is not IMainUI)
            {
                Log.Warning($"[EzUI] {existing.GetType().Name} 未实现 IMainUI，无法作为主UI");
                return null;
            }

            if (existing == null && !IsMainUIPath(path))
                return null;

            var newMain = Show(path, useAnimation, userData);
            if (newMain != null && newMain != _currentMainUI)
            {
                var oldMain = _currentMainUI;
                if (oldMain != null)
                {
                    oldMain.Hide(useAnimation);
                }

                NotifyMainUIChange(oldMain, newMain);
                _currentMainUI = newMain;
            }

            if (newMain != null)
                newMain.userData = userData;

            return newMain;
        }

        /// <summary>
        /// 切换MainUI（关闭旧的MainUI）
        /// </summary>
        public T SwitchMainUI<T>(bool useAnimation = true, object userData = null) where T : Panel, IMainUI
        {
            return SwitchMainUI(GetPath(typeof(T)), useAnimation, userData) as T;
        }

        public Panel SwitchMainUI(string path, bool useAnimation = true, object userData = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var oldMain = _currentMainUI;
            var existing = FindUI(path);
            if (existing != null && existing is not IMainUI)
            {
                Log.Warning($"[EzUI] {existing.GetType().Name} 未实现 IMainUI，无法作为主UI");
                return null;
            }

            if (existing == null && !IsMainUIPath(path))
                return null;

            var newMain = Show(path, useAnimation, userData);

            if (newMain != null && newMain != oldMain)
            {
                NotifyMainUIChange(oldMain, newMain);
                _currentMainUI = newMain;

                if (oldMain != null)
                    oldMain.Close(useAnimation);
            }

            if (newMain != null)
                newMain.userData = userData;

            return newMain;
        }


        /// <summary>
        /// 通知MainUI切换，处理附加UI
        /// </summary>
        private void NotifyMainUIChange(Panel oldMain, Panel newMain)
        {
            if (oldMain == newMain)
                return;

            var uisToProcess = new List<Panel>(_instanceUIs);
            foreach (var ui in uisToProcess)
            {
                // 跳过MainUI本身
                if (ui is IMainUI)
                    continue;

                // 跳过持久化UI
                if (_persistenceUI.Contains(ui))
                    continue;

                // 根据UI的mainUIChangeBehavior处理
                switch (ui.mainUIChangeBehavior)
                {
                    case MainUIChangeBehavior.None:
                        break;
                    case MainUIChangeBehavior.Hide:
                        if (ui.IsState(Panel.StateEnum.Show | Panel.StateEnum.Shown))
                            ui.Hide(true);
                        break;
                    case MainUIChangeBehavior.Close:
                        if (!ui.IsState(Panel.StateEnum.Close))
                            ui.Close(true);
                        break;
                }
            }

            Dispatch(new UIMainUIChanged
            {
                previous = oldMain,
                current = newMain
            });
        }

        #endregion

        internal void NotifyPanelStateChanged(Panel panel, Panel.StateEnum previous, Panel.StateEnum current)
        {
            Dispatch(new UIPanelStateChanged
            {
                panel = panel,
                previous = previous,
                current = current
            });

            switch (current)
            {
                case Panel.StateEnum.Show:
                    Dispatch(new UIPanelShow {panel = panel, path = panel.path});
                    break;
                case Panel.StateEnum.Shown:
                    Dispatch(new UIPanelShown {panel = panel, path = panel.path});
                    break;
                case Panel.StateEnum.Hide:
                    Dispatch(new UIPanelHide {panel = panel, path = panel.path});
                    break;
                case Panel.StateEnum.Hidden:
                    Dispatch(new UIPanelHidden {panel = panel, path = panel.path});
                    break;
                case Panel.StateEnum.Close:
                    Dispatch(new UIPanelClose {panel = panel, path = panel.path});
                    if (panel == _currentMainUI)
                    {
                        var oldMain = _currentMainUI;
                        _currentMainUI = null;
                        Dispatch(new UIMainUIChanged
                        {
                            previous = oldMain,
                            current = null
                        });
                    }
                    break;
            }
        }

        private void RegisterEventHandlers()
        {
            var dispatcher = AzathrixFramework.Dispatcher;

            _subscriptions.Add(dispatcher.Subscribe<UIShowRequest>((ref UIShowRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                Show(path, evt.useAnimation, evt.userData);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIHideRequest>((ref UIHideRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                Hide(path, evt.useAnimation);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UICloseRequest>((ref UICloseRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                Close(path, evt.useAnimation);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIDestroyRequest>((ref UIDestroyRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                var panel = FindUI(path);
                Destroy(panel, evt.force);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIDestroyAllRequest>((ref UIDestroyAllRequest evt) =>
            {
                DestroyAll(evt.force);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIShowOrHideRequest>((ref UIShowOrHideRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                ShowOrHide(path, evt.useAnimation);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIShowMainRequest>((ref UIShowMainRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                ShowMainUI(path, evt.useAnimation, evt.userData);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UISwitchMainRequest>((ref UISwitchMainRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                SwitchMainUI(path, evt.useAnimation, evt.userData);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UILoadPersistenceRequest>((ref UILoadPersistenceRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                LoadPersistenceUI(path);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UISetPersistenceRequest>((ref UISetPersistenceRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                var panel = FindUI(path);
                if (panel == null) return;
                if (evt.persistent)
                    SetPersistenceUI(panel);
                else
                    CancelPersistenceUI(panel);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIRefreshRequest>((ref UIRefreshRequest evt) =>
            {
                RefreshUI();
            }));
        }

        private void UnregisterEventHandlers()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Unsubscribe();
            }
            _subscriptions.Clear();
        }

        private static void Dispatch<T>(T evt) where T : struct
        {
            AzathrixFramework.Dispatcher.Dispatch(evt);
        }
    }
}
