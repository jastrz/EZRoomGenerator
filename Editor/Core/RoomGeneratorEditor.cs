#if UNITY_EDITOR

using EZRoomGen.Generation;
using EZRoomGen.Generation.Editor;
using UnityEditor;
using UnityEngine;

namespace EZRoomGen.Core.Editor
{
    /// <summary>
    /// Main editor class that handles room generation settings and displays layout grid.
    /// </summary>
    [CustomEditor(typeof(RoomGenerator))]
    public class RoomGeneratorEditor : UnityEditor.Editor
    {
        private const float cellSize = 30f;
        private const float gridPadding = 10f;
        private bool isDragging = false;
        private Vector2 scrollPos;
        private bool showGrid = true;

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
        private SerializedProperty generatorTypeProp;

        private RoomCorridorLayoutGeneratorEditor roomCorridorGeneratorEditor = new();
        private DungeonLayoutGeneratorEditor dungeonLayoutGeneratorEditor = new();
        private RoomCorridorLayoutGeneratorSettings roomCorridorGeneratorSettings = new();
        private DungeonLayoutGeneratorSettings dungeonLayoutGeneratorSettings = new();
        private ILayoutGenerator layoutGenerator;
        private RoomGenerator generator;

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
            generatorTypeProp = serializedObject.FindProperty("generatorType");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            generator = (RoomGenerator)target;
            if (generator == null) return;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            ShowHeader();
            DrawGridSettings();
            bool paramsChanged = DrawParameters();
            bool materialsChanged = DrawMaterials();
            bool lightingParamsChanged = DrawLightPlacement();

            EditorGUILayout.Space();
            HandleCellSelection();

            EditorGUILayout.Space();
            DrawLayoutGeneratorMenu();

            EditorGUILayout.Space();
            HandleGrid();

            EditorGUILayout.Space();
            DrawBottomMenu();

            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();

            if (realtimeGenerationProp.boolValue && (paramsChanged || materialsChanged || lightingParamsChanged))
            {
                generator.GenerateRoom();
            }
        }

        /// <summary>
        /// Controls grid display.
        /// </summary>
        private void HandleGrid()
        {
            showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);

            if (showGrid)
            {
                // EditorGUILayout.LabelField("Room Layout (Click to paint, Right-click to select)", EditorStyles.boldLabel);
                DrawGrid(generator);
            }
        }

        private void ShowHeader()
        {
            EditorGUILayout.LabelField("EZRoom Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        private void DrawGridSettings()
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

        /// <summary>
        /// Shows mesh generation settings.
        /// </summary>
        private bool DrawParameters()
        {
            EditorGUILayout.LabelField("Room/Mesh Generation", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(defaultHeightProp, 1f, 10f, new GUIContent("Default Height"));
            EditorGUILayout.IntSlider(meshResolutionProp, 1, 12, new GUIContent("Mesh Resolution"));
            EditorGUILayout.Slider(uvScaleProp, 1f, 10f, new GUIContent("UV Scale"));
            EditorGUILayout.PropertyField(cellWindingProp);
            EditorGUILayout.PropertyField(realtimeGenerationProp);
            EditorGUILayout.PropertyField(addCollidersProp);
            EditorGUILayout.PropertyField(invertRoofProp);
            bool changed = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            return changed;
        }

        /// <summary>
        /// Shows materials settings.
        /// </summary>
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

        /// <summary>
        /// Displays options for procedural light placement.
        /// </summary>
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

        /// <summary>
        /// Draws menu with basic operations for generated room.
        /// </summary>
        private void DrawBottomMenu()
        {
            EditorGUILayout.BeginHorizontal();

            // Clear Grid button
            if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
            {
                generator.ClearGrid();
                if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
            }

            // Generate Room button (only if not in realtime mode)
            if (!realtimeGenerationProp.boolValue)
            {
                if (GUILayout.Button("Generate Room", GUILayout.Height(30)))
                {
                    generator.GenerateRoom();
                }
            }

#if USE_FBX_EXPORTER
            // Export as FBX button (only if room object exists)
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
#endif

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Displays layout generation settings.
        /// </summary>
        private void DrawLayoutGeneratorMenu()
        {
            EditorGUILayout.LabelField("Layout Generation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(generatorTypeProp);

            var index = generatorTypeProp.enumValueIndex;
            LayoutGeneratorType type = (LayoutGeneratorType)index;

            bool shouldGenerate = false;

            if (type == LayoutGeneratorType.RoomCorridor)
            {
                shouldGenerate = roomCorridorGeneratorEditor.DrawInspector(roomCorridorGeneratorSettings);
                roomCorridorGeneratorSettings.height = defaultHeightProp.floatValue;
                layoutGenerator = new RoomCorridorLayoutGenerator(roomCorridorGeneratorSettings);

            }
            else if (type == LayoutGeneratorType.Dungeon)
            {
                shouldGenerate = dungeonLayoutGeneratorEditor.DrawInspector(dungeonLayoutGeneratorSettings);
                dungeonLayoutGeneratorSettings.height = defaultHeightProp.floatValue;
                layoutGenerator = new DungeonLayoutGenerator(dungeonLayoutGeneratorSettings);
            }

            if (realtimeGenerationProp.boolValue)
            {
                if (shouldGenerate)
                {
                    GenerateRoomFromLayout();
                }
            }
            else
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Generate Layout & Room", GUILayout.Height(30), GUILayout.Width(240)))
                {
                    GenerateRoomFromLayout();
                }
            }
        }

        private void GenerateRoomFromLayout()
        {
            float[,] layout = layoutGenerator.Generate(gridWidthProp.intValue, gridHeightProp.intValue);
            generator.LoadGridFromArray(layout);
            generator.GenerateRoom();
        }

        /// <summary>
        /// Handles setting height and walls configuration for selected cell.
        /// </summary>
        private void HandleCellSelection()
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

        /// <summary>
        /// Displays grid layout and handles its input.
        /// </summary>
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

                    // Left mouse: paint
                    if (isHovering && e.type == EventType.MouseDown && e.button == 0)
                    {
                        isDragging = true;
                        generator.ToggleCell(x, y);
                        if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
                        e.Use();
                        Repaint();
                    }
                    // Right mouse: select
                    else if (isHovering && e.type == EventType.MouseDown && e.button == 1)
                    {
                        selectedXField.SetValue(generator, x);
                        selectedYField.SetValue(generator, y);
                        e.Use();
                        Repaint();
                    }
                    // Left mouse drag: paint
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
                    // MIDDLE mouse down or drag: ERASE
                    else if (isHovering &&
                        ((e.type == EventType.MouseDown && e.button == 2) ||
                         (e.type == EventType.MouseDrag && e.button == 2)))
                    {
                        if (generator.GetCellHeight(x, y) > 0)
                        {
                            generator.SetCellHeight(x, y, 0f);  // Set inactive
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

            EditorGUILayout.HelpBox("Left-click and drag to paint cells. Right-click to select cell. Middle-click and drag to erase cells.", MessageType.Info);
        }
    }
}

#endif