using Azathrix.EzUI.Animations;
using Azathrix.EzUI.Core;
using Azathrix.GameKit.Editor;
using UnityEditor;

namespace Azathrix.EzUI.Editor
{
    [CustomEditor(typeof(Panel), true)]
    public class PanelEditor : GameScriptEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            serializedObject.Update();

            var animationProp = serializedObject.FindProperty("_animation");
            var panel = target as Panel;
            if (panel == null)
            {
                serializedObject.ApplyModifiedProperties();
                return; 
            }

            var current = animationProp.objectReferenceValue as UIAnimationComponent;
            if (current == null)
            {
                current = panel.GetComponent<UIAnimationComponent>();
                if (current != null)
                    animationProp.objectReferenceValue = current;
            }

            var types = UIAnimationEditorUtility.GetAnimationTypes();
            var names = UIAnimationEditorUtility.BuildTypeDisplayNames(types);
            var currentIndex =
                UIAnimationEditorUtility.FindTypeIndex(types, current != null ? current.GetType() : null);

            var newIndex = EditorGUILayout.Popup("动画组件", currentIndex, names);
            if (newIndex != currentIndex)
            {
                if (newIndex == 0)
                {
                    if (current != null)
                    {
                        Undo.DestroyObjectImmediate(current);
                    }

                    animationProp.objectReferenceValue = null;
                }
                else
                {
                    var newType = types[newIndex - 1];
                    if (current == null || current.GetType() != newType)
                    {
                        if (current != null)
                            Undo.DestroyObjectImmediate(current);

                        var added = Undo.AddComponent(panel.gameObject, newType) as UIAnimationComponent;
                        animationProp.objectReferenceValue = added;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}