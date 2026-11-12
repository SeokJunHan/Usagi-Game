using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UsagiGame.Editor
{
    [CustomEditor(typeof(CameraBackground))]
    public sealed class CameraBackgroundEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (!GUILayout.Button("배경 기준 보드 즉시 맞춤", GUILayout.Height(30f))) return;

            CameraBackground background = (CameraBackground)target;
            background.Refresh();

            BoardLayout[] layouts = FindObjectsByType<BoardLayout>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < layouts.Length; i++) layouts[i].Refresh(true);

            EditorUtility.SetDirty(background);
            if (background.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(background.gameObject.scene);
            SceneView.RepaintAll();
        }
    }
}
