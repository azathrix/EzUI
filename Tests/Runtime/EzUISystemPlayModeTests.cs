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
    public class EzUISystemPlayModeTests
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
            _settings.autoCreateUICamera = true;
            _settings.panelPathFormat = "UI/{0}";
            _settings.defaultGameInputScheme = "Game";
            _settings.defaultPopUIInputScheme = "UI";
            _settings.inputSchemeSwitchMode = EzUISettings.InputSchemeSwitchMode.EventOnly;
            EzUISettings.SetSettings(_settings);

            RegisterPrefabs();

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
                _ui.DestroyAll(true);

            var root = GameObject.Find(_settings.autoCreatedUIRootName ?? "[UIRoot]");
            if (root != null)
                Object.Destroy(root);

            AzathrixFramework.Dispatcher.Clear();
            yield return null;
        }

        private void RegisterPrefabs()
        {
            AddPrefab<TestPanel>(new Color(0.2f, 0.6f, 0.9f, 0.9f));
            AddPrefab<TestMainUIA>(new Color(0.2f, 0.8f, 0.4f, 0.9f));
            AddPrefab<TestMainUIB>(new Color(0.8f, 0.4f, 0.2f, 0.9f));
            AddPrefab<TestPopUI>(new Color(0.7f, 0.2f, 0.9f, 0.9f));
        }

        private void AddPrefab<T>(Color color) where T : Panel
        {
            var path = string.Format(_settings.panelPathFormat, typeof(T).Name);
            var prefab = TestPrefabFactory.CreatePanelPrefab<T>(path, color);
            _loader.Add(path, prefab);
        }

        [UnityTest]
        public IEnumerator ShowHideClose_Panel_Events()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();

            var panel = _ui.Show<TestPanel>(false);
            Assert.IsNotNull(panel);
            Assert.IsTrue(panel.IsState(Panel.StateEnum.Shown));
            Assert.AreEqual(1, recorder.showCount);
            Assert.AreEqual(1, recorder.shownCount);

            _ui.Hide<TestPanel>(false);
            Assert.IsTrue(panel.IsState(Panel.StateEnum.Hidden));
            Assert.AreEqual(1, recorder.hideCount);
            Assert.AreEqual(1, recorder.hiddenCount);

            _ui.Close<TestPanel>(false);
            yield return null;

            Assert.IsNull(_ui.FindUI(_ui.GetPath(typeof(TestPanel))));
            Assert.GreaterOrEqual(recorder.closeCount, 1);
            Assert.GreaterOrEqual(recorder.destroyCount, 1);
        }

        [UnityTest]
        public IEnumerator MainUI_Switch_And_Close()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();

            var mainA = _ui.ShowMainUI<TestMainUIA>(false);
            Assert.AreEqual(mainA, _ui.CurrentMainUI);

            var mainB = _ui.ShowMainUI<TestMainUIB>(false);
            Assert.AreEqual(mainB, _ui.CurrentMainUI);
            Assert.GreaterOrEqual(recorder.mainChangedCount, 2);

            _ui.Close<TestMainUIB>(false);
            yield return null;

            Assert.IsNull(_ui.CurrentMainUI);
        }

        [UnityTest]
        public IEnumerator PopUI_Mask_InputScheme_Focus()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();

            var pop = _ui.Show<TestPopUI>(false);
            Assert.IsNotNull(pop);
            Assert.IsTrue(pop.IsState(Panel.StateEnum.Shown));
            Assert.AreEqual("UI", _ui.CurrentInputScheme);
            Assert.GreaterOrEqual(recorder.inputSchemeChangedCount, 1);
            Assert.IsTrue(recorder.lastMaskActive);
            Assert.AreEqual(pop, recorder.lastMaskTarget);
            Assert.GreaterOrEqual(recorder.focusChangedCount, 1);

            _ui.Hide<TestPopUI>(false);
            Assert.AreEqual("Game", _ui.CurrentInputScheme);
        }

        [UnityTest]
        public IEnumerator InputScheme_Handler_IsCalled()
        {
            var handler = ScriptableObject.CreateInstance<TestInputSchemeHandler>();
            _settings.inputSchemeSwitchMode = EzUISettings.InputSchemeSwitchMode.HandlerThenEvent;
            _settings.inputSchemeHandler = handler;
            EzUISettings.SetSettings(_settings);

            _ui.Show<TestPopUI>(false);

            Assert.AreEqual(1, handler.callCount);
            Assert.AreEqual("Game", handler.previous);
            Assert.AreEqual("UI", handler.current);
        }
    }
}
