namespace Azathrix.EzUI.Interfaces
{
    public interface IUIFocus
    {
        /// <summary>
        /// 当前是否处于焦点
        /// </summary>
        bool isFocused { set; get; }

        /// <summary>
        /// 焦点对应的输入方案（为空则不切换）
        /// </summary>
        string InputScheme { get; }
    }
}
