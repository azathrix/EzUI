using Azathrix.EzUI.Interfaces;

namespace Azathrix.EzUI.Core
{
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

        protected virtual void OnFocusChanged()
        {
            // TODO: 焦点变化逻辑
        }
    }
}
