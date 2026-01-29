using System;
using Azathrix.EzUI.Events;
using Azathrix.EzUI.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Azathrix.EzUI.Core
{
    // 队列与状态机驱动的操作流程
    public partial class UISystem
    {
        private UIOperationHandle EnqueueOperation(UIOperationHandle handle)
        {
            _operationQueue.Enqueue(handle);
            if (!_isProcessingQueue)
                ProcessOperationQueueAsync().Forget();
            return handle;
        }

        private async UniTask ProcessOperationQueueAsync()
        {
            if (_isProcessingQueue)
                return;

            _isProcessingQueue = true;
            try
            {
                while (_operationQueue.Count > 0)
                {
                    var handle = _operationQueue.Dequeue();
                    handle.state = UIOperationState.Running;

                    try
                    {
                        var panel = await ExecuteOperationAsync(handle);
                        handle.panel = panel;
                        handle.state = UIOperationState.Completed;
                        handle.completion.TrySetResult(panel);
                    }
                    catch (Exception e)
                    {
                        handle.exception = e;
                        handle.state = UIOperationState.Failed;
                        handle.completion.TrySetException(e);
                    }
                }
            }
            finally
            {
                _isProcessingQueue = false;
                if (_operationQueue.Count > 0)
                    ProcessOperationQueueAsync().Forget();
            }
        }

        private async UniTask<Panel> ExecuteOperationAsync(UIOperationHandle handle)
        {
            switch (handle.type)
            {
                case UIOperationType.Show:
                    return await ExecuteShowAsync(handle.path, handle.useAnimation, handle.userData);
                case UIOperationType.Hide:
                    return await ExecuteHideAsync(handle);
                case UIOperationType.Close:
                    return await ExecuteCloseAsync(handle);
                case UIOperationType.Toggle:
                    return await ExecuteToggleAsync(handle);
                case UIOperationType.Destroy:
                    return await ExecuteDestroyAsync(handle);
                case UIOperationType.DestroyAll:
                    await ExecuteDestroyAllAsync(handle);
                    return null;
                default:
                    return null;
            }
        }

        private async UniTask<Panel> ExecuteShowAsync(string path, bool useAnimation, object userData)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var ui = FindUI(path);
            Panel prefab = null;
            bool isMainUI;
            if (ui != null)
            {
                isMainUI = ui is IMainUI;
            }
            else
            {
                prefab = LoadUI(path);
                if (prefab == null)
                    return null;
                isMainUI = prefab is IMainUI;
            }

            if (!isMainUI)
            {
                if (ui != null && ui.IsVisible)
                {
                    await AutoCloseInternal(ui, AutoCloseReason.MainUISwitch, false);
                    if (!ui || ui.IsState(Panel.StateEnum.Closed))
                        ui = null;
                }

                if (ui == null)
                    ui = Instantiate(path);

                if (ui == null)
                    return null;

                ui.transform.SetAsLastSibling();
                ui.userData = userData;
                await SystemShowAsync(ui, useAnimation);
                return ui;
            }

            var oldMain = _currentMainUI;

            // 同一个MainUI正在显示时，强制新建实例以走完整流程
            if (ui != null && ui.IsVisible)
                ui = null;

            if (ui == null)
                ui = Instantiate(path);

            if (ui == null)
            {
                _currentMainUI = null;
                return null;
            }

            var loadable = (prefab as IMainUILoadable) ?? (ui as IMainUILoadable);
            var needLoading = _loadingHandler != null && loadable != null;

            await ShowMainUIFlowAsync(path, ui, oldMain, useAnimation, userData, loadable, needLoading);
            return _currentMainUI;
        }

        private async UniTask<Panel> ExecuteHideAsync(UIOperationHandle handle)
        {
            var ui = handle.target ?? FindUI(handle.path);
            if (!ui)
                return null;
            await SystemHideAsync(ui, handle.useAnimation);
            return ui;
        }

        private async UniTask<Panel> ExecuteCloseAsync(UIOperationHandle handle)
        {
            var ui = handle.target ?? FindUI(handle.path);
            if (!ui)
                return null;
            await SystemCloseAsync(ui, handle.useAnimation);
            return ui;
        }

        private async UniTask<Panel> ExecuteToggleAsync(UIOperationHandle handle)
        {
            if (string.IsNullOrWhiteSpace(handle.path))
                return null;

            var ui = FindUI(handle.path);
            if (ui == null)
                return await ExecuteShowAsync(handle.path, handle.useAnimation, handle.userData);

            if (ui.IsState(Panel.StateEnum.Hidden))
            {
                await SystemShowAsync(ui, handle.useAnimation);
                return ui;
            }

            if (ui.IsState(Panel.StateEnum.Shown))
            {
                await SystemHideAsync(ui, handle.useAnimation);
                return ui;
            }

            return ui;
        }

        private async UniTask<Panel> ExecuteDestroyAsync(UIOperationHandle handle)
        {
            var ui = handle.target ?? FindUI(handle.path);
            if (!ui)
                return null;
            if (_persistenceUI.Contains(ui) && !handle.force)
                return ui;

            var wasShown = ui.IsState(Panel.StateEnum.Shown);
            if (wasShown)
                await SystemHide(ui, false);

            DestroyInternal(ui, handle.force, true);
            return ui;
        }

        private async UniTask ExecuteDestroyAllAsync(UIOperationHandle handle)
        {
            if (handle.force)
                _persistenceUI.Clear();

            var list = new System.Collections.Generic.List<Panel>();
            foreach (var ui in _instanceUIs)
            {
                if (!ui)
                {
                    list.Add(ui);
                    continue;
                }

                if (_persistenceUI.Contains(ui))
                    continue;
                list.Add(ui);
            }

            _suppressRefreshCount++;
            try
            {
                foreach (var ui in list)
                {
                    _instanceUIs.Remove(ui);
                    if (!ui)
                        continue;

                    if (ui.IsState(Panel.StateEnum.Shown))
                        await SystemHide(ui, false);

                    DestroyInternal(ui, true, false);
                }
            }
            finally
            {
                _suppressRefreshCount--;
            }

            RefreshUI();
        }

        private UniTask SystemShow(Panel ui, bool useAnimation = true)
        {
            if (ui == null)
                return UniTask.CompletedTask;
            if (!ui.IsState(Panel.StateEnum.Hidden))
                return UniTask.CompletedTask;
            return SystemShowAsync(ui, useAnimation);
        }

        private async UniTask SystemShowAsync(Panel ui, bool useAnimation = true)
        {
            if (ui == null)
                return;
            if (!ui.IsState(Panel.StateEnum.Hidden))
                return;

            if (!useAnimation)
            {
                ui.CancelAnimation();
                ui.IsShowingInternal = true;
                ui.gameObject.SetActive(true);
                ui.InvokeOnShow();
                ui.SetStateInternal(Panel.StateEnum.Shown);
                RefreshUI();
                ui.IsShowingInternal = false;
                return;
            }

            ui.IsShowingInternal = true;
            ui.gameObject.SetActive(true);
            ui.InvokeOnShow();
            RefreshUI();

            var token = ui.StartAnimationToken();
            ui.IsShowingInternal = true;
            await ui.RunShowAnimationAsync(token).SuppressCancellationThrow();
            if (token.IsCancellationRequested)
            {
                ui.IsShowingInternal = false;
                return;
            }

            ui.SetStateInternal(Panel.StateEnum.Shown);
            ui.IsShowingInternal = false;
        }

        private UniTask SystemHide(Panel ui, bool useAnimation = true)
        {
            if (ui == null)
                return UniTask.CompletedTask;
            if (ui.IsState(Panel.StateEnum.Closed))
                return UniTask.CompletedTask;
            if (ui.IsState(Panel.StateEnum.Hidden) && !ui.IsVisible)
                return UniTask.CompletedTask;
            return SystemHideAsync(ui, useAnimation);
        }

        private async UniTask SystemHideAsync(Panel ui, bool useAnimation = true)
        {
            if (ui == null)
                return;
            if (ui.IsState(Panel.StateEnum.Closed))
                return;

            var wasVisible = ui.IsVisible;
            if (ui.IsState(Panel.StateEnum.Hidden) && !wasVisible)
                return;

            if (!useAnimation)
            {
                ui.CancelAnimation();
                ui.IsHidingInternal = true;
                if (wasVisible)
                    ui.InvokeOnHide();
                ui.gameObject.SetActive(false);
                ui.SetStateInternal(Panel.StateEnum.Hidden);
                RefreshUI();
                ui.IsHidingInternal = false;
                return;
            }

            ui.IsHidingInternal = true;
            if (wasVisible)
                ui.InvokeOnHide();
            RefreshUI();

            var token = ui.StartAnimationToken();
            ui.IsHidingInternal = true;
            await ui.RunHideAnimationAsync(token).SuppressCancellationThrow();
            if (token.IsCancellationRequested)
            {
                ui.IsHidingInternal = false;
                return;
            }
            ui.gameObject.SetActive(false);

            ui.SetStateInternal(Panel.StateEnum.Hidden);
            ui.IsHidingInternal = false;
        }

        private UniTask SystemClose(Panel ui, bool useAnimation = true)
        {
            if (ui == null)
                return UniTask.CompletedTask;
            return SystemCloseAsync(ui, useAnimation);
        }

        private async UniTask SystemCloseAsync(Panel ui, bool useAnimation = true)
        {
            if (ui == null)
                return;
            if (ui.IsState(Panel.StateEnum.Closed))
                return;

            var wasVisible = ui.IsVisible;
            if (!useAnimation)
            {
                ui.CancelAnimation();
                ui.IsHidingInternal = true;
                if (wasVisible)
                    ui.InvokeOnHide();
                ui.InvokeOnClose();
                ui.gameObject.SetActive(false);
                ui.SetStateInternal(Panel.StateEnum.Hidden);
                ui.SetStateInternal(Panel.StateEnum.Closed);
                RefreshUI();
                ui.IsHidingInternal = false;
                DestroyInternal(ui, false, false);
                return;
            }

            ui.IsHidingInternal = true;
            if (wasVisible)
                ui.InvokeOnHide();
            ui.InvokeOnClose();
            RefreshUI();

            var token = ui.StartAnimationToken();
            ui.IsHidingInternal = true;
            await ui.RunHideAnimationAsync(token).SuppressCancellationThrow();
            if (token.IsCancellationRequested)
            {
                ui.IsHidingInternal = false;
                return;
            }
            ui.gameObject.SetActive(false);

            ui.SetStateInternal(Panel.StateEnum.Hidden);
            ui.SetStateInternal(Panel.StateEnum.Closed);

            ui.IsHidingInternal = false;
            DestroyInternal(ui, false, false);
        }

        private bool DestroyInternal(Panel ui, bool force, bool refresh)
        {
            if (ui == null)
                return false;

            if (_persistenceUI.Contains(ui) && !force)
                return false;

            _instanceUIs.Remove(ui);
            _persistenceUI.Remove(ui);
            SetInputScheme(ui, null);

            Dispatch(new UIPanelDestroyed
            {
                panel = ui,
                path = ui.path
            });

            ui.CancelAnimation();
            Object.Destroy(ui.gameObject);
            ui.InvokeOnClosed();

            if (refresh)
                RefreshUI();
            return true;
        }
    }
}
