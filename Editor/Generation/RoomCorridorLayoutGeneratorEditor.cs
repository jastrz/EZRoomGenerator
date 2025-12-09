#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace EZRoomGen.Generation.Editor
{
    public interface IGeneratorEditor<T>
    {
        bool DrawInspector(T settings);
    }

    /// <summary>
    /// Editor utility class used to display Room Corridor layout generator settings.
    /// </summary>
    public class RoomCorridorLayoutGeneratorEditor : IGeneratorEditor<RoomCorridorLayoutGeneratorSettings>
    {
        public bool DrawInspector(RoomCorridorLayoutGeneratorSettings settings)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();

            settings.seed = EditorGUILayout.IntField("Seed", settings.seed);
            settings.maxRooms = EditorGUILayout.IntSlider("Max Rooms", settings.maxRooms, 1, 40);
            settings.minRoomSize = EditorGUILayout.IntSlider("Min Room Size", settings.minRoomSize, 1, 10);

            settings.maxRoomSize = EditorGUILayout.IntSlider("Max Room Size", settings.maxRoomSize, 1, 40);
            settings.maxRoomSize = Mathf.Clamp(settings.maxRoomSize, settings.minRoomSize + 1, settings.maxRoomSize);

            bool changed = EditorGUI.EndChangeCheck();

            return changed;
        }
    }
}

#endif