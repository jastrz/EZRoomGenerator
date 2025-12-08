#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace EZRoomGen.Core.Editor
{
    public class RoomGridEditorWindow : EditorWindow
    {
        private RoomGenerator generator;
        private Vector2 scrollPos;
        private RoomGridDrawer gridDrawer = new RoomGridDrawer();

        public static void OpenWindow(RoomGenerator generator)
        {
            var window = GetWindow<RoomGridEditorWindow>("Room Grid Editor");
            window.generator = generator;
            window.Show();
        }

        private void OnGUI()
        {
            if (generator == null)
            {
                EditorGUILayout.HelpBox("No Room Generator selected. Please select a Room Generator in the scene.", MessageType.Warning);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            gridDrawer.Draw(generator);

            EditorGUILayout.EndScrollView();
        }

        private void OnFocus()
        {
            Repaint();
        }
    }
}

#endif