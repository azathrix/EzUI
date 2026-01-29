using System;
using System.Collections;
using System.Linq;
using Azathrix.EzUI.Core;
using Azathrix.EzUI.Events;
using Azathrix.Framework.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Azathrix.EzUI.Tests
{
    public class EzUIEzInputBridgePlayModeTests
    {
        private SystemRuntimeManager _manager;
        private TestResourcesLoader _loader;
        private Type _ezInputType;
        private Type _bridgeType;
        private object _ezInputSystem;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _manager = new SystemRuntimeManager { IsEditorMode = true };
            AzathrixFramework.SetEditorRuntimeManager(_manager);
            AzathrixFramework.MarkEditorStarted();

            _loader = new TestResourcesLoader();
            AzathrixFramework.ResourcesLoader = _loader;

            _ezInputType = Type.GetType("Azathrix.EzInput.Core.EzInputSystem, Azathrix.EzInput");
            _bridgeType = Type.GetType("Azathrix.EzUI.Integrations.EzInputBridgeSystem, Azathrix.EzUI.EzInputBridge");

            if (_ezInputType == null || _bridgeType == null)
                yield break;

            ConfigureEzInputSettings();

            yield return UniTask.ToCoroutine(async () =>
            {
                await _manager.RegisterSystemAsync(_ezInputType);
                await _manager.RegisterSystemAsync(_bridgeType);
            });

            _ezInputSystem = _manager.GetAllSystems()
                .FirstOrDefault(sys => sys.GetType() == _ezInputType);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AzathrixFramework.Dispatcher.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EzInputBridge_InputScheme_Changes_Map()
        {
            if (_ezInputType == null || _bridgeType == null || _ezInputSystem == null)
            {
                Assert.Ignore("EzInput 或 Bridge 未安装，跳过测试");
                yield break;
            }

            var ownerGo = new GameObject("EzInputBridge_Owner");
            var owner = ownerGo.AddComponent<TestPanel>();

            AzathrixFramework.Dispatcher.Dispatch(new UIInputSchemeChanged
            {
                previous = "Game",
                current = "UI",
                count = 1,
                source = owner
            });

            Assert.AreEqual("UI", GetOverlayValue(_ezInputSystem, "CurrentMap"));

            AzathrixFramework.Dispatcher.Dispatch(new UIInputSchemeChanged
            {
                previous = "UI",
                current = "Game",
                count = 0,
                source = owner
            });

            Assert.AreEqual("Game", GetOverlayValue(_ezInputSystem, "CurrentMap"));

            UnityEngine.Object.Destroy(ownerGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EzInputBridge_AnimationState_Controls_Input()
        {
            if (_ezInputType == null || _bridgeType == null || _ezInputSystem == null)
            {
                Assert.Ignore("EzInput 或 Bridge 未安装，跳过测试");
                yield break;
            }

            var ownerGo = new GameObject("EzInputBridge_AnimOwner");
            var owner = ownerGo.AddComponent<TestPanel>();

            AzathrixFramework.Dispatcher.Dispatch(new UIAnimationStateChanged
            {
                isPlaying = true,
                blockInput = true,
                source = owner
            });

            Assert.AreEqual("False", GetOverlayValue(_ezInputSystem, "InputState"));

            AzathrixFramework.Dispatcher.Dispatch(new UIAnimationStateChanged
            {
                isPlaying = false,
                blockInput = true,
                source = owner
            });

            Assert.AreEqual("True", GetOverlayValue(_ezInputSystem, "InputState"));

            UnityEngine.Object.Destroy(ownerGo);
            yield return null;
        }

        private static string GetOverlayValue(object ezInputSystem, string propertyName)
        {
            var prop = ezInputSystem.GetType().GetProperty(propertyName);
            var overlay = prop?.GetValue(ezInputSystem);
            if (overlay == null)
                return null;

            var valueProp = overlay.GetType().GetProperty("Value");
            if (valueProp != null)
            {
                var value = valueProp.GetValue(overlay);
                return value?.ToString();
            }

            return overlay.ToString();
        }

        private static void ConfigureEzInputSettings()
        {
            var settingsType = Type.GetType("Azathrix.EzInput.Settings.EzInputSettings, Azathrix.EzInput");
            if (settingsType == null)
                return;

            var settings = ScriptableObject.CreateInstance(settingsType);
            var autoCreateField = settingsType.GetField("autoCreatePlayerInput");
            if (autoCreateField != null)
                autoCreateField.SetValue(settings, false);

            var inputAssetField = settingsType.GetField("inputActionAsset");
            if (inputAssetField != null)
                inputAssetField.SetValue(settings, null);

            var setSettings = settingsType.GetMethod("SetSettings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            setSettings?.Invoke(null, new[] { settings });
        }
    }
}
