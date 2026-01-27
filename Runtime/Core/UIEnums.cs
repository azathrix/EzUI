namespace Azathrix.EzUI.Core
{
    /// <summary>
    /// MainUI切换时的行为
    /// </summary>
    public enum MainUIChangeBehavior
    {
        /// <summary>
        /// 不处理（OverlayUI默认）
        /// </summary>
        None,

        /// <summary>
        /// 隐藏但保留实例（AdditiveUI默认）
        /// </summary>
        Hide,

        /// <summary>
        /// 关闭并销毁（PopUI默认）
        /// </summary>
        Close,
    }

    /// <summary>
    /// 自动关闭原因
    /// </summary>
    public enum AutoCloseReason
    {
        /// <summary>
        /// 点击遮罩
        /// </summary>
        MaskClick,

        /// <summary>
        /// Pop 自动关闭（ESC/返回键）
        /// </summary>
        PopAutoClose,

        /// <summary>
        /// 切换 MainUI
        /// </summary>
        MainUISwitch,

        /// <summary>
        /// 销毁所有 UI
        /// </summary>
        DestroyAll,
    }
}
