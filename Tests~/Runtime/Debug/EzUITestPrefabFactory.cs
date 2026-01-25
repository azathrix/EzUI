#if UNITY_EDITOR
using Azathrix.EzUI.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Azathrix.EzUI.DebugTools
{
    public static class EzUITestPrefabFactory
    {
        public static GameObject CreatePanelPrefab<T>(string name, Color color) where T : Panel
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5;

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 360);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            var img = go.GetComponent<Image>();
            img.color = color;

            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(go.transform, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = label.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.text = name;
            text.color = Color.white;
            text.fontSize = 36;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            go.AddComponent<T>();
            return go;
        }
    }
}
#endif
