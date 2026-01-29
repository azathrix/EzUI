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
    public partial class UISystem : ISystem, ISystemInitialize, ISystemRegister
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
        private Image _maskImg;

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

        private readonly Queue<UIOperationHandle> _operationQueue = new Queue<UIOperationHandle>();
        private int _operationId;
        private bool _isProcessingQueue;
        private int _suppressRefreshCount;

        /// <summary>
        /// 输入方案栈（后入先出）
        /// </summary>
        private readonly List<(Panel owner, string scheme)> _inputSchemes = new();

        /// <summary>
        /// EventSystem 引用
        /// </summary>
        private EventSystem _eventSystem;

        /// <summary>
        /// Loading 处理器
        /// </summary>
        private IUILoadingHandler _loadingHandler;

        private readonly List<SubscriptionResult> _subscriptions = new List<SubscriptionResult>();

        private EzUISettings Settings => EzUISettings.Instance;

        private string DefaultGameInputScheme =>
            string.IsNullOrWhiteSpace(Settings?.defaultGameInputScheme) ? "Game" : Settings.defaultGameInputScheme;

        private string DefaultPanelPathFormat =>
            string.IsNullOrWhiteSpace(Settings?.panelPathFormat) ? "UI/{0}" : Settings.panelPathFormat;

        private string DefaultUIRootPath =>
            string.IsNullOrWhiteSpace(Settings?.uiRootPath) ? "UI/UIRoot" : Settings.uiRootPath;

    }
}
