using Azathrix.EzUI.Events;
using Azathrix.Framework.Core;
using Azathrix.Framework.Tools;
using Azathrix.GameKit.Runtime.Builder.PrefabBuilders;
using Azathrix.GameKit.Runtime.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Azathrix.EzUI.Core
{
    // 实例化/注入/加载相关逻辑
    public partial class UISystem
    {
        private Panel Instantiate(string path)
        {
            var ui = LoadUI(path);
            if (ui == null)
                return null;

            Transform layer = GetLayer(ui.layer);

            var go = PrefabBuilder.Get().SetDefaultActive(false).SetPrefab(ui.gameObject).SetParent(layer).Build();

            if (go == null)
                return null;
            ui = go.GetComponent<Panel>();

            go.name = path;

            _instanceUIs.Add(ui);

            // 注入依赖（Panel及其所有子View）
            InjectPanel(ui);

            ui.Initialize(path);

            Dispatch(new UIPanelCreated
            {
                panel = ui,
                path = path
            });

            return ui;
        }

        /// <summary>
        /// 向Panel及其子组件注入依赖
        /// </summary>
        private void InjectPanel(Panel panel)
        {
            // 注入Panel本身
            panel.UISystem = this;
            AzathrixFramework.InjectTo(panel);

            // 注入所有子View
            var views = panel.GetComponentsInChildren<View>(true);
            foreach (var view in views)
            {
                AzathrixFramework.InjectTo(view);
            }
        }

        private Panel LoadUI(string path)
        {
            if (_loadedUI.TryGetValue(path, out var p))
            {
                if (p)
                    return p;
                _loadedUI.Remove(path);
            }

            var prefab = path.LoadAsset<GameObject>();
            if (prefab == null)
            {
                Log.Warning("加载UI失败: " + path);
                return null;
            }

            if (!prefab.TryGetComponent<Panel>(out p))
                return null;

            _loadedUI.Add(path, p);
            return p;
        }
    }
}
