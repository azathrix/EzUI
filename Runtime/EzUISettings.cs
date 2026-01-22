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
        [Header("路径")]
        [Tooltip("UIRoot 预设路径（Resources 相对路径）")]
        public string uiRootPath = "UI/UIRoot";

        [Tooltip("Panel 默认路径格式（Resources 相对路径），{0} = 类型名")]
        public string panelPathFormat = "UI/{0}";

        [Tooltip("Resources 下的 UI 根目录（用于预加载等）")]
        public string resourcesFolder = "UI";

        [Header("遮罩")]
        public Color maskColor = new Color(0f, 0f, 0f, 0.95f);

        [Tooltip("是否启用遮罩点击")]
        public bool maskClickable = true;
    }
}
