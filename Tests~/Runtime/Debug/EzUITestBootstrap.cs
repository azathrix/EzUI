#if UNITY_EDITOR
using Azathrix.EzUI.Core;
using Azathrix.Framework.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azathrix.EzUI.DebugTools
{
    [DefaultExecutionOrder(-1000)]
    public class EzUITestBootstrap : MonoBehaviour
    {
        [Header("Setup")]
        public bool useTestResourcesLoader = true;
        public bool registerUISystemIfMissing = true;

        [Header("Auto Show")]
        public bool autoShowMainUI = true;
        public bool autoShowPopUI;

        private UISystem _uiSystem;

        private async void Start()
        {
            SetupSettings();

            if (useTestResourcesLoader)
                SetupResourcesLoader();

            EnsureFrameworkManager();

            if (registerUISystemIfMissing)
            {
                var manager = AzathrixFramework.EffectiveRuntimeManager;
                if (manager != null && !manager.HasSystem<UISystem>())
                    await manager.RegisterSystemAsync(typeof(UISystem));

                _uiSystem = manager?.GetSystem<UISystem>();
            }

            if (_uiSystem != null && autoShowMainUI)
                _uiSystem.ShowMainUI<EzUITestMainUIA>(false);

            if (_uiSystem != null && autoShowPopUI)
                _uiSystem.Show<EzUITestPopUI>(false);
        }

        private static void SetupSettings()
        {
            var settings = ScriptableObject.CreateInstance<EzUISettings>();
            settings.autoCreateUIRoot = true;
            settings.autoCreateEventSystem = true;
            settings.autoCreateUICamera = true;
            settings.inputSchemeSwitchMode = EzUISettings.InputSchemeSwitchMode.EventOnly;
            settings.defaultPopUIInputScheme = "UI";
            settings.defaultGameInputScheme = "Game";
            EzUISettings.SetSettings(settings);
        }

        private static void SetupResourcesLoader()
        {
            var loader = new EzUITestResourcesLoader();
            loader.Add("UI/EzUITestPanel",
                EzUITestPrefabFactory.CreatePanelPrefab<EzUITestPanel>("EzUI Test Panel",
                    new Color(0.2f, 0.6f, 0.9f, 0.9f)));
            loader.Add("UI/EzUITestMainUIA",
                EzUITestPrefabFactory.CreatePanelPrefab<EzUITestMainUIA>("EzUI MainUI A",
                    new Color(0.2f, 0.8f, 0.4f, 0.9f)));
            loader.Add("UI/EzUITestMainUIB",
                EzUITestPrefabFactory.CreatePanelPrefab<EzUITestMainUIB>("EzUI MainUI B",
                    new Color(0.8f, 0.4f, 0.2f, 0.9f)));
            loader.Add("UI/EzUITestPopUI",
                EzUITestPrefabFactory.CreatePanelPrefab<EzUITestPopUI>("EzUI PopUI",
                    new Color(0.7f, 0.2f, 0.9f, 0.9f)));

            AzathrixFramework.ResourcesLoader = loader;
        }

        private static void EnsureFrameworkManager()
        {
            if (AzathrixFramework.EffectiveRuntimeManager == null)
            {
                var manager = new SystemRuntimeManager { IsEditorMode = true };
                AzathrixFramework.SetEditorRuntimeManager(manager);
            }

            AzathrixFramework.MarkEditorStarted();
        }
    }
}
#endif
