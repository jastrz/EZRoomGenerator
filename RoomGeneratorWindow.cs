using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using System.IO;

namespace EZRoomGen
{
    public enum CellWinding
    {
        Default,
        Flipped,
        DoubleSided
    }

    public class Cell
    {
        public float height;
    }

    public class RoomGeneratorWindow : EditorWindow
    {
        [SerializeField] private GridData gridData;
        private float defaultHeight = 2.5f;
        private int meshResolution = 2;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material roofMaterial;
        private bool realtimeGeneration = true;
        private bool automaticallyAddLights = true;

        private GameObject roomObject;
        private Vector2 scrollPos;
        private const float cellSize = 30f;
        private const float gridPadding = 10f;
        private int selectedX = -1;
        private int selectedY = -1;
        private bool isDragging = false;
        private float uvScale = 1f;

        // light placement data
        [SerializeField] private float roomSpacing = 4f;
        [SerializeField] private float corridorSpacing = 6f;
        [SerializeField] private GameObject lampPrefab;

        private CellWinding cellWinding;

        private RoomMeshGenerator roomMeshGenerator;

        [MenuItem("Tools/Room Generator")]
        public static void ShowWindow()
        {
            GetWindow<RoomGeneratorWindow>("Room Generator");
        }

        private void OnEnable()
        {
            gridData = new GridData(10, 10);
            selectedX = -1;
            selectedY = -1;
        }

