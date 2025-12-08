#if UNITY_EDITOR

using System.IO;
using EZRoomGen.Generation;
using EZRoomGen.Generation.Editor;
using UnityEditor;
using UnityEngine;

#if USE_FBX_EXPORTER
using UnityEditor.Formats.Fbx.Exporter;
#endif

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
        private MazeLayoutGeneratorEditor mazeLayoutGeneratorEditor = new();
        private RoomCorridorLayoutGeneratorSettings roomCorridorGeneratorSettings = new();
        private DungeonLayoutGeneratorSettings dungeonLayoutGeneratorSettings = new();
        private MazeLayoutLayoutGeneratorSettings mazeLayoutLayoutGeneratorSettings = new();
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
            EditorGUILayout.Slider(roomSpacingProp, 2f, 10f, new GUIContent("Room Spacing"));
            EditorGUILayout.Slider(corridorSpacingProp, 2f, 10f, new GUIContent("Corridor Spacing"));

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
                        ExportRoomAsFBX(path);
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
            else if (type == LayoutGeneratorType.Maze)
            {
                shouldGenerate = mazeLayoutGeneratorEditor.DrawInspector(mazeLayoutLayoutGeneratorSettings);
                mazeLayoutLayoutGeneratorSettings.height = defaultHeightProp.floatValue;
                layoutGenerator = new MazeLayoutGenerator(mazeLayoutLayoutGeneratorSettings);
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
            var gridDimensions = GetGridDimensions(generator);
            var selectedCell = GetSelectedCell(generator);

            Rect gridRect = GUILayoutUtility.GetRect(
                cellSize * gridDimensions.width + gridPadding * 2,
                cellSize * gridDimensions.height + gridPadding * 2
            );

            Event e = Event.current;

            // Draw each cell in the grid
            for (int y = 0; y < gridDimensions.height; y++)
            {
                for (int x = 0; x < gridDimensions.width; x++)
                {
                    Rect cellRect = CalculateCellRect(gridRect, x, y, gridDimensions.height);

                    HandleCellInput(generator, x, y, cellRect, e, ref selectedCell);

                    bool isActive = generator.GetCellHeight(x, y) > 0;
                    bool isSelected = (x == selectedCell.x && y == selectedCell.y);

                    DrawCell(generator, cellRect, x, y, isActive, isSelected);
                }
            }

            // Reset dragging state when mouse button is released
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
            }

            EditorGUILayout.HelpBox("Left-click and drag to paint cells. Right-click to select cell. Middle-click and drag to erase cells.", MessageType.Info);
        }

        /// <summary>
        /// Retrieves the grid dimensions from the RoomGenerator .
        /// </summary>
        private (int width, int height) GetGridDimensions(RoomGenerator generator)
        {
            int width = (int)typeof(RoomGenerator).GetField("gridWidth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(generator);
            int height = (int)typeof(RoomGenerator).GetField("gridHeight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(generator);

            return (width, height);
        }

        /// <summary>
        /// Retrieves the currently selected cell coordinates from the RoomGenerator.
        /// </summary>
        private (int x, int y) GetSelectedCell(RoomGenerator generator)
        {
            var selectedXField = typeof(RoomGenerator).GetField("selectedX",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var selectedYField = typeof(RoomGenerator).GetField("selectedY",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            int x = (int)selectedXField.GetValue(generator);
            int y = (int)selectedYField.GetValue(generator);

            return (x, y);
        }

        /// <summary>
        /// Sets the selected cell coordinates in the RoomGenerator using reflection.
        /// </summary>
        private void SetSelectedCell(RoomGenerator generator, int x, int y)
        {
            var selectedXField = typeof(RoomGenerator).GetField("selectedX",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var selectedYField = typeof(RoomGenerator).GetField("selectedY",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            selectedXField.SetValue(generator, x);
            selectedYField.SetValue(generator, y);
        }

        /// <summary>
        /// Calculates the screen-space rectangle for a grid cell.
        /// Y-axis is inverted so that (0,0) is at the bottom-left.
        /// </summary>
        private Rect CalculateCellRect(Rect gridRect, int x, int y, int gridHeight)
        {
            return new Rect(
                gridRect.x + gridPadding + x * cellSize,
                gridRect.y + gridPadding + (gridHeight - 1 - y) * cellSize,
                cellSize - 2,
                cellSize - 2
            );
        }

        /// <summary>
        /// Handles mouse input for a cell: painting, selecting, and erasing.
        /// - Left click/drag: Toggle or paint cells
        /// - Right click: Select cell
        /// - Middle click/drag: Erase cells
        /// </summary>
        private void HandleCellInput(RoomGenerator generator, int x, int y, Rect cellRect, Event e, ref (int x, int y) selectedCell)
        {
            bool isHovering = cellRect.Contains(e.mousePosition);
            if (!isHovering) return;

            // Left mouse: paint (toggle cell on initial click)
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                isDragging = true;
                generator.ToggleCell(x, y);
                if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
                e.Use();
                Repaint();
            }
            // Right mouse: select cell for editing
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                SetSelectedCell(generator, x, y);
                selectedCell = (x, y);
                e.Use();
                Repaint();
            }
            // Left mouse drag: paint only inactive cells
            else if (e.type == EventType.MouseDrag && isDragging && e.button == 0)
            {
                if (generator.GetCellHeight(x, y) <= 0)
                {
                    generator.SetCellHeight(x, y, defaultHeightProp.floatValue);
                    if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
                    Repaint();
                }
                e.Use();
            }
            // Middle mouse: erase cells (set height to 0)
            else if ((e.type == EventType.MouseDown && e.button == 2) ||
                     (e.type == EventType.MouseDrag && e.button == 2))
            {
                if (generator.GetCellHeight(x, y) > 0)
                {
                    generator.SetCellHeight(x, y, 0f);
                    if (realtimeGenerationProp.boolValue) generator.GenerateRoom();
                    Repaint();
                }
                e.Use();
            }
        }

        /// <summary>
        /// Renders a single grid cell with its color, outline, and height label.
        /// </summary>
        private void DrawCell(RoomGenerator generator, Rect cellRect, int x, int y, bool isActive, bool isSelected)
        {
            Color cellColor = GetCellColor(generator, x, y, isActive, isSelected);

            // Draw cell background and outline
            EditorGUI.DrawRect(cellRect, cellColor);
            Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, new Color(0.3f, 0.3f, 0.3f));

            // Draw height value label on active cells
            if (isActive)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10
                };
                style.normal.textColor = Color.white;

                GUI.Label(cellRect, generator.GetCellHeight(x, y).ToString("F1"), style);
            }
        }

        /// <summary>
        /// Determines the color of a cell based on its state.
        /// </summary>
        private Color GetCellColor(RoomGenerator generator, int x, int y, bool isActive, bool isSelected)
        {
            if (isSelected && isActive)
                return new Color(1f, 0.8f, 0.2f); // Bright orange

            if (isSelected)
                return new Color(0.8f, 0.5f, 0.1f); // Dark orange

            if (isActive)
            {
                // Gradient from blue (low height) to green (high height)
                float heightRatio = generator.GetCellHeight(x, y) / 5f;
                return Color.Lerp(new Color(0.2f, 0.4f, 1f), new Color(0.2f, 1f, 0.4f), heightRatio);
            }

            return new Color(0.2f, 0.2f, 0.2f); // Dark gray (inactive)
        }

#if USE_FBX_EXPORTER
        /// <summary>
        /// Exports the currently generated room as an FBX file.
        /// Only available in the Unity Editor. Creates the directory if it doesn't exist.
        /// </summary>
        /// <param name="path">Full file path for the exported FBX file (including .fbx extension).</param>
        public void ExportRoomAsFBX(string path)
        {
            if (generator.RoomObject == null)
            {
                Debug.LogWarning("EZ Room Gen: No generated room to export.");
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                ModelExporter.ExportObject(path, generator.RoomObject);

                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    AssetDatabase.Refresh();
                    Debug.Log($"EZ Room Gen: ✅ Room successfully exported to: {relativePath}");
                }
                else
                {
                    Debug.Log($"EZ Room Gen: ✅ Room successfully exported to: {path}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"EZ Room Gen: ❌ FBX Export failed: {ex.Message}");
            }
        }
#endif
    }
}

#endif