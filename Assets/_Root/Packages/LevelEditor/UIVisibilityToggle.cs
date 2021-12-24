#if UNITY_EDITOR
// ReSharper disable AccessToStaticMemberViaDerivedType
namespace Snorlax.Editor
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Automatically manages visibility of UI and 3D objects based on whether the editor is in 2D or 3D mode.
    /// </summary>
    [InitializeOnLoad]
    internal class UIVisibilityToggle
    {
        public static bool sLastModeWas2D;

        static UIVisibilityToggle()
        {
            if (SceneView.lastActiveSceneView != null)
                sLastModeWas2D = SceneView.lastActiveSceneView.in2DMode;

            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChange;
        }

        private static void Update()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                return;

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (sceneView.in2DMode && !sLastModeWas2D)
                    SetUIVisibility(true, EditorSceneManager.GetActiveScene());
                else if (!sceneView.in2DMode && sLastModeWas2D)
                    SetUIVisibility(false, EditorSceneManager.GetActiveScene());
            }

            sLastModeWas2D = sceneView.in2DMode;
        }

        private static void SetUIVisibility(bool uiVisbility, Scene scene)
        {
            if (scene == null) return;

            var objects = scene.GetRootGameObjects();
            foreach (var obj in objects)
            {
                var canvas = obj.GetComponent<Canvas>();
                bool isUI = canvas != null && canvas.renderMode != RenderMode.WorldSpace;
                obj.SetActive(isUI ? uiVisbility : !uiVisbility);
            }
        }

        private static void ShowAllObjects(Scene scene)
        {
            if (scene == null) return;

            // Restore all top-level objects
            var objects = scene.GetRootGameObjects();
            foreach (var obj in objects)
            {
                obj.SetActive(true);
            }
        }

        private static void OnPlayModeChange(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                ShowAllObjects(EditorSceneManager.GetActiveScene());
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SetUIVisibility(sLastModeWas2D, EditorSceneManager.GetActiveScene());
            }
        }
    }
}
#endif