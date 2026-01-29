using System;
using Azathrix.EzUI.Events;
using Azathrix.Framework.Core;

namespace Azathrix.EzUI.Core
{
    // 事件分发与订阅相关逻辑
    public partial class UISystem
    {
        private string ResolvePath(string path, Type panelType)
        {
            if (!string.IsNullOrWhiteSpace(path))
                return path;
            if (panelType != null)
                return GetPath(panelType);
            return null;
        }

        internal void NotifyPanelStateChanged(Panel panel, Panel.StateEnum previous, Panel.StateEnum current)
        {
            Dispatch(new UIPanelStateChanged
            {
                panel = panel,
                previous = previous,
                current = current
            });

            switch (current)
            {
                case Panel.StateEnum.Shown:
                    Dispatch(new UIPanelShown {panel = panel, path = panel.path});
                    break;
                case Panel.StateEnum.Hidden:
                    Dispatch(new UIPanelHidden {panel = panel, path = panel.path});
                    break;
                case Panel.StateEnum.Closed:
                    Dispatch(new UIPanelClose {panel = panel, path = panel.path});
                    if (panel == _currentMainUI)
                    {
                        _currentMainUI = null;
                    }

                    break;
            }
        }

        private void RegisterEventHandlers()
        {
            var dispatcher = AzathrixFramework.Dispatcher;

            _subscriptions.Add(dispatcher.Subscribe<UIShowRequest>((ref UIShowRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                Show(path, evt.useAnimation, evt.userData);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIHideRequest>((ref UIHideRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                Hide(path, evt.useAnimation);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UICloseRequest>((ref UICloseRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                Close(path, evt.useAnimation);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIDestroyRequest>((ref UIDestroyRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                var panel = FindUI(path);
                Destroy(panel, evt.force);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIDestroyAllRequest>((ref UIDestroyAllRequest evt) =>
            {
                DestroyAll(evt.force);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIShowOrHideRequest>((ref UIShowOrHideRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                ShowOrHide(path, evt.useAnimation);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIShowMainRequest>((ref UIShowMainRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                Show(path, evt.useAnimation, evt.userData);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UISwitchMainRequest>((ref UISwitchMainRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                Show(path, evt.useAnimation, evt.userData);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UILoadPersistenceRequest>((ref UILoadPersistenceRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                LoadPersistenceUI(path);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UISetPersistenceRequest>((ref UISetPersistenceRequest evt) =>
            {
                var path = ResolvePath(evt.path, evt.panelType);
                if (string.IsNullOrWhiteSpace(path)) return;
                var panel = FindUI(path);
                if (panel == null) return;
                if (evt.persistent)
                    SetPersistenceUI(panel);
                else
                    CancelPersistenceUI(panel);
            }));

            _subscriptions.Add(dispatcher.Subscribe<UIRefreshRequest>((ref UIRefreshRequest evt) => { RefreshUI(); }));
        }

        private void UnregisterEventHandlers()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Unsubscribe();
            }

            _subscriptions.Clear();
        }

        private static void Dispatch<T>(T evt) where T : struct
        {
            AzathrixFramework.Dispatcher.Dispatch(evt);
        }
    }
}
