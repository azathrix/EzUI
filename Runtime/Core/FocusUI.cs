using Azathrix.EzUI.Interfaces;

namespace Azathrix.EzUI.Core
{
    /// <summary>
    /// 焦点 UI 基类
    /// </summary>
    public class FocusUI : Panel, IUIFocus
    {
        private bool _focus;

        public bool isFocused
        {
            get => _focus;
            set
            {
                if (_focus != value)
                {
                    _focus = value;
                    OnFocusChanged();
                }
            }
        }

        public virtual string InputScheme => null;

        protected virtual void OnFocusChanged()
        {
            // TODO: 焦点变化逻辑
        }
    }
}