        private void ResizeGrid(int newWidth, int newHeight)
        {
            gridData.ResizeGrid(newWidth, newHeight);

            if (selectedX >= gridData.gridWidth || selectedY >= gridData.gridHeight)
            {
                selectedX = -1;
                selectedY = -1;
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("Room Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.Label("Grid Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int newWidth = EditorGUILayout.IntSlider("Grid Width", gridData.gridWidth, 2, 50);
            int newGridHeight = EditorGUILayout.IntSlider("Grid Height", gridData.gridHeight, 2, 50);
            if (EditorGUI.EndChangeCheck())
            {
                ResizeGrid(newWidth, newGridHeight);
                if (realtimeGeneration) GenerateRoom();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Parameters", EditorStyles.boldLabel);

            realtimeGeneration = EditorGUILayout.Toggle("Realtime Generation", realtimeGeneration);

            EditorGUI.BeginChangeCheck();
            defaultHeight = EditorGUILayout.Slider("Default Height", defaultHeight, 0.5f, 5f);
            meshResolution = EditorGUILayout.IntSlider("Mesh Resolution", meshResolution, 1, 10);
            uvScale = EditorGUILayout.Slider("UV Scale", uvScale, 0.1f, 5f);
            cellWinding = (CellWinding)EditorGUILayout.EnumPopup("Cell Winding", cellWinding);

            bool paramsChanged = EditorGUI.EndChangeCheck();

            if (selectedX >= 0 && selectedY >= 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Selected Cell: ({selectedX}, {selectedY})", EditorStyles.boldLabel);
                Cell selectedCell = gridData.cells[selectedX, selectedY];

                EditorGUI.BeginChangeCheck();

                selectedCell.height = EditorGUILayout.Slider("Cell Height", selectedCell.height, 0f, 5f);
                // Add more fields here as Cell gets more properties!

                if (EditorGUI.EndChangeCheck())
                {
                    if (realtimeGeneration) GenerateRoom();
                    Repaint();
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Materials", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            wallMaterial = (Material)EditorGUILayout.ObjectField("Wall Material", wallMaterial, typeof(Material), false);
            floorMaterial = (Material)EditorGUILayout.ObjectField("Floor Material", floorMaterial, typeof(Material), false);
            roofMaterial = (Material)EditorGUILayout.ObjectField("Roof Material", roofMaterial, typeof(Material), false);

            GUILayout.Label("Light Placement", EditorStyles.boldLabel);
            lampPrefab = (GameObject)EditorGUILayout.ObjectField("Lamp prefab", lampPrefab, typeof(GameObject), false);
            roomSpacing = EditorGUILayout.Slider("Room spacing", roomSpacing, 1f, 8f);
            corridorSpacing = EditorGUILayout.Slider("Room spacing", corridorSpacing, 1f, 8f);

            bool materialsChanged = EditorGUI.EndChangeCheck();
            EditorGUILayout.Space();

            GUILayout.Label("Room Layout (Click to select cell, adjust height above)", EditorStyles.boldLabel);
            DrawGrid();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Grid"))
            {
                ClearGrid();
                if (realtimeGeneration) GenerateRoom();
            }
            if (GUILayout.Button("Fill Grid"))
            {
                FillGrid();
                if (realtimeGeneration) GenerateRoom();
            }
            EditorGUILayout.EndHorizontal();

            if (!realtimeGeneration)
            {
                if (GUILayout.Button("Generate Room", GUILayout.Height(40)))
                {
                    GenerateRoom();
                }
            }

            if (roomObject != null)
            {
                if (GUILayout.Button("Clear Room", GUILayout.Height(30)))
                {
                    DestroyImmediate(roomObject);
                    roomObject = null;
                }

                if (GUILayout.Button("Export as FBX", GUILayout.Height(30)))
                {
                    ExportRoomAsFBX();
                }
            }

            EditorGUILayout.EndScrollView();

            if (realtimeGeneration && (paramsChanged || materialsChanged))
            {
                if (HasActiveCells())
                {
                    GenerateRoom();
                }
            }
        }

        private bool HasActiveCells()
        {
            for (int y = 0; y < gridData.gridHeight; y++)
                for (int x = 0; x < gridData.gridWidth; x++)
                    if (gridData.cells[x, y].height > 0)
                        return true;
            return false;
        }

        private void DrawGrid()
        {
            Rect gridRect = GUILayoutUtility.GetRect(
                cellSize * gridData.gridWidth + gridPadding * 2,
                cellSize * gridData.gridHeight + gridPadding * 2
            );

            Event e = Event.current;

            for (int y = 0; y < gridData.gridHeight; y++)
            {
                for (int x = 0; x < gridData.gridWidth; x++)
                {
                    Rect cellRect = new Rect(
                        gridRect.x + gridPadding + x * cellSize,
                        gridRect.y + gridPadding + (gridData.gridHeight - 1 - y) * cellSize,
                        cellSize - 2,
                        cellSize - 2
                    );

                    bool isHovering = cellRect.Contains(e.mousePosition);

                    // Handle mouse down to start painting
                    if (isHovering && e.type == EventType.MouseDown && e.button == 0)
                    {
                        isDragging = true;
                        gridData.cells[x, y].height = gridData.cells[x, y].height > 0 ? 0 : defaultHeight;
                        if (realtimeGeneration) GenerateRoom();
                        e.Use();
                        Repaint();
                    }
                    // Handle right-click to select cell
                    else if (isHovering && e.type == EventType.MouseDown && e.button == 1)
                    {
                        selectedX = x;
                        selectedY = y;
                        e.Use();
                        Repaint();
                    }
                    // Handle dragging to paint cells
                    else if (isHovering && e.type == EventType.MouseDrag && isDragging && e.button == 0)
                    {
                        if (gridData.cells[x, y].height <= 0)
                        {
                            gridData.cells[x, y].height = defaultHeight;
                            if (realtimeGeneration) GenerateRoom();
                            Repaint();
                        }
                        e.Use();
                    }

                    // Draw the cell
                    bool isActive = gridData.cells[x, y].height > 0;
                    bool isSelected = (x == selectedX && y == selectedY);

                    Color cellColor;

                    // For coloring
                    if (isSelected && isActive)
                        cellColor = new Color(1f, 0.8f, 0.2f);
                    else if (isSelected)
                        cellColor = new Color(0.8f, 0.5f, 0.1f);
                    else if (isActive)
                    {
                        float heightRatio = gridData.cells[x, y].height / 5f;
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
                        GUI.Label(cellRect, gridData.cells[x, y].height.ToString("F1"), style);
                    }
                }
            }

            // Stop dragging on mouse up (processed after all cells)
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
            }

            EditorGUILayout.HelpBox("Left-click and drag to paint cells. Right-click to select cell and adjust height.", MessageType.Info);
        }

        private void ClearGrid()
        {
            gridData.ClearGrid();
            selectedX = -1;
            selectedY = -1;
            Repaint();
        }

        private void FillGrid()
        {
            gridData.FillGrid(defaultHeight);
            Repaint();
        }

        private void GenerateRoom()
        {
            if (roomObject != null)
            {
                DestroyImmediate(roomObject);
                roomObject = null;
            }

            roomMeshGenerator = new RoomMeshGenerator(gridData, uvScale, meshResolution, cellWinding,
                floorMaterial, wallMaterial, roofMaterial, false);

            roomObject = roomMeshGenerator.GenerateRoom();

            AddColliders(roomObject);

            if (automaticallyAddLights)
            {
                var lightsPlacer = new LightsPlacer(gridData);

                lightsPlacer.AddCeilingLights(roomObject, lampPrefab, roomSpacing, corridorSpacing);
            }

            Selection.activeGameObject = roomObject;
        }

        private void AddColliders(GameObject roomObject)
        {
            foreach (Transform child in roomObject.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Floor" || child.name == "Walls")
                {
                    MeshCollider collider = child.gameObject.GetComponent<MeshCollider>();
                    if (collider == null)
                    {
                        collider = child.gameObject.AddComponent<MeshCollider>();
                    }
                }
            }
        }

        private void ExportRoomAsFBX()
        {
            if (roomObject == null)
            {
                EditorUtility.DisplayDialog("Export Failed", "No generated room to export.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Room as FBX", "Assets", "GeneratedRoom.fbx", "fbx");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                ModelExporter.ExportObject(path, roomObject);

                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    AssetDatabase.Refresh();
                    Debug.Log($"✅ Room successfully exported to: {relativePath}");
                }
                else
                {
                    Debug.Log($"✅ Room successfully exported to: {path}");
                }

                EditorUtility.DisplayDialog("Export Successful", $"Room exported to:\n{path}", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ FBX Export failed: {ex.Message}");
                EditorUtility.DisplayDialog("Export Failed", "An error occurred during export.\n\n" + ex.Message, "OK");
            }
        }
    }
}

