#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace EZRoomGen.Generation.Editor
{
    public class DungeonLayoutGeneratorEditor : IGeneratorEditor<DungeonLayoutGeneratorSettings>
    {
        public bool DrawInspector(DungeonLayoutGeneratorSettings settings)
        {
            EditorGUI.BeginChangeCheck();

            settings.useRecursiveBacktracker = EditorGUILayout.Toggle(
                new GUIContent("Recursive Backtracker", "Use recursive backtracker instead of cellular automata"),
                settings.useRecursiveBacktracker);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);

            settings.seed = EditorGUILayout.IntField(
                new GUIContent("Seed", "Random seed"),
                settings.seed);

            if (!settings.useRecursiveBacktracker)
            {
                settings.density = EditorGUILayout.Slider(
                    new GUIContent("Density", "Initial wall density (0-1)"),
                    settings.density, 0f, 1f);

                settings.iterations = EditorGUILayout.IntSlider(
                    new GUIContent("Iterations", "Cellular automata iterations"),
                    settings.iterations, 1, 10);

                settings.pathWidth = EditorGUILayout.IntSlider(
                    new GUIContent("Path Width", "Width of corridors"),
                    settings.pathWidth, 1, 5);
            }
            else
            {
                settings.pathWidth = 1;
            }

            settings.deadEndKeepChance = EditorGUILayout.Slider(
                new GUIContent("Keep Chance", "Percentage of dead ends to keep"),
                settings.deadEndKeepChance, 0f, 1f);

            settings.loopCount = EditorGUILayout.IntSlider(
                new GUIContent("Loop Count", "Number of extra connections"),
                settings.loopCount, 0, 20);

            settings.smoothEdges = EditorGUILayout.Toggle(
                new GUIContent("Smooth Edges", "Round off sharp corners"),
                settings.smoothEdges);

            bool changed = EditorGUI.EndChangeCheck();

            return changed;
        }
    }
}

#endif