using Azathrix.EzUI;
using UnityEditor;
using UnityEngine;

namespace Azathrix.EzUI.Editor
{
    [CustomEditor(typeof(EzUISettings))]
    public class EzUISettingsEditor : UnityEditor.Editor
    {
        // 初始化设置
        private SerializedProperty _initializeMode;

        // 自动初始化 - 预设模式
        private SerializedProperty _uiRootPath;

        // 自动初始化 - 自动创建模式
        private SerializedProperty _autoCreateUIRoot;
        private SerializedProperty _autoCreatedUIRootName;
        private SerializedProperty _autoCreateEventSystem;
        private SerializedProperty _autoCreateUICamera;
        private SerializedProperty _uiCameraTag;

        // 路径设置
        private SerializedProperty _panelPathFormat;

        // 输入方案设置
        private SerializedProperty _defaultGameInputScheme;
        private SerializedProperty _defaultPopUIInputScheme;

        // 遮罩设置
        private SerializedProperty _maskColor;
        private SerializedProperty _maskClickable;

        // 动画设置
        private SerializedProperty _blockInputDuringAnimation;

        private bool _initFoldout = true;
        private bool _pathFoldout = true;
        private bool _inputFoldout = true;
        private bool _maskFoldout = true;
        private bool _animationFoldout = true;

        private void OnEnable()
        {
            _initializeMode = serializedObject.FindProperty("initializeMode");
            _uiRootPath = serializedObject.FindProperty("uiRootPath");
            _autoCreateUIRoot = serializedObject.FindProperty("autoCreateUIRoot");
            _autoCreatedUIRootName = serializedObject.FindProperty("autoCreatedUIRootName");
            _autoCreateEventSystem = serializedObject.FindProperty("autoCreateEventSystem");
            _autoCreateUICamera = serializedObject.FindProperty("autoCreateUICamera");
            _uiCameraTag = serializedObject.FindProperty("uiCameraTag");
            _panelPathFormat = serializedObject.FindProperty("panelPathFormat");
            _defaultGameInputScheme = serializedObject.FindProperty("defaultGameInputScheme");
            _defaultPopUIInputScheme = serializedObject.FindProperty("defaultPopUIInputScheme");
            _maskColor = serializedObject.FindProperty("maskColor");
            _maskClickable = serializedObject.FindProperty("maskClickable");
            _blockInputDuringAnimation = serializedObject.FindProperty("blockInputDuringAnimation");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawInitializationSection();
            EditorGUILayout.Space(10);
            DrawPathSection();
            EditorGUILayout.Space(10);
            DrawInputSchemeSection();
            EditorGUILayout.Space(10);
            DrawMaskSection();
            EditorGUILayout.Space(10);
            DrawAnimationSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInitializationSection()
        {
            _initFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_initFoldout, "初始化设置");
            if (_initFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_initializeMode, new GUIContent("初始化模式"));

                var mode = (EzUISettings.InitializeMode)_initializeMode.enumValueIndex;

                if (mode == EzUISettings.InitializeMode.Auto)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("预设模式", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_uiRootPath, new GUIContent("UIRoot 预设路径"));

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("自动创建模式", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_autoCreateUIRoot, new GUIContent("自动创建 UIRoot"));

                    if (_autoCreateUIRoot.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(_autoCreatedUIRootName, new GUIContent("UIRoot 名称"));
                        EditorGUILayout.PropertyField(_uiCameraTag, new GUIContent("UICamera Tag", "用于查找场景中已存在的 UICamera"));
                        EditorGUILayout.PropertyField(_autoCreateUICamera, new GUIContent("自动创建 UI 摄像机", "如果通过 Tag 找不到则自动创建"));
                        EditorGUILayout.PropertyField(_autoCreateEventSystem, new GUIContent("自动创建 EventSystem", "如果场景中不存在则自动创建"));
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "手动模式下，需要在代码中调用以下方法进行初始化：\n" +
                        "• UISystem.SetUIRoot(Transform root)\n" +
                        "• UISystem.SetUICamera(Camera camera)\n" +
                        "• UISystem.SetEventSystem(EventSystem eventSystem)",
                        MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawPathSection()
        {
            _pathFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_pathFoldout, "路径设置");
            if (_pathFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_panelPathFormat, new GUIContent("Panel 路径格式", "使用 {0} 作为 Panel 类名占位符"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawInputSchemeSection()
        {
            _inputFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_inputFoldout, "输入方案设置");
            if (_inputFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_defaultGameInputScheme, new GUIContent("默认游戏输入方案"));
                EditorGUILayout.PropertyField(_defaultPopUIInputScheme, new GUIContent("默认弹窗输入方案"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawMaskSection()
        {
            _maskFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_maskFoldout, "遮罩设置");
            if (_maskFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_maskColor, new GUIContent("遮罩颜色"));
                EditorGUILayout.PropertyField(_maskClickable, new GUIContent("遮罩可点击"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawAnimationSection()
        {
            _animationFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_animationFoldout, "动画设置");
            if (_animationFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_blockInputDuringAnimation, new GUIContent("动画期间屏蔽输入"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
