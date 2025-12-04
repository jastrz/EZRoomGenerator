#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace EZRoomGen
{
    [CustomEditor(typeof(RoomGenerator))]
    public class RoomGeneratorEditor : Editor
    {
        private const float cellSize = 30f;
        private const float gridPadding = 10f;
        private bool isDragging = false;
        private Vector2 scrollPos;

        private SerializedProperty gridWidthProp;
        private SerializedProperty gridHeightProp;
        private SerializedProperty defaultHeightProp;
        private SerializedProperty meshResolutionProp;
        private SerializedProperty uvScaleProp;
        private SerializedProperty cellWindingProp;
        private SerializedProperty realtimeGenerationProp;
        private SerializedProperty wallMaterialProp;
        private SerializedProperty floorMaterialProp;
        private SerializedProperty roofMaterialProp;
        private SerializedProperty automaticallyAddLightsProp;
        private SerializedProperty lampPrefabProp;
        private SerializedProperty roomSpacingProp;
        private SerializedProperty corridorSpacingProp;
        // private SerializedProperty generateOnStartProp;
        private SerializedProperty addCollidersProp;
        private SerializedProperty invertRoofProp;

        private void OnEnable()
        {
            gridWidthProp = serializedObject.FindProperty("gridWidth");
            gridHeightProp = serializedObject.FindProperty("gridHeight");
            defaultHeightProp = serializedObject.FindProperty("defaultHeight");
            meshResolutionProp = serializedObject.FindProperty("meshResolution");
            uvScaleProp = serializedObject.FindProperty("uvScale");
            cellWindingProp = serializedObject.FindProperty("cellWinding");
            realtimeGenerationProp = serializedObject.FindProperty("realtimeGeneration");
            wallMaterialProp = serializedObject.FindProperty("wallMaterial");
            floorMaterialProp = serializedObject.FindProperty("floorMaterial");
            roofMaterialProp = serializedObject.FindProperty("roofMaterial");
            automaticallyAddLightsProp = serializedObject.FindProperty("automaticallyAddLights");
            lampPrefabProp = serializedObject.FindProperty("lampPrefab");
            roomSpacingProp = serializedObject.FindProperty("roomSpacing");
            corridorSpacingProp = serializedObject.FindProperty("corridorSpacing");
            // generateOnStartProp = serializedObject.FindProperty("generateOnStart");
            addCollidersProp = serializedObject.FindProperty("addColliders");
            invertRoofProp = serializedObject.FindProperty("invertRoof");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RoomGenerator generator = (RoomGenerator)target;
            if (generator == null) return;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            ShowHeader();
            DrawGridSettings(generator);
            bool paramsChanged = DrawParameters();
            bool materialsChanged = DrawMaterials();
            bool lightingParamsChanged = DrawLightPlacement();

            EditorGUILayout.Space();
            HandleCellSelection(generator);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Room Layout (Click to paint, Right-click to select)", EditorStyles.boldLabel);
            DrawGrid(generator);

            EditorGUILayout.Space();
            DrawBottomMenu(generator);

            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();

            if (realtimeGenerationProp.boolValue && (paramsChanged || materialsChanged || lightingParamsChanged))
            {
                generator.GenerateRoom();
            }
        }

        private void ShowHeader()
        {
            EditorGUILayout.LabelField("Room Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        private void DrawGridSettings(RoomGenerator generator)
        {
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.IntSlider(gridWidthProp, Constants.MinRoomWidth, Constants.MaxRoomWidth, new GUIContent("Grid Width"));
            EditorGUILayout.IntSlider(gridHeightProp, Constants.MinRoomHeight, Constants.MaxRoomHeight, new GUIContent("Grid Height"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                generator.ResizeGrid(gridWidthProp.intValue, gridHeightProp.intValue);
                if (realtimeGenerationProp.boolValue)
                {
                    generator.GenerateRoom();
                }
            }

            EditorGUILayout.Space();
        }

        private bool DrawParameters()
        {
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(realtimeGenerationProp);
            // EditorGUILayout.PropertyField(generateOnStartProp);
            EditorGUILayout.PropertyField(addCollidersProp);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(defaultHeightProp, 1f, 10f, new GUIContent("Default Height"));
            EditorGUILayout.IntSlider(meshResolutionProp, 1, 12, new GUIContent("Mesh Resolution"));
            EditorGUILayout.Slider(uvScaleProp, 1f, 10f, new GUIContent("UV Scale"));
            EditorGUILayout.PropertyField(cellWindingProp);
            EditorGUILayout.PropertyField(invertRoofProp);
            bool changed = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            return changed;
        }

        private bool DrawMaterials()
        {
            // EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(wallMaterialProp);
            EditorGUILayout.PropertyField(floorMaterialProp);
            EditorGUILayout.PropertyField(roofMaterialProp);
            bool changed = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            return changed;
        }

        private bool DrawLightPlacement()
        {
            // EditorGUILayout.LabelField("Light Placement", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(automaticallyAddLightsProp);
            EditorGUILayout.PropertyField(lampPrefabProp);
            EditorGUILayout.Slider(roomSpacingProp, 1f, 10f, new GUIContent("Room Spacing"));
            EditorGUILayout.Slider(corridorSpacingProp, 1f, 10f, new GUIContent("Corridor Spacing"));

            bool changed = EditorGUI.EndChangeCheck();
            return changed;
        }

        private void DrawBottomMenu(RoomGenerator generator)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Grid"))
            {
                generator.ClearGrid();
                if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
            }
            EditorGUILayout.EndHorizontal();

            if (!realtimeGenerationProp.boolValue)
            {
                if (GUILayout.Button("Generate Room", GUILayout.Height(40)))
                {
                    generator.GenerateRoom();
                }
            }

            if (generator.GetRoomObject() != null)
            {
                if (GUILayout.Button("Export as FBX", GUILayout.Height(30)))
                {
                    string path = EditorUtility.SaveFilePanel("Export Room as FBX", "Assets", "GeneratedRoom.fbx", "fbx");
                    if (!string.IsNullOrEmpty(path))
                    {
                        generator.ExportRoomAsFBX(path);
                    }
                }
            }
        }

        private void HandleCellSelection(RoomGenerator generator)
        {
            // Selected cell info
            int selectedX = (int)typeof(RoomGenerator).GetField("selectedX",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(generator);
            int selectedY = (int)typeof(RoomGenerator).GetField("selectedY",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(generator);

            if (selectedX >= 0 && selectedY >= 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Selected Cell: ({selectedX}, {selectedY})", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                float currentHeight = generator.GetCellHeight(selectedX, selectedY);
                float newHeight = EditorGUILayout.Slider("Cell Height", currentHeight, 0f, 5f);
                if (EditorGUI.EndChangeCheck())
                {
                    generator.SetCellHeight(selectedX, selectedY, newHeight);
                    if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
                }

                // Wall toggles - only show if cell has height
                if (generator.GetCellHeight(selectedX, selectedY) > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Walls", EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();

                    bool leftWall = generator.GetCellWall(selectedX, selectedY, WallSide.Left);
                    bool rightWall = generator.GetCellWall(selectedX, selectedY, WallSide.Right);
                    bool backWall = generator.GetCellWall(selectedX, selectedY, WallSide.Back);
                    bool frontWall = generator.GetCellWall(selectedX, selectedY, WallSide.Front);

                    leftWall = EditorGUILayout.Toggle("Left Wall (West)", leftWall);
                    rightWall = EditorGUILayout.Toggle("Right Wall (East)", rightWall);
                    backWall = EditorGUILayout.Toggle("Back Wall (South)", backWall);
                    frontWall = EditorGUILayout.Toggle("Front Wall (North)", frontWall);

                    if (EditorGUI.EndChangeCheck())
                    {
                        generator.SetCellWall(selectedX, selectedY, WallSide.Left, leftWall);
                        generator.SetCellWall(selectedX, selectedY, WallSide.Right, rightWall);
                        generator.SetCellWall(selectedX, selectedY, WallSide.Back, backWall);
                        generator.SetCellWall(selectedX, selectedY, WallSide.Front, frontWall);

                        if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
                    }
                }
            }
        }

        private void DrawGrid(RoomGenerator generator)
        {
            int gridWidth = (int)typeof(RoomGenerator).GetField("gridWidth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(generator);
            int gridHeight = (int)typeof(RoomGenerator).GetField("gridHeight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(generator);

            var selectedXField = typeof(RoomGenerator).GetField("selectedX",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var selectedYField = typeof(RoomGenerator).GetField("selectedY",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            int selectedX = (int)selectedXField.GetValue(generator);
            int selectedY = (int)selectedYField.GetValue(generator);

            Rect gridRect = GUILayoutUtility.GetRect(
                cellSize * gridWidth + gridPadding * 2,
                cellSize * gridHeight + gridPadding * 2
            );

            Event e = Event.current;

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Rect cellRect = new Rect(
                        gridRect.x + gridPadding + x * cellSize,
                        gridRect.y + gridPadding + (gridHeight - 1 - y) * cellSize,
                        cellSize - 2,
                        cellSize - 2
                    );

                    bool isHovering = cellRect.Contains(e.mousePosition);

                    if (isHovering && e.type == EventType.MouseDown && e.button == 0)
                    {
                        isDragging = true;
                        generator.ToggleCell(x, y);
                        if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
                        e.Use();
                        Repaint();
                    }
                    else if (isHovering && e.type == EventType.MouseDown && e.button == 1)
                    {
                        selectedXField.SetValue(generator, x);
                        selectedYField.SetValue(generator, y);
                        e.Use();
                        Repaint();
                    }
                    else if (isHovering && e.type == EventType.MouseDrag && isDragging && e.button == 0)
                    {
                        if (generator.GetCellHeight(x, y) <= 0)
                        {
                            generator.SetCellHeight(x, y, defaultHeightProp.floatValue);
                            if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
                            Repaint();
                        }
                        e.Use();
                    }

                    bool isActive = generator.GetCellHeight(x, y) > 0;
                    bool isSelected = (x == selectedX && y == selectedY);

                    Color cellColor;

                    if (isSelected && isActive)
                        cellColor = new Color(1f, 0.8f, 0.2f);
                    else if (isSelected)
                        cellColor = new Color(0.8f, 0.5f, 0.1f);
                    else if (isActive)
                    {
                        float heightRatio = generator.GetCellHeight(x, y) / 5f;
                        cellColor = Color.Lerp(new Color(0.2f, 0.4f, 1f), new Color(0.2f, 1f, 0.4f), heightRatio);
                    }
                    else
                        cellColor = new Color(0.2f, 0.2f, 0.2f);

                    EditorGUI.DrawRect(cellRect, cellColor);
                    Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, new Color(0.3f, 0.3f, 0.3f));

                    if (isActive)
                    {
                        GUIStyle style = new GUIStyle(GUI.skin.label);
                        style.alignment = TextAnchor.MiddleCenter;
                        style.normal.textColor = Color.white;
                        style.fontSize = 10;
                        GUI.Label(cellRect, generator.GetCellHeight(x, y).ToString("F1"), style);
                    }
                }
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
            }

            EditorGUILayout.HelpBox("Left-click and drag to paint cells. Right-click to select cell and adjust height.", MessageType.Info);
        }
    }
}


#endif