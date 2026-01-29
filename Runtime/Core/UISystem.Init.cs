using Azathrix.EzUI.Events;
using Azathrix.Framework.Core;
using Azathrix.Framework.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Object = UnityEngine.Object;

namespace Azathrix.EzUI.Core
{
    // 初始化与运行时根节点创建逻辑
    public partial class UISystem
    {
        /// <summary>
        /// 检查是否完成初始化
        /// </summary>
        private void CheckInitialized()
        {
            if (IsInitialized) return;

            // 手动模式下，只需要 UIRoot 即可认为初始化完成
            if (_uiRoot != null)
            {
                IsInitialized = true;
                Dispatch(new UIRootCreated
                {
                    root = _uiRoot,
                    uiCamera = _uiCamera
                });
            }
        }

        /// <summary>
        /// 从预设初始化 UI 系统
        /// </summary>
        private void InitFromPrefab()
        {
            var prefab = AzathrixFramework.ResourcesLoader.Load<GameObject>(DefaultUIRootPath);
            if (prefab == null)
            {
                if (Settings?.autoCreateUIRoot ?? false)
                {
                    CreateDefaultUIRoot();
                    return;
                }

                Log.Error("UIRoot 预设未找到: " + DefaultUIRootPath);
                return;
            }

            var rootGo = Object.Instantiate(prefab);
            rootGo.name = Settings?.autoCreatedUIRootName ?? "[UIRoot]";
            Object.DontDestroyOnLoad(rootGo);
            _uiRoot = rootGo.transform;
            _uiCamera = rootGo.GetComponentInChildren<Camera>();

            // URP: 将 UI Camera 添加到主摄像机堆叠
            SetupURPCameraStack();
            UpdateLayerCameras();

            Dispatch(new UIRootCreated
            {
                root = _uiRoot,
                uiCamera = _uiCamera
            });
        }

        private void CreateDefaultUIRoot()
        {
            var rootName = Settings?.autoCreatedUIRootName ?? "[UIRoot]";
            var rootGo = new GameObject(rootName);
            Object.DontDestroyOnLoad(rootGo);
            _uiRoot = rootGo.transform;

            // 尝试通过 Tag 查找已存在的 UICamera
            var uiCameraTag = Settings?.uiCameraTag ?? "UICamera";
            if (!string.IsNullOrWhiteSpace(uiCameraTag))
            {
                try
                {
                    var existingCamGo = GameObject.FindWithTag(uiCameraTag);
                    if (existingCamGo != null)
                    {
                        _uiCamera = existingCamGo.GetComponent<Camera>();
                        if (_uiCamera != null)
                        {
                            SetupURPCameraStack();
                        }
                    }
                }
                catch
                {
                    // Tag 不存在时会抛出异常，忽略
                }
            }

            // 如果没有找到已存在的 UICamera，则创建新的
            if (_uiCamera == null && (Settings?.autoCreateUICamera ?? true))
            {
                var camGo = new GameObject("UICamera");
                camGo.transform.SetParent(_uiRoot);
                camGo.transform.localPosition = Vector3.zero;
                camGo.transform.localRotation = Quaternion.identity;
                camGo.transform.localScale = Vector3.one;
                camGo.layer = 5;

                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Depth;
                cam.orthographic = true;
                cam.cullingMask = 1 << 5;
                _uiCamera = cam;
                SetupURPCameraStack();
            }

            // 检查是否已存在 EventSystem
            _eventSystem = Object.FindObjectOfType<EventSystem>();
            if (_eventSystem == null && (Settings?.autoCreateEventSystem ?? true))
            {
                var esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(_uiRoot);
                _eventSystem = esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGo.AddComponent<InputSystemUIInputModule>();
#else
                Log.Warning("[EzUI] 新输入系统未启用，EventSystem 未添加输入模块");
#endif
            }

            UpdateLayerCameras();

            Dispatch(new UIRootCreated
            {
                root = _uiRoot,
                uiCamera = _uiCamera
            });
        }

        /// <summary>
        /// 设置 URP 摄像机堆叠
        /// </summary>
        private void SetupURPCameraStack()
        {
            if (_uiCamera == null) return;

            var mainCamera = Camera.main;
            if (mainCamera == null) return;

            var uiCameraData = _uiCamera.GetUniversalAdditionalCameraData();
            if (uiCameraData != null)
                uiCameraData.renderType = CameraRenderType.Overlay;

            var mainCameraData = mainCamera.GetUniversalAdditionalCameraData();
            if (mainCameraData != null && !mainCameraData.cameraStack.Contains(_uiCamera))
                mainCameraData.cameraStack.Add(_uiCamera);
        }

        private void UpdateLayerCameras()
        {
            if (_layers.Count == 0)
                return;

            foreach (var layer in _layers.Values)
            {
                if (layer == null)
                    continue;
                var canvas = layer.GetComponent<Canvas>();
                if (canvas != null)
                    canvas.worldCamera = _uiCamera;
            }
        }
    }
}
