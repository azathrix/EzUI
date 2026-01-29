using Azathrix.GameKit.Runtime.Behaviours;

namespace Azathrix.EzUI.Core
{
    public class View : GameScript
    {
        public Panel rootUI { set; get; }

        protected override void OnScriptInitialize()
        {
            base.OnScriptInitialize();
            rootUI = GetComponentInParent<Panel>();
            if (rootUI != null)
                rootUI.RegisterView(this);
        }

        public virtual void OnCreate()
        {
        }

        public virtual void OnShow()
        {
        }

        public virtual void OnShown()
        {
        }

        public virtual void OnHide()
        {
        }

        public virtual void OnHidden()
        {
        }

        public virtual void OnClose()
        {
        }

        public virtual void OnClosed()
        {
        }
    }
}
