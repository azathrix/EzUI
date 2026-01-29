using System.Collections.Generic;
using Azathrix.EzUI.Interfaces;
using Azathrix.Framework.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Azathrix.EzUI.Core
{
    // 渲染/层级/遮罩相关逻辑
    public partial class UISystem
    {
        private Transform GetLayer(int layer)
        {
            if (_layers.TryGetValue(layer, out var p))
                return p;

            return CreateLayer(layer);
        }

        private Transform CreateLayer(int layer)
        {
#if UNITY_EDITOR
            if (!AzathrixFramework.IsApplicationStarted) return null;
#endif
            if (_uiRoot == null) return null;

            GameObject go = new GameObject("Layer - " + layer, typeof(Canvas), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            go.layer = 5;
            RectTransform trans = (RectTransform) go.transform;
            trans.SetParent(_uiRoot);
            trans.localScale = Vector3.one;
            trans.localPosition = Vector3.zero;
            trans.anchorMax = Vector2.one;
            trans.anchorMin = Vector2.zero;
            trans.offsetMax = Vector2.zero;
            trans.offsetMin = Vector2.zero;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = layer;
            canvas.worldCamera = _uiCamera;
            _layers.Add(layer, trans);
            return trans;
        }

        private void SortUI()
        {
            _instanceUIs.Sort((x, y) =>
            {
                if (x.IsState(Panel.StateEnum.Shown) &&
                    !y.IsState(Panel.StateEnum.Shown))
                    return -1;

                if (y.IsState(Panel.StateEnum.Shown) &&
                    !x.IsState(Panel.StateEnum.Shown))
                    return 1;

                if (y.layer != x.layer)
                    return y.layer.CompareTo(x.layer);

                return y.transform.GetSiblingIndex().CompareTo(x.transform.GetSiblingIndex());
            });
        }

        private void OnMaskClick()
        {
            foreach (var ui in _instanceUIs)
            {
                if (ui.useMask && ui.IsState(Panel.StateEnum.Shown))
                {
                    if (ui is IMaskClickable mask)
                        mask.OnMaskClick();
                    break;
                }
            }
        }

        private void InitMask()
        {
            if (_uiRoot == null) return;

            GameObject go = new GameObject("Mask", typeof(Image), typeof(Button));
            go.layer = 5;
            RectTransform rt = (RectTransform) go.transform;
            ConfigureMaskRect(rt, _uiRoot);

            var img = go.GetComponent<Image>();
            var btn = go.GetComponent<Button>();

            var maskColor = Settings?.maskColor ?? new Color(0f, 0f, 0f, 0.95f);
            img.color = maskColor;

            var clickable = Settings?.maskClickable ?? true;
            if (clickable)
            {
                btn.onClick.AddListener(OnMaskClick);
            }
            else
            {
                btn.enabled = false;
            }

            btn.navigation = new Navigation() {mode = Navigation.Mode.None};
            btn.transition = Selectable.Transition.None;
            _mask = rt;
            go.SetActive(false);
        }

        private static void ConfigureMaskRect(RectTransform rt, Transform parent)
        {
            rt.SetParent(parent, false);
            rt.anchorMax = Vector2.one;
            rt.anchorMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.anchoredPosition3D = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
    }
}
