using Azathrix.Framework.Settings;
using UnityEngine;

namespace Azathrix.EzUI
{
    /// <summary>
    /// EzUI 配置
    /// </summary>
    [SettingsPath("EzUISettings")]
    [ShowSetting("EzUI")]
    public class EzUISettings : SettingsBase<EzUISettings>
    {
        /// <summary>
        /// 初始化模式
        /// </summary>
        public enum InitializeMode
        {
            /// <summary>
            /// 自动初始化（从预设或自动创建）
            /// </summary>
            Auto,

            /// <summary>
            /// 手动初始化（需要调用 SetUIRoot 等方法）
            /// </summary>
            Manual
        }


        // ========== 初始化设置 ==========
        [HideInInspector]
        public InitializeMode initializeMode = InitializeMode.Auto;

        // ========== 自动初始化 - 预设模式 ==========
        [HideInInspector]
        public string uiRootPath = "UI/UIRoot";

        // ========== 自动初始化 - 自动创建模式 ==========
        [HideInInspector]
        public bool autoCreateUIRoot = true;

        [HideInInspector]
        public string autoCreatedUIRootName = "[UIRoot]";

        [HideInInspector]
        public bool autoCreateEventSystem = true;

        [HideInInspector]
        public bool autoCreateUICamera = true;

        [HideInInspector]
        public string uiCameraTag = "UICamera";

        // ========== 路径设置 ==========
        [HideInInspector]
        public string panelPathFormat = "UI/{0}";

        // ========== 输入方案设置 ==========
        [HideInInspector]
        public string defaultGameInputScheme = "Game";

        [HideInInspector]
        public string defaultPopUIInputScheme = "UI";

        // ========== 遮罩设置 ==========
        [HideInInspector]
        public Color maskColor = new Color(0f, 0f, 0f, 0.95f);

        [HideInInspector]
        public bool maskClickable = true;

        // ========== 动画设置 ==========
        [HideInInspector]
        public bool blockInputDuringAnimation = true;
    }
}
