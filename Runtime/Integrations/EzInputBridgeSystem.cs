#if EZINPUT_INSTALLED
using System.Collections.Generic;
using Azathrix.EzInput.Core;
using Azathrix.EzInput.Enums;
using Azathrix.EzUI.Events;
using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Events.Results;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.GameKit.Runtime.Utils;
using Cysharp.Threading.Tasks;

namespace Azathrix.EzUI.Integrations
{
    /// <summary>
    /// EzUI -> EzInput bridge system (optional).
    /// </summary>
    public sealed class EzInputBridgeSystem : ISystem, ISystemRegister, ISystemInitialize
    {
        [WeakInject] public EzInputSystem EzInput { get; set; }

        private SubscriptionResult _schemeSub;
        private SubscriptionResult _animSub;
        private readonly Dictionary<object, Token> _ownerTokenMap = new();
        private readonly Dictionary<object, Token> _animationTokenMap = new();

        public UniTask OnInitializeAsync() => UniTask.CompletedTask;

        public void OnRegister()
        {
            if (EzInput == null)
                return;

            var dispatcher = AzathrixFramework.Dispatcher;
            _schemeSub = dispatcher.Subscribe<UIInputSchemeChanged>(OnUIInputSchemeChanged);
            _animSub = dispatcher.Subscribe<UIAnimationStateChanged>(OnUIAnimationStateChanged);
        }

        public void OnUnRegister()
        {
            _schemeSub.Unsubscribe();
            _animSub.Unsubscribe();

            if (EzInput != null)
            {
                foreach (var token in _ownerTokenMap.Values)
                {
                    if (token.IsValid)
                        EzInput.RemoveMap(token);
                }

                foreach (var token in _animationTokenMap.Values)
                {
                    if (token.IsValid)
                        EzInput.EnableInput(token);
                }
            }

            _animationTokenMap.Clear();
            _ownerTokenMap.Clear();
        }

        private void OnUIInputSchemeChanged(ref UIInputSchemeChanged evt)
        {
            if (EzInput == null)
                return;

            object source = evt.source;
            if (evt.current == "UI" || evt.current == "Menu")
            {
                SetMap(source, InputMapType.UI, evt.count);
            }
            else if (evt.current == "Game" || string.IsNullOrEmpty(evt.current))
            {
                RemoveMap(source);
            }
            else
            {
                SetMap(source, InputMapType.UI, evt.count);
            }
        }

        private void OnUIAnimationStateChanged(ref UIAnimationStateChanged evt)
        {
            if (EzInput == null)
                return;
            if (!evt.blockInput)
                return;

            var source = evt.source;
            if (evt.isPlaying)
            {
                if (!_animationTokenMap.ContainsKey(source))
                    _animationTokenMap[source] = EzInput.DisableInput();
            }
            else
            {
                if (_animationTokenMap.TryGetValue(source, out var token))
                {
                    EzInput.EnableInput(token);
                    _animationTokenMap.Remove(source);
                }
            }
        }

        private Token SetMap(object owner, InputMapType type, int priority = 0)
        {
            if (owner != null && _ownerTokenMap.TryGetValue(owner, out var existing))
            {
                EzInput.SetMap(existing, type, priority);
                return existing;
            }

            var token = EzInput.SetMap(type, priority);
            if (owner != null)
                _ownerTokenMap[owner] = token;
            return token;
        }

        private void RemoveMap(object owner)
        {
            if (owner == null)
                return;

            if (_ownerTokenMap.TryGetValue(owner, out var token))
            {
                EzInput.RemoveMap(token);
                _ownerTokenMap.Remove(owner);
            }
        }
    }
}
#endif
