#if EZINPUT_INSTALLED
using System.Collections.Generic;
using Azathrix.EzInput.Core;
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
                foreach (var token in _animationTokenMap.Values)
                {
                    if (token.IsValid)
                        EzInput.EnableInput(token);
                }
            }

            _animationTokenMap.Clear();
        }

        private void OnUIInputSchemeChanged(ref UIInputSchemeChanged evt)
        {
            if (EzInput == null)
                return;

            var mapName = ResolveMapName(evt.current);
            if (string.IsNullOrWhiteSpace(mapName))
                return;

            EzInput.SetMap(mapName);
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

        private static string ResolveMapName(string scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme))
                return null;
            if (scheme == "Menu")
                return "UI";
            return scheme;
        }
    }
}
#endif
