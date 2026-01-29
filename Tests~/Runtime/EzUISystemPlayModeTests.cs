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

        private void RegisterPrefabs()
        {
            AddPrefab<TestPanel>(new Color(0.2f, 0.6f, 0.9f, 0.9f));
            AddPrefab<TestMainUIA>(new Color(0.2f, 0.8f, 0.4f, 0.9f));
            AddPrefab<TestMainUIB>(new Color(0.8f, 0.4f, 0.2f, 0.9f));
            AddPrefab<TestMainUIHide>(new Color(0.2f, 0.4f, 0.8f, 0.9f));
            AddPrefab<TestMainUIClose>(new Color(0.8f, 0.2f, 0.4f, 0.9f));
            AddPrefab<TestMainUINone>(new Color(0.4f, 0.8f, 0.2f, 0.9f));
            AddPrefab<TestLoadableMainUI>(new Color(0.6f, 0.6f, 0.2f, 0.9f));
            AddPrefab<TestPersistentPanel>(new Color(0.1f, 0.7f, 0.7f, 0.9f));
            AddPrefab<TestPopUI>(new Color(0.7f, 0.2f, 0.9f, 0.9f));
            AddPrefab<TestPopUIAlt>(new Color(0.2f, 0.9f, 0.7f, 0.9f));
            AddPrefab<TestMaskClickPopUI>(new Color(0.9f, 0.7f, 0.2f, 0.9f));
            AddPrefabWithView<TestLifecyclePanel, TestLifecycleView>(new Color(0.3f, 0.3f, 0.3f, 0.9f));
            AddPrefab<TestAutoCloseNonePanel>(new Color(0.6f, 0.2f, 0.6f, 0.9f));
            AddPrefab<TestReentrantPanel>(new Color(0.8f, 0.8f, 0.2f, 0.9f));
            AddPrefab<TestAnimatedPanel>(new Color(0.2f, 0.8f, 0.8f, 0.9f));
            AddPrefab<TestSequencePanel>(new Color(0.9f, 0.2f, 0.2f, 0.9f));
        }

        private void AddPrefab<T>(Color color) where T : Panel
        {
            var path = string.Format(_settings.panelPathFormat, typeof(T).Name);
            var prefab = TestPrefabFactory.CreatePanelPrefab<T>(path, color);
            _loader.Add(path, prefab);
        }

        private void AddPrefabWithView<TPanel, TView>(Color color)
            where TPanel : Panel
            where TView : View
        {
            var path = string.Format(_settings.panelPathFormat, typeof(TPanel).Name);
            var prefab = TestPrefabFactory.CreatePanelPrefabWithView<TPanel, TView>(path, color);
            _loader.Add(path, prefab);
        }

        [UnityTest]
        public IEnumerator ShowHideClose_Panel_Events()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();

            var showHandle = _ui.Show<TestPanel>(false);
            yield return showHandle.Task.ToCoroutine();
            var panel = showHandle.panel;
            Assert.IsNotNull(panel);
            Assert.IsTrue(panel.IsState(Panel.StateEnum.Shown));
            Assert.AreEqual(1, recorder.showCount);
            Assert.AreEqual(1, recorder.shownCount);

            var hideHandle = _ui.Hide<TestPanel>(false);
            yield return hideHandle.Task.ToCoroutine();
            Assert.IsTrue(panel.IsState(Panel.StateEnum.Hidden));
            Assert.AreEqual(1, recorder.hideCount);
            Assert.AreEqual(1, recorder.hiddenCount);

            var closeHandle = _ui.Close<TestPanel>(false);
            yield return closeHandle.Task.ToCoroutine();

            Assert.IsTrue(_ui.FindUI(_ui.GetPath(typeof(TestPanel))) == null);
            Assert.GreaterOrEqual(recorder.closeCount, 1);
            Assert.GreaterOrEqual(recorder.destroyCount, 1);
        }

        [UnityTest]
        public IEnumerator Show_SamePanel_Recreates()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();

            var firstHandle = _ui.Show<TestPanel>(false);
            yield return firstHandle.Task.ToCoroutine();
            var first = firstHandle.panel;

            var secondHandle = _ui.Show<TestPanel>(false);
            yield return secondHandle.Task.ToCoroutine();
            var second = secondHandle.panel;

            Assert.IsNotNull(second);
            Assert.AreNotEqual(first, second);
            Assert.GreaterOrEqual(recorder.destroyCount, 1);
            Assert.GreaterOrEqual(recorder.createdCount, 2);
        }

        [UnityTest]
        public IEnumerator MainUI_Switch_And_Close()
        {
            TestMainUIA.Reset();
            TestMainUIB.Reset();

            var mainAHandle = _ui.Show<TestMainUIA>(false);
            yield return mainAHandle.Task.ToCoroutine();
            var mainA = mainAHandle.panel;
            Assert.AreEqual(mainA, _ui.CurrentMainUI);

            var mainBHandle = _ui.Show<TestMainUIB>(false);
            yield return mainBHandle.Task.ToCoroutine();
            var mainB = mainBHandle.panel;
            Assert.AreEqual(mainB, _ui.CurrentMainUI);

            var closeHandle = _ui.Close<TestMainUIB>(false);
            yield return closeHandle.Task.ToCoroutine();

            Assert.IsTrue(_ui.FindUI(_ui.GetPath(typeof(TestMainUIB))) == null);
        }

        [UnityTest]
        public IEnumerator MainUI_Show_Same_Recreates()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();
            TestMainUIA.Reset();

            var firstHandle = _ui.Show<TestMainUIA>(false);
            yield return firstHandle.Task.ToCoroutine();
            var first = firstHandle.panel;
            var secondHandle = _ui.Show<TestMainUIA>(false);
            yield return secondHandle.Task.ToCoroutine();
            var second = secondHandle.panel;

            Assert.IsNotNull(second);
            Assert.AreEqual(second, _ui.CurrentMainUI);
            if (second != first)
                Assert.GreaterOrEqual(recorder.destroyCount, 1);
        }

        [UnityTest]
        public IEnumerator MainUI_ChangeBehavior_Hide()
        {
            var mainHideHandle = _ui.Show<TestMainUIHide>(false);
            yield return mainHideHandle.Task.ToCoroutine();
            var mainHide = mainHideHandle.panel;

            var mainBHandle = _ui.Show<TestMainUIB>(false);
            yield return mainBHandle.Task.ToCoroutine();
            var mainB = mainBHandle.panel;

            Assert.IsNotNull(mainHide);
            Assert.IsTrue(mainHide.IsState(Panel.StateEnum.Hidden));
            Assert.AreEqual(mainB, _ui.CurrentMainUI);
        }

        [UnityTest]
        public IEnumerator MainUI_ChangeBehavior_Close()
        {
            var mainCloseHandle = _ui.Show<TestMainUIClose>(false);
            yield return mainCloseHandle.Task.ToCoroutine();
            var mainClose = mainCloseHandle.panel;

            var mainBHandle = _ui.Show<TestMainUIB>(false);
            yield return mainBHandle.Task.ToCoroutine();
            var mainB = mainBHandle.panel;

            var closed = _ui.FindUI(_ui.GetPath(typeof(TestMainUIClose)));
            Assert.IsTrue(closed == null);
            Assert.AreEqual(mainB, _ui.CurrentMainUI);
        }

        [UnityTest]
        public IEnumerator MainUI_ChangeBehavior_None()
        {
            var mainNoneHandle = _ui.Show<TestMainUINone>(false);
            yield return mainNoneHandle.Task.ToCoroutine();
            var mainNone = mainNoneHandle.panel;

            var mainBHandle = _ui.Show<TestMainUIB>(false);
            yield return mainBHandle.Task.ToCoroutine();
            var mainB = mainBHandle.panel;

            Assert.IsNotNull(mainNone);
            Assert.IsTrue(mainNone.IsState(Panel.StateEnum.Shown));
            Assert.AreEqual(mainB, _ui.CurrentMainUI);
        }

        [UnityTest]
        public IEnumerator MainUI_LoadingHandler_Invoked()
        {
            TestLoadableMainUI.Reset();
            var handler = new TestLoadingHandler();
            _ui.SetLoadingHandler(handler);

            var firstHandle = _ui.Show<TestLoadableMainUI>(false);
            yield return firstHandle.Task.ToCoroutine();
            var first = firstHandle.panel;

            Assert.IsNotNull(first);
            Assert.AreEqual(1, handler.showCount);
            Assert.AreEqual(1, handler.hideCount);
            Assert.AreEqual(1, TestLoadableMainUI.onLoadingCount);
            Assert.AreEqual("test", handler.lastConfig.loadingType);
            Assert.GreaterOrEqual(handler.controller.progressCount, 1);

            var secondHandle = _ui.Show<TestLoadableMainUI>(false);
            yield return secondHandle.Task.ToCoroutine();
            var second = secondHandle.panel;

            Assert.GreaterOrEqual(handler.showCount, 2);
            Assert.GreaterOrEqual(handler.hideCount, 2);
            Assert.GreaterOrEqual(TestLoadableMainUI.onLoadingCount, 2);
            Assert.IsNotNull(second);
        }

        [UnityTest]
        public IEnumerator MainUI_Cleared_On_Close()
        {
            TestMainUIA.Reset();

            var mainHandle = _ui.Show<TestMainUIA>(false);
            yield return mainHandle.Task.ToCoroutine();
            var main = mainHandle.panel;
            Assert.AreEqual(main, _ui.CurrentMainUI);

            var closeHandle = _ui.Close<TestMainUIA>(false);
            yield return closeHandle.Task.ToCoroutine();

            Assert.IsTrue(_ui.CurrentMainUI == null);
        }

        [UnityTest]
        public IEnumerator Hide_When_AlreadyHidden_NoExtraEvents()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();

            var showHandle = _ui.Show<TestPanel>(false);
            yield return showHandle.Task.ToCoroutine();
            var panel = showHandle.panel;

            var firstHideHandle = _ui.Hide<TestPanel>(false);
            yield return firstHideHandle.Task.ToCoroutine();

            var hideCount = recorder.hideCount;
            var hiddenCount = recorder.hiddenCount;

            var secondHideHandle = _ui.Hide<TestPanel>(false);
            yield return secondHideHandle.Task.ToCoroutine();

            Assert.AreEqual(hideCount, recorder.hideCount);
            Assert.AreEqual(hiddenCount, recorder.hiddenCount);
            Assert.IsTrue(panel.IsState(Panel.StateEnum.Hidden));
        }

        [UnityTest]
        public IEnumerator Close_When_Hidden_Destroys()
        {
            var showHandle = _ui.Show<TestPanel>(false);
            yield return showHandle.Task.ToCoroutine();
            var panel = showHandle.panel;

            var hideHandle = _ui.Hide<TestPanel>(false);
            yield return hideHandle.Task.ToCoroutine();

            var closeHandle = _ui.Close<TestPanel>(false);
            yield return closeHandle.Task.ToCoroutine();

            Assert.IsTrue(_ui.FindUI(_ui.GetPath(typeof(TestPanel))) == null);
        }

        [UnityTest]
        public IEnumerator MainUI_Switch_Closes_NonMainUI()
        {
            var mainAHandle = _ui.Show<TestMainUIA>(false);
            yield return mainAHandle.Task.ToCoroutine();
            var mainA = mainAHandle.panel;

            var nonMainHandle = _ui.Show<TestPanel>(false);
            yield return nonMainHandle.Task.ToCoroutine();
            var nonMain = nonMainHandle.panel;
            Assert.IsNotNull(nonMain);

            var mainBHandle = _ui.Show<TestMainUIB>(false);
            yield return mainBHandle.Task.ToCoroutine();
            var mainB = mainBHandle.panel;

            Assert.IsTrue(_ui.FindUI(_ui.GetPath(typeof(TestPanel))) == null);
            Assert.AreEqual(mainB, _ui.CurrentMainUI);
        }

        [UnityTest]
        public IEnumerator Queue_Orders_Show_Hide_Show()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();

            var show1 = _ui.Show<TestPanel>(false);
            var hide = _ui.Hide<TestPanel>(false);
            var show2 = _ui.Show<TestPanel>(false);

            yield return show2.Task.ToCoroutine();

            var panel = show2.panel;
            Assert.IsNotNull(panel);
            Assert.IsTrue(panel.IsState(Panel.StateEnum.Shown));
            Assert.GreaterOrEqual(recorder.showCount, 2);
            Assert.GreaterOrEqual(recorder.hideCount, 1);
        }

        [UnityTest]
        public IEnumerator MainUI_Switch_Queued_Sequence()
        {
            var a1 = _ui.Show<TestMainUIA>(false);
            var b = _ui.Show<TestMainUIB>(false);
            var a2 = _ui.Show<TestMainUIA>(false);

            yield return a2.Task.ToCoroutine();

            Assert.IsNotNull(_ui.CurrentMainUI);
            Assert.AreEqual(a2.panel, _ui.CurrentMainUI);
            Assert.IsTrue(_ui.FindUI(_ui.GetPath(typeof(TestMainUIB))) == null);
        }

        [UnityTest]
        public IEnumerator TryGetPanel_Returns_Null_When_Pending()
        {
            TestLoadableMainUI.Reset();
            var handler = new TestLoadingHandler();
            _ui.SetLoadingHandler(handler);

            var handle = _ui.Show<TestLoadableMainUI>(false);
            var pendingPanel = handle.TryGetPanel();
            Assert.IsNull(pendingPanel);

            yield return handle.Task.ToCoroutine();

            Assert.IsNotNull(handle.panel);
            Assert.AreEqual(1, TestLoadableMainUI.onLoadingCount);
        }

        [UnityTest]
        public IEnumerator Batch_Show_10_NoAnimation()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();

            UIOperationHandle last = null;
            for (int i = 0; i < 10; i++)
                last = _ui.Show<TestPanel>(false);

            yield return last.Task.ToCoroutine();

            Assert.IsNotNull(last.panel);
            Assert.IsTrue(last.panel.IsState(Panel.StateEnum.Shown));
            Assert.GreaterOrEqual(recorder.createdCount, 10);
            Assert.GreaterOrEqual(recorder.destroyCount, 9);
        }

        [UnityTest]
        public IEnumerator Show_InvalidPath_NoSideEffects()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();

            var resultHandle = _ui.Show("UI/NotExist", false);
            yield return resultHandle.Task.ToCoroutine();

            Assert.IsNull(resultHandle.panel);
            Assert.IsNull(_ui.CurrentMainUI);
            Assert.AreEqual(0, recorder.createdCount);
        }

        [UnityTest]
        public IEnumerator PersistenceUI_Not_Autoclosed_On_MainChange()
        {
            var mainAHandle = _ui.Show<TestMainUIA>(false);
            yield return mainAHandle.Task.ToCoroutine();
            var mainA = mainAHandle.panel;

            var persistentHandle = _ui.Show<TestPersistentPanel>(false);
            yield return persistentHandle.Task.ToCoroutine();
            var persistent = persistentHandle.panel;
            _ui.SetPersistenceUI(persistent);

            var mainBHandle = _ui.Show<TestMainUIB>(false);
            yield return mainBHandle.Task.ToCoroutine();
            var mainB = mainBHandle.panel;

            Assert.IsNotNull(persistent);
            Assert.IsTrue(persistent.IsState(Panel.StateEnum.Hidden));
            Assert.AreEqual(mainB, _ui.CurrentMainUI);
        }

        [UnityTest]
        public IEnumerator Lifecycle_OnShow_OnHide_Panel_And_View()
        {
            TestLifecyclePanel.Reset();
            TestLifecycleView.Reset();

            var showHandle = _ui.Show<TestLifecyclePanel>(false);
            yield return showHandle.Task.ToCoroutine();
            var panel = showHandle.panel;

            Assert.IsNotNull(panel);
            Assert.AreEqual(1, TestLifecyclePanel.onCreateCount);
            Assert.AreEqual(1, TestLifecyclePanel.onShowCount);
            Assert.AreEqual(1, TestLifecyclePanel.onShownCount);
            Assert.AreEqual(1, TestLifecycleView.onCreateCount);
            Assert.AreEqual(1, TestLifecycleView.onShowCount);
            Assert.AreEqual(1, TestLifecycleView.onShownCount);

            var hideHandle = _ui.Hide<TestLifecyclePanel>(false);
            yield return hideHandle.Task.ToCoroutine();

            Assert.AreEqual(1, TestLifecyclePanel.onHideCount);
            Assert.AreEqual(1, TestLifecyclePanel.onHiddenCount);
            Assert.AreEqual(1, TestLifecycleView.onHideCount);
            Assert.AreEqual(1, TestLifecycleView.onHiddenCount);
        }

        [UnityTest]
        public IEnumerator PopUI_Mask_InputScheme_Focus()
        {
            using var recorder = new UIEventRecorder();
            recorder.Start();

            var popHandle = _ui.Show<TestPopUI>(false);
            yield return popHandle.Task.ToCoroutine();
            var pop = popHandle.panel;
            Assert.IsNotNull(pop);
            Assert.IsTrue(pop.IsState(Panel.StateEnum.Shown));
            if (_ui.CurrentInputScheme != "UI")
            {
                _ui.SetInputScheme(pop, "UI");
            }
            Assert.AreEqual("UI", _ui.CurrentInputScheme);
            Assert.GreaterOrEqual(recorder.inputSchemeChangedCount, 1);
            if (recorder.maskChangedCount > 0)
            {
                Assert.IsTrue(recorder.lastMaskActive);
                Assert.AreEqual(pop, recorder.lastMaskTarget);
            }
            if (recorder.focusChangedCount > 0)
                Assert.GreaterOrEqual(recorder.focusChangedCount, 1);

            var hideHandle = _ui.Hide<TestPopUI>(false);
            if (_ui.CurrentInputScheme != "Game")
            {
                _ui.SetInputScheme(pop, null);
            }
            Assert.AreEqual("Game", _ui.CurrentInputScheme);
            yield return hideHandle.Task.ToCoroutine();
        }

        [UnityTest]
        public IEnumerator AutoClose_None_Does_Not_Close_On_MainSwitch()
        {
            var mainAHandle = _ui.Show<TestMainUIA>(false);
            yield return mainAHandle.Task.ToCoroutine();

            var noneHandle = _ui.Show<TestAutoCloseNonePanel>(false);
            yield return noneHandle.Task.ToCoroutine();
            var nonePanel = noneHandle.panel;

            var mainBHandle = _ui.Show<TestMainUIB>(false);
            yield return mainBHandle.Task.ToCoroutine();

            var stillThere = _ui.FindUI(_ui.GetPath(typeof(TestAutoCloseNonePanel)));
            Assert.IsNotNull(stillThere);
            Assert.IsTrue(stillThere.IsState(Panel.StateEnum.Shown));
            Assert.AreEqual(mainBHandle.panel, _ui.CurrentMainUI);
            Assert.AreEqual(nonePanel, stillThere);
        }

        [UnityTest]
        public IEnumerator InputScheme_Stack_Push_Pop()
        {
            var pop1Handle = _ui.Show<TestPopUI>(false);
            yield return pop1Handle.Task.ToCoroutine();
            if (_ui.CurrentInputScheme != "UI")
                _ui.SetInputScheme(pop1Handle.panel, "UI");
            Assert.AreEqual("UI", _ui.CurrentInputScheme);

            var pop2Handle = _ui.Show<TestPopUIAlt>(false);
            yield return pop2Handle.Task.ToCoroutine();
            if (_ui.CurrentInputScheme != "UI2")
                _ui.SetInputScheme(pop2Handle.panel, "UI2");
            Assert.AreEqual("UI2", _ui.CurrentInputScheme);

            var hide2 = _ui.Hide<TestPopUIAlt>(false);
            yield return hide2.Task.ToCoroutine();
            _ui.SetInputScheme(pop2Handle.panel, null);
            Assert.AreEqual("UI", _ui.CurrentInputScheme);

            var hide1 = _ui.Hide<TestPopUI>(false);
            yield return hide1.Task.ToCoroutine();
            _ui.SetInputScheme(pop1Handle.panel, null);
            Assert.AreEqual("Game", _ui.CurrentInputScheme);
        }

        [UnityTest]
        public IEnumerator DestroyAll_Preserves_Persistent_When_NotForced()
        {
            var handle = _ui.Show<TestPersistentPanel>(false);
            yield return handle.Task.ToCoroutine();
            var panel = handle.panel;
            _ui.SetPersistenceUI(panel);

            var destroyHandle = _ui.DestroyAll(false);
            if (destroyHandle != null)
                yield return destroyHandle.Task.ToCoroutine();

            var stillThere = _ui.FindUI(_ui.GetPath(typeof(TestPersistentPanel)));
            Assert.IsNotNull(stillThere);
        }

        [UnityTest]
        public IEnumerator DestroyAll_Force_Removes_Persistent()
        {
            var handle = _ui.Show<TestPersistentPanel>(false);
            yield return handle.Task.ToCoroutine();
            var panel = handle.panel;
            _ui.SetPersistenceUI(panel);

            var destroyHandle = _ui.DestroyAll(true);
            if (destroyHandle != null)
                yield return destroyHandle.Task.ToCoroutine();

            Assert.IsNull(_ui.FindUI(_ui.GetPath(typeof(TestPersistentPanel))));
        }

        [UnityTest]
        public IEnumerator Reentrant_Show_Enqueues_Immediate()
        {
            TestReentrantPanel.Reset();

            var handle = _ui.Show<TestReentrantPanel>(false);
            yield return handle.Task.ToCoroutine();

            Assert.IsNotNull(TestReentrantPanel.lastHandle);
            yield return TestReentrantPanel.lastHandle.Task.ToCoroutine();

            Assert.IsNotNull(TestReentrantPanel.lastHandle.panel);
            Assert.IsTrue(TestReentrantPanel.lastHandle.panel.IsState(Panel.StateEnum.Shown));
        }

        [UnityTest]
        public IEnumerator Show_Assigns_UserData()
        {
            var data = new object();
            var handle = _ui.Show<TestPanel>(data, false);
            yield return handle.Task.ToCoroutine();

            Assert.IsNotNull(handle.panel);
            Assert.AreSame(data, handle.panel.userData);
        }

        [UnityTest]
        public IEnumerator Toggle_ShowOrHide_Transitions()
        {
            var showHandle = _ui.Show<TestPanel>(false);
            yield return showHandle.Task.ToCoroutine();
            var panel = showHandle.panel;

            var hideHandle = _ui.ShowOrHide<TestPanel>(false);
            yield return hideHandle.Task.ToCoroutine();
            Assert.IsTrue(panel.IsState(Panel.StateEnum.Hidden));

            var showAgainHandle = _ui.ShowOrHide<TestPanel>(false);
            yield return showAgainHandle.Task.ToCoroutine();
            Assert.IsTrue(panel.IsState(Panel.StateEnum.Shown));
        }

        [UnityTest]
        public IEnumerator Lifecycle_OnClose_OnClosed_Called_Once()
        {
            TestLifecyclePanel.Reset();

            var showHandle = _ui.Show<TestLifecyclePanel>(false);
            yield return showHandle.Task.ToCoroutine();

            var closeHandle = _ui.Close<TestLifecyclePanel>(false);
            yield return closeHandle.Task.ToCoroutine();

            Assert.AreEqual(1, TestLifecyclePanel.onCloseCount);
            Assert.AreEqual(1, TestLifecyclePanel.onClosedCount);
        }

        [UnityTest]
        public IEnumerator Hide_InvalidPath_NoSideEffects()
        {
            var handle = _ui.Hide("UI/NotExist", false);
            yield return handle.Task.ToCoroutine();
            Assert.IsNull(handle.panel);
        }

        [UnityTest]
        public IEnumerator MaskClick_AutoClose_Hides_PopUI()
        {
            var popHandle = _ui.Show<TestMaskClickPopUI>(false);
            yield return popHandle.Task.ToCoroutine();
            var pop = popHandle.panel as TestMaskClickPopUI;
            Assert.IsNotNull(pop);

            pop.OnMaskClick();
            yield return new WaitUntil(() => pop.IsState(Panel.StateEnum.Hidden));
        }

        [UnityTest]
        public IEnumerator Animated_Show_Hide_Uses_Animation_Path()
        {
            TestAnimatedPanel.Reset();

            var showHandle = _ui.Show<TestAnimatedPanel>(true);
            yield return showHandle.Task.ToCoroutine();
            Assert.GreaterOrEqual(TestAnimatedPanel.showAnimCount, 1);

            var hideHandle = _ui.Hide<TestAnimatedPanel>(true);
            yield return hideHandle.Task.ToCoroutine();
            Assert.GreaterOrEqual(TestAnimatedPanel.hideAnimCount, 1);
        }

        [UnityTest]
        public IEnumerator Close_InvalidPath_NoSideEffects()
        {
            var handle = _ui.Close("UI/NotExist", false);
            yield return handle.Task.ToCoroutine();
            Assert.IsNull(handle.panel);
        }

        [UnityTest]
        public IEnumerator Destroy_InvalidPath_NoSideEffects()
        {
            var handle = _ui.Destroy("UI/NotExist");
            yield return handle.Task.ToCoroutine();
            Assert.IsNull(handle.panel);
        }

        [UnityTest]
        public IEnumerator Destroy_Queued_Removes_NonPersistent()
        {
            var showHandle = _ui.Show<TestPanel>(false);
            yield return showHandle.Task.ToCoroutine();

            var destroyHandle = _ui.Destroy<TestPanel>();
            yield return destroyHandle.Task.ToCoroutine();

            Assert.IsNull(_ui.FindUI(_ui.GetPath(typeof(TestPanel))));
        }

        [UnityTest]
        public IEnumerator Destroy_Queued_Preserves_Persistent()
        {
            var showHandle = _ui.Show<TestPersistentPanel>(false);
            yield return showHandle.Task.ToCoroutine();
            var panel = showHandle.panel;
            _ui.SetPersistenceUI(panel);

            var destroyHandle = _ui.Destroy<TestPersistentPanel>();
            yield return destroyHandle.Task.ToCoroutine();

            var stillThere = _ui.FindUI(_ui.GetPath(typeof(TestPersistentPanel)));
            Assert.IsNotNull(stillThere);
        }

        [UnityTest]
        public IEnumerator Destroy_Queued_Force_Removes_Persistent()
        {
            var showHandle = _ui.Show<TestPersistentPanel>(false);
            yield return showHandle.Task.ToCoroutine();
            var panel = showHandle.panel;
            _ui.SetPersistenceUI(panel);

            var destroyHandle = _ui.Destroy(panel, true);
            yield return destroyHandle.Task.ToCoroutine();

            Assert.IsNull(_ui.FindUI(_ui.GetPath(typeof(TestPersistentPanel))));
        }

        [UnityTest]
        public IEnumerator Queue_Serializes_Animated_Show_Then_Hide()
        {
            TestSequencePanel.Reset();

            _ui.Show<TestSequencePanel>(true);
            var hideHandle = _ui.Hide<TestSequencePanel>(true);
            yield return hideHandle.Task.ToCoroutine();

            Assert.IsTrue(TestSequencePanel.showStarted);
            Assert.IsTrue(TestSequencePanel.showCompleted);
            Assert.IsTrue(TestSequencePanel.hideStarted);
            Assert.IsTrue(TestSequencePanel.hideCompleted);
            Assert.IsTrue(TestSequencePanel.hideStartedAfterShow);
        }

        [UnityTest]
        public IEnumerator Queue_Serializes_Show_Then_Destroy()
        {
            TestSequencePanel.Reset();

            _ui.Show<TestSequencePanel>(true);
            var destroyHandle = _ui.Destroy<TestSequencePanel>();
            yield return destroyHandle.Task.ToCoroutine();

            Assert.IsTrue(TestSequencePanel.showCompleted);
            Assert.IsNull(_ui.FindUI(_ui.GetPath(typeof(TestSequencePanel))));
        }

        [UnityTest]
        public IEnumerator DestroyAll_Queued_Between_Operations()
        {
            _ui.Show<TestPanel>(false);
            _ui.Show<TestAutoCloseNonePanel>(false);
            _ui.DestroyAll(false);
            var lastShow = _ui.Show<TestPanel>(false);

            yield return lastShow.Task.ToCoroutine();

            Assert.IsNotNull(lastShow.panel);
            Assert.IsNull(_ui.FindUI(_ui.GetPath(typeof(TestAutoCloseNonePanel))));
        }

        [UnityTest]
        public IEnumerator InputScheme_Reset_On_Destroy()
        {
            var popHandle = _ui.Show<TestPopUI>(false);
            yield return popHandle.Task.ToCoroutine();
            if (_ui.CurrentInputScheme != "UI")
                _ui.SetInputScheme(popHandle.panel, "UI");
            Assert.AreEqual("UI", _ui.CurrentInputScheme);

            var destroyHandle = _ui.Destroy(popHandle.panel);
            yield return destroyHandle.Task.ToCoroutine();

            Assert.AreEqual("Game", _ui.CurrentInputScheme);
        }
    }
}
