#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace EZRoomGen.Generation.Editor
{
    public interface IGeneratorEditor<T>
    {
        bool DrawInspector(T settings);
    }

    public abstract class BaseLayoutGeneratorEditor<T> : IGeneratorEditor<T> where T : LayoutGeneratorSettings
    {
        public abstract bool DrawInspector(T settings);

        protected void DrawBaseFields(T settings)
        {
            settings.seed = EditorGUILayout.IntField("Seed", settings.seed);
            settings.height = EditorGUILayout.Slider(new GUIContent("Default Height", "Default height of generated layout's cells"), settings.height, 1f, 10f);
        }
    }
}

#endif