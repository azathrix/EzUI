#if UNITY_EDITOR
using Azathrix.EzUI.Core;
using Azathrix.EzUI.Interfaces;

namespace Azathrix.EzUI.DebugTools
{
    public class EzUITestPanel : Panel
    {
    }

    public class EzUITestMainUIA : Panel, IMainUI
    {
    }

    public class EzUITestMainUIB : Panel, IMainUI
    {
    }

    public class EzUITestPopUI : PopUI
    {
    }
}
#endif
