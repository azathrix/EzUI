#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Azathrix.EzUI.Core;
using Azathrix.EzUI.Events;
using Azathrix.EzUI.Interfaces;
using Azathrix.Framework.Core;
using Azathrix.Framework.Events.Results;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Azathrix.EzUI.DebugTools
{
    /// <summary>
    /// EzUI debug panel (Editor only)
    /// </summary>
    public class EzUIDebugPanel : MonoBehaviour
    {
        [Header("Window")]
        public bool showWindow = true;
        public KeyCode toggleKey = KeyCode.F10;
        public int maxEvents = 200;

        [Header("Actions")]
        public bool showActions = true;

        private readonly List<string> _logs = new List<string>();
        private readonly List<SubscriptionResult> _subscriptions = new List<SubscriptionResult>();
        private Vector2 _scroll;

        private UISystem _uiSystem;
        private bool _maskActive;
        private Panel _maskTarget;
        private IUIFocus _focus;
        private Panel _mainUI;
        private string _inputScheme;

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
                showWindow = !showWindow;
#elif ENABLE_INPUT_SYSTEM
            if (toggleKey != KeyCode.None && IsKeyPressed(Keyboard.current, toggleKey))
                showWindow = !showWindow;
#endif

            if (_uiSystem == null)
                _uiSystem = AzathrixFramework.EffectiveRuntimeManager?.GetSystem<UISystem>();
        }

#if ENABLE_INPUT_SYSTEM
        private static bool IsKeyPressed(Keyboard keyboard, KeyCode keyCode)
        {
            if (keyboard == null)
                return false;

            switch (keyCode)
            {
                case KeyCode.F1: return keyboard.f1Key.wasPressedThisFrame;
                case KeyCode.F2: return keyboard.f2Key.wasPressedThisFrame;
                case KeyCode.F3: return keyboard.f3Key.wasPressedThisFrame;
                case KeyCode.F4: return keyboard.f4Key.wasPressedThisFrame;
                case KeyCode.F5: return keyboard.f5Key.wasPressedThisFrame;
                case KeyCode.F6: return keyboard.f6Key.wasPressedThisFrame;
                case KeyCode.F7: return keyboard.f7Key.wasPressedThisFrame;
                case KeyCode.F8: return keyboard.f8Key.wasPressedThisFrame;
                case KeyCode.F9: return keyboard.f9Key.wasPressedThisFrame;
                case KeyCode.F10: return keyboard.f10Key.wasPressedThisFrame;
                case KeyCode.F11: return keyboard.f11Key.wasPressedThisFrame;
                case KeyCode.F12: return keyboard.f12Key.wasPressedThisFrame;
                case KeyCode.Escape: return keyboard.escapeKey.wasPressedThisFrame;
                case KeyCode.Tab: return keyboard.tabKey.wasPressedThisFrame;
                case KeyCode.Space: return keyboard.spaceKey.wasPressedThisFrame;
                case KeyCode.Return: return keyboard.enterKey.wasPressedThisFrame;
                case KeyCode.KeypadEnter: return keyboard.numpadEnterKey.wasPressedThisFrame;
                case KeyCode.BackQuote: return keyboard.backquoteKey.wasPressedThisFrame;
                default: return false;
            }
        }
#endif

        private void SubscribeEvents()
        {
            var dispatcher = AzathrixFramework.Dispatcher;
            _subscriptions.Add(dispatcher.Subscribe<UIRootCreated>((ref UIRootCreated evt) =>
            {
                AddLog($"UIRoot Created: {(evt.root ? evt.root.name : "null")}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIPanelCreated>((ref UIPanelCreated evt) =>
            {
                AddLog($"Panel Created: {evt.path}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIPanelDestroyed>((ref UIPanelDestroyed evt) =>
            {
                AddLog($"Panel Destroyed: {evt.path}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIPanelShow>((ref UIPanelShow evt) =>
            {
                AddLog($"Panel Show: {evt.path}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIPanelShown>((ref UIPanelShown evt) =>
            {
                AddLog($"Panel Shown: {evt.path}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIPanelHide>((ref UIPanelHide evt) =>
            {
                AddLog($"Panel Hide: {evt.path}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIPanelHidden>((ref UIPanelHidden evt) =>
            {
                AddLog($"Panel Hidden: {evt.path}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIPanelClose>((ref UIPanelClose evt) =>
            {
                AddLog($"Panel Close: {evt.path}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIPanelStateChanged>((ref UIPanelStateChanged evt) =>
            {
                AddLog($"Panel State: {evt.panel?.name} {evt.previous} -> {evt.current}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIFocusChanged>((ref UIFocusChanged evt) =>
            {
                _focus = evt.current;
                AddLog($"Focus Changed: {evt.previous} -> {evt.current}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIMaskStateChanged>((ref UIMaskStateChanged evt) =>
            {
                _maskActive = evt.active;
                _maskTarget = evt.target;
                AddLog($"Mask: active={evt.active}, target={evt.target?.name}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIMainUIChanged>((ref UIMainUIChanged evt) =>
            {
                _mainUI = evt.current;
                AddLog($"MainUI: {evt.previous?.name} -> {evt.current?.name}");
            }).AsResult());

            _subscriptions.Add(dispatcher.Subscribe<UIInputSchemeChanged>((ref UIInputSchemeChanged evt) =>
            {
                _inputScheme = evt.current;
                AddLog($"InputScheme: {evt.previous} -> {evt.current} (count={evt.count})");
            }).AsResult());
        }

        private void UnsubscribeEvents()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
        }

        private void AddLog(string msg)
        {
            _logs.Add($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
            if (_logs.Count > maxEvents)
                _logs.RemoveAt(0);
        }

        private void OnGUI()
        {
            if (!showWindow)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 560, 640), GUI.skin.window);
            GUILayout.Label("EzUI Debug Panel");

            GUILayout.Label($"MainUI: {_mainUI?.name ?? "null"}");
            GUILayout.Label($"InputScheme: {_inputScheme ?? (_uiSystem?.CurrentInputScheme ?? "null")}");
            GUILayout.Label($"Focus: {_focus}");
            GUILayout.Label($"Mask: {_maskActive} target={_maskTarget?.name ?? "null"}");

            if (showActions && _uiSystem != null)
            {
                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Show Main A"))
                    _uiSystem.ShowMainUI<EzUITestMainUIA>(false);
                if (GUILayout.Button("Show Main B"))
                    _uiSystem.ShowMainUI<EzUITestMainUIB>(false);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Show Panel"))
                    _uiSystem.Show<EzUITestPanel>(false);
                if (GUILayout.Button("Hide Panel"))
                    _uiSystem.Hide<EzUITestPanel>(false);
                if (GUILayout.Button("Close Panel"))
                    _uiSystem.Close<EzUITestPanel>(false);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Show Pop"))
                    _uiSystem.Show<EzUITestPopUI>(false);
                if (GUILayout.Button("Hide Pop"))
                    _uiSystem.Hide<EzUITestPopUI>(false);
                if (GUILayout.Button("Close Pop"))
                    _uiSystem.Close<EzUITestPopUI>(false);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Destroy All"))
                    _uiSystem.DestroyAll(true);
                if (GUILayout.Button("Refresh UI"))
                    _uiSystem.RefreshUI();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            _scroll = GUILayout.BeginScrollView(_scroll, false, true, GUILayout.Height(420));
            for (int i = _logs.Count - 1; i >= 0; i--)
                GUILayout.Label(_logs[i]);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }
    }
}
#endif
