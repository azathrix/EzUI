using System;
using Azathrix.EzUI.Interfaces;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azathrix.EzUI.Core
{
    // MainUI 切换与自动关闭相关逻辑
    public partial class UISystem
    {
        private bool IsMainUIPath(string path)
        {
            var prefab = LoadUI(path);
            if (prefab == null)
            {
                Log.Warning($"[EzUI] 未找到UI预设: {path}");
                return false;
            }

            if (prefab is not IMainUI)
            {
                Log.Warning($"[EzUI] {prefab.GetType().Name} 未实现 IMainUI，无法作为主UI");
                return false;
            }

            return true;
        }

        private UniTask AutoCloseInternal(Panel panel, AutoCloseReason reason, bool useAnimation)
        {
            if (panel == null)
                return UniTask.CompletedTask;
            switch (panel.GetAutoCloseType(reason))
            {
                case AutoCloseBehavior.Hide:
                    return SystemHide(panel, useAnimation);
                case AutoCloseBehavior.Close:
                    return SystemClose(panel, useAnimation);
            }

            return UniTask.CompletedTask;
        }

        private async UniTask AutoCloseAllNonMainUI(bool useAnimation)
        {
            var list = new System.Collections.Generic.List<Panel>(_instanceUIs);
            for (int i = 0; i < list.Count; i++)
            {
                var ui = list[i];
                if (ui == null || ui is IMainUI)
                    continue;

                if (_persistenceUI.Contains(ui))
                {
                    if (ui.IsState(Panel.StateEnum.Shown))
                        await SystemHide(ui, false);
                    continue;
                }

                await AutoCloseInternal(ui, AutoCloseReason.MainUISwitch, useAnimation);
            }
        }

        private async UniTask ShowMainUIFlowAsync(
            string path,
            Panel ui,
            Panel oldMain,
            bool useAnimation,
            object userData,
            IMainUILoadable loadable,
            bool needLoading)
        {
            // 主UI切换统一不播放动画
            const bool switchUseAnimation = false;

            ILoadingController controller = null;
            if (needLoading)
            {
                try
                {
                    controller = await _loadingHandler.ShowLoading(loadable.LoadingConfig);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _suppressRefreshCount++;
            try
            {
                // 清空当前所有非 MainUI（持久化 UI 强制 Hide）
                await AutoCloseAllNonMainUI(switchUseAnimation);

                if (oldMain != null)
                {
                    await AutoCloseInternal(oldMain, AutoCloseReason.MainUISwitch, switchUseAnimation);

                    if (oldMain.mainUIChangeBehavior != MainUIChangeBehavior.None && switchUseAnimation)
                    {
                        await UniTask.WaitUntil(() =>
                            !oldMain || oldMain.IsState(Panel.StateEnum.Hidden) || oldMain.IsState(Panel.StateEnum.Closed));
                    }
                }

                if (!ui || ui.IsState(Panel.StateEnum.Closed))
                    ui = Instantiate(path);

                if (ui == null)
                {
                    _currentMainUI = null;
                    return;
                }

                ui.transform.SetAsLastSibling();
                ui.userData = userData;
                await SystemShow(ui, switchUseAnimation);

                _currentMainUI = ui;
            }
            finally
            {
                _suppressRefreshCount--;
                RefreshUI();
            }

            if (needLoading && controller != null)
            {
                try
                {
                    await loadable.OnLoading(controller);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                try
                {
                    await _loadingHandler.HideLoading();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
