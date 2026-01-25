using Azathrix.Framework.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Azathrix.EzUI
{
    /// <summary>
    /// EzUI 配置
    /// </summary>
    [SettingsPath("EzUISettings")]
    [ShowSetting("EzUI")]
    public class EzUISettings : SettingsBase<EzUISettings>
    {
        public enum InputSchemeSwitchMode
        {
            None,
            EventOnly
        }

        [Header("路径")]
        [Tooltip("UIRoot 预设路径")]
        public string uiRootPath = "UI/UIRoot";

        [Tooltip("Panel 默认路径格式，{0} = 类型名")]
        public string panelPathFormat = "UI/{0}";

        [Header("输入方案")]
        [Tooltip("游戏输入方案名称（空则使用默认 \"Game\"）")]
        public string defaultGameInputScheme = "Game";

        [Tooltip("PopUI 默认输入方案名称（空则不切换）")]
        public string defaultPopUIInputScheme = "UI";

        [Tooltip("FullScreenPopUI 默认输入方案名称（空则不切换）")]
        public string defaultFullScreenPopUIInputScheme = "UI";

        [Tooltip("FloatingPopUI 默认输入方案名称（空则不切换）")]
        public string defaultFloatingPopUIInputScheme = "";

        [Tooltip("输入方案切换模式")]
        public InputSchemeSwitchMode inputSchemeSwitchMode = InputSchemeSwitchMode.EventOnly;

        [Header("UIRoot")]
        [Tooltip("当 UIRoot 预设不存在时自动生成")]
        public bool autoCreateUIRoot = true;

        [Tooltip("自动生成的 UIRoot 名称")]
        public string autoCreatedUIRootName = "[UIRoot]";

        [Tooltip("自动生成时创建 EventSystem")]
        public bool autoCreateEventSystem = true;

        [Tooltip("自动生成时创建 UI Camera")]
        public bool autoCreateUICamera = true;

        [Header("遮罩")]
        public Color maskColor = new Color(0f, 0f, 0f, 0.95f);

        [Tooltip("是否启用遮罩点击")]
        public bool maskClickable = true;

        [Header("动画")]
        [Tooltip("动画播放时屏蔽输入（全局默认值，可被 Panel 重载）")]
        public bool blockInputDuringAnimation = true;
    }
}
