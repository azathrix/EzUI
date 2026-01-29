using System.Collections;
using Azathrix.EzUI;
using Azathrix.EzUI.Core;
using Azathrix.Framework.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Azathrix.EzUI.Tests
{
    public class EzUICameraLateSetPlayModeTests
    {
        private SystemRuntimeManager _manager;
        private UISystem _ui;
        private TestResourcesLoader _loader;
        private EzUISettings _settings;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _manager = new SystemRuntimeManager { IsEditorMode = true };
            AzathrixFramework.SetEditorRuntimeManager(_manager);
            AzathrixFramework.MarkEditorStarted();

            _loader = new TestResourcesLoader();
            AzathrixFramework.ResourcesLoader = _loader;

            _settings = ScriptableObject.CreateInstance<EzUISettings>();
            _settings.autoCreateUIRoot = true;
            _settings.autoCreateEventSystem = false;
            _settings.autoCreateUICamera = false;
            _settings.panelPathFormat = "UI/{0}";
            EzUISettings.SetSettings(_settings);

            var path = string.Format(_settings.panelPathFormat, typeof(TestPanel).Name);
            var prefab = TestPrefabFactory.CreatePanelPrefab<TestPanel>(path, Color.gray);
            _loader.Add(path, prefab);

            yield return UniTask.ToCoroutine(async () =>
            {
                await _manager.RegisterSystemAsync(typeof(UISystem));
            });

            _ui = _manager.GetSystem<UISystem>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_ui != null)
            {
                var destroyHandle = _ui.DestroyAll(true);
                if (destroyHandle != null)
                    yield return destroyHandle.Task.ToCoroutine();
            }

            var root = GameObject.Find(_settings.autoCreatedUIRootName ?? "[UIRoot]");
            if (root != null)
                Object.Destroy(root);

            AzathrixFramework.Dispatcher.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LayerCanvas_Updates_When_SetUICamera_Late()
        {
            var handle = _ui.Show<TestPanel>(false);
            yield return handle.Task.ToCoroutine();
            var panel = handle.panel;

            var layerCanvas = panel.transform.parent.GetComponent<Canvas>();
            Assert.IsNotNull(layerCanvas);
            Assert.IsNull(layerCanvas.worldCamera);

            var camGo = new GameObject("UICamera_Late");
            var cam = camGo.AddComponent<Camera>();

            _ui.SetUICamera(cam);

            Assert.AreEqual(cam, layerCanvas.worldCamera);

            Object.Destroy(camGo);
        }
    }
}
