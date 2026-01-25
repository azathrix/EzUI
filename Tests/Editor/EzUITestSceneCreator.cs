#if UNITY_EDITOR
using Azathrix.EzUI.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Azathrix.EzUI.Tests.Editor
{
    public static class EzUITestSceneCreator
    {
        private const string ScenePath = "Assets/Scenes/EzUI_TestScene.unity";

        [MenuItem("Azathrix/EzUI/Create Test Scene")]
        public static void CreateTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("EzUI_TestBootstrap");
            go.AddComponent<EzUITestBootstrap>();
            go.AddComponent<EzUIDebugPanel>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorUtility.DisplayDialog("EzUI", $"Test scene created: {ScenePath}", "OK");
        }
    }
}
#endif
