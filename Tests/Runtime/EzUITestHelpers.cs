using System;
using System.Collections.Generic;
using Azathrix.EzUI.Core;
using Azathrix.EzUI.Events;
using Azathrix.EzUI.Interfaces;
using Azathrix.Framework.Core;
using Azathrix.Framework.Events.Results;
using Azathrix.Framework.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Azathrix.EzUI.Tests
{
    internal sealed class TestResourcesLoader : IResourcesLoader
    {
        private readonly Dictionary<string, UnityEngine.Object> _assets = new Dictionary<string, UnityEngine.Object>();

        public void Add(string path, UnityEngine.Object asset)
        {
            if (string.IsNullOrWhiteSpace(path) || asset == null)
                return;
            _assets[path] = asset;
        }

        public T Load<T>(string key) where T : UnityEngine.Object
        {
            if (_assets.TryGetValue(key, out var obj))
                return obj as T;
            return null;
        }

        public UniTask<T> LoadAsync<T>(string key) where T : UnityEngine.Object
        {
            return UniTask.FromResult(Load<T>(key));
        }

        public UniTask LoadSceneAsync(string key, bool additive = false)
        {
            return UniTask.CompletedTask;
        }
    }

    internal static class TestPrefabFactory
    {
        public static GameObject CreatePanelPrefab<T>(string name, Color color) where T : Panel
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5;

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(480, 280);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            var img = go.GetComponent<Image>();
            img.color = color;

            go.AddComponent<T>();
            return go;
        }
    }

    internal sealed class TestInputSchemeHandler : ScriptableObject, IEzUIInputSchemeHandler
    {
        public int callCount;
        public string previous;
        public string current;
        public object source;

        public void ApplyInputScheme(string prev, string cur, object src)
        {
            callCount++;
            previous = prev;
            current = cur;
            source = src;
        }
    }

    internal sealed class UIEventRecorder : IDisposable
    {
        public int showCount;
        public int shownCount;
        public int hideCount;
        public int hiddenCount;
        public int closeCount;
        public int destroyCount;
        public int mainChangedCount;
        public int inputSchemeChangedCount;
        public int maskChangedCount;
        public int focusChangedCount;

        public Panel lastPanel;
        public Panel lastMainUI;
        public string lastInputScheme;
        public bool lastMaskActive;
        public Panel lastMaskTarget;
        public IUIFocus lastFocus;

        private readonly List<SubscriptionResult> _subs = new List<SubscriptionResult>();

        public void Start()
        {
            var dispatcher = AzathrixFramework.Dispatcher;

            _subs.Add(dispatcher.Subscribe<UIPanelShow>((ref UIPanelShow evt) =>
            {
                showCount++;
                lastPanel = evt.panel;
            }).AsResult());

            _subs.Add(dispatcher.Subscribe<UIPanelShown>((ref UIPanelShown evt) =>
            {
                shownCount++;
                lastPanel = evt.panel;
            }).AsResult());

            _subs.Add(dispatcher.Subscribe<UIPanelHide>((ref UIPanelHide evt) =>
            {
                hideCount++;
                lastPanel = evt.panel;
            }).AsResult());

            _subs.Add(dispatcher.Subscribe<UIPanelHidden>((ref UIPanelHidden evt) =>
            {
                hiddenCount++;
                lastPanel = evt.panel;
            }).AsResult());

            _subs.Add(dispatcher.Subscribe<UIPanelClose>((ref UIPanelClose evt) =>
            {
                closeCount++;
                lastPanel = evt.panel;
            }).AsResult());

            _subs.Add(dispatcher.Subscribe<UIPanelDestroyed>((ref UIPanelDestroyed evt) =>
            {
                destroyCount++;
                lastPanel = evt.panel;
            }).AsResult());

            _subs.Add(dispatcher.Subscribe<UIMainUIChanged>((ref UIMainUIChanged evt) =>
            {
                mainChangedCount++;
                lastMainUI = evt.current;
            }).AsResult());

            _subs.Add(dispatcher.Subscribe<UIInputSchemeChanged>((ref UIInputSchemeChanged evt) =>
            {
                inputSchemeChangedCount++;
                lastInputScheme = evt.current;
            }).AsResult());

            _subs.Add(dispatcher.Subscribe<UIMaskStateChanged>((ref UIMaskStateChanged evt) =>
            {
                maskChangedCount++;
                lastMaskActive = evt.active;
                lastMaskTarget = evt.target;
            }).AsResult());

            _subs.Add(dispatcher.Subscribe<UIFocusChanged>((ref UIFocusChanged evt) =>
            {
                focusChangedCount++;
                lastFocus = evt.current;
            }).AsResult());
        }

        public void Dispose()
        {
            for (int i = 0; i < _subs.Count; i++)
                _subs[i].Dispose();
            _subs.Clear();
        }
    }
}
