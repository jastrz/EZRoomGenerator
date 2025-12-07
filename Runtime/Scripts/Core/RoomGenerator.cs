using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using EZRoomGen.Generation;

#if USE_FBX_EXPORTER
using UnityEditor.Formats.Fbx.Exporter;
#endif
using System.IO;
#endif

namespace EZRoomGen.Core
{
    /// <summary>
    /// Main component for procedural room generation from a grid-based layout.
    /// Provides grid editing, mesh generation, lighting placement, and FBX export functionality.
    /// Can be used both at runtime and in the Unity Editor.
    /// </summary>
    [ExecuteAlways]
    public class RoomGenerator : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;

        [Header("Room Parameters")]
        [SerializeField] private float defaultHeight = 2.5f;
        [SerializeField] private int meshResolution = 2;
        [SerializeField] private float uvScale = 1f;
        [SerializeField] private CellWinding cellWinding = CellWinding.Default;
        [SerializeField] private bool realtimeGeneration = true;
        [SerializeField] private bool invertRoof = false;

        [Header("Materials")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material roofMaterial;

        [Header("Lighting")]
        [SerializeField] private bool automaticallyAddLights = true;
        [SerializeField] private GameObject lampPrefab;
        [SerializeField] private float roomSpacing = 4f;
        [SerializeField] private float corridorSpacing = 6f;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = false;
        [SerializeField] private bool addColliders = true;

        [SerializeField][HideInInspector] private GridData gridData;
        [SerializeField][HideInInspector] private GameObject roomObject;
        [SerializeField] private LayoutGeneratorType generatorType;
        private RoomMeshGenerator roomMeshGenerator;


        // Editor-only fields for grid interaction
        private int selectedX = -1;
        private int selectedY = -1;

        private void Awake()
        {
            InitializeGrid();
            LoadDefaultAssets();
        }

        private void Start()
        {
            if (generateOnStart && HasActiveCells())
            {
                GenerateRoom();
            }
        }

        /// <summary>
        /// Initializes or reinitializes the grid data structure.
        /// Creates a new grid if it doesn't exist or if dimensions have changed.
        /// </summary>
        public void InitializeGrid()
        {
            if (gridData == null || gridData.gridWidth != gridWidth || gridData.gridHeight != gridHeight)
            {
                gridData = new GridData(gridWidth, gridHeight);
            }
        }

        /// <summary>
        /// Sets the height of a specific grid cell.
        /// </summary>
        /// <param name="height">The height value to set. Use 0 for empty cells.</param>
        public void SetCellHeight(int x, int y, float height)
        {
            if (gridData == null) InitializeGrid();

            if (x >= 0 && x < gridData.gridWidth && y >= 0 && y < gridData.gridHeight)
            {
                gridData.cells[x, y].height = height;
            }
        }

        /// <summary>
        /// Gets the height of a specific grid cell.
        /// </summary>
        /// <returns>The height of the cell, or 0 if coordinates are out of bounds.</returns>
        public float GetCellHeight(int x, int y)
        {
            if (gridData == null) InitializeGrid();

            if (x >= 0 && x < gridData.gridWidth && y >= 0 && y < gridData.gridHeight)
            {
                return gridData.cells[x, y].height;
            }
            return 0f;
        }

        /// <summary>
        /// Toggles a cell between active (default height) and inactive (height 0).
        /// </summary>
        public void ToggleCell(int x, int y)
        {
            if (gridData == null) InitializeGrid();

            if (x >= 0 && x < gridData.gridWidth && y >= 0 && y < gridData.gridHeight)
            {
                gridData.cells[x, y].height = gridData.cells[x, y].height > 0 ? 0 : defaultHeight;
            }
        }

        /// <summary>
        /// Clears all cells in the grid, setting all heights to 0.
        /// Also resets the selected cell indices.
        /// </summary>
        public void ClearGrid()
        {
            if (gridData == null) InitializeGrid();
            gridData.ClearGrid();
            selectedX = -1;
            selectedY = -1;
        }

        /// <summary>
        /// Resizes the grid to new dimensions, preserving existing cell data where possible.
        /// Dimensions are clamped between 2 and 50.
        /// </summary>
        public void ResizeGrid(int newWidth, int newHeight)
        {
            gridWidth = Mathf.Clamp(newWidth, 2, 50);
            gridHeight = Mathf.Clamp(newHeight, 2, 50);

            if (gridData == null)
            {
                gridData = new GridData(gridWidth, gridHeight);
            }
            else
            {
                gridData.ResizeGrid(ref gridWidth, ref gridHeight);
            }

            if (selectedX >= gridData.gridWidth || selectedY >= gridData.gridHeight)
            {
                selectedX = -1;
                selectedY = -1;
            }
        }

        /// <summary>
        /// Generates the room mesh from the current grid data.
        /// Destroys any previously generated room and creates a new one with floors, walls, roof, and optional lighting.
        /// </summary>
        /// <returns>The newly generated room GameObject, or null if generation failed.</returns>
        public GameObject GenerateRoom()
        {
            if (gridData == null) InitializeGrid();

            if (roomObject != null)
            {
                if (Application.isPlaying)
                    Destroy(roomObject);
                else
                    DestroyImmediate(roomObject);
                roomObject = null;
            }

            roomMeshGenerator = new RoomMeshGenerator(
                gridData,
                uvScale,
                meshResolution,
                cellWinding,
                floorMaterial,
                wallMaterial,
                roofMaterial,
                invertRoof
            );

            roomObject = roomMeshGenerator.GenerateRoom();

            if (roomObject != null)
            {
                roomObject.transform.SetParent(transform);
                roomObject.transform.localPosition = Vector3.zero;
                roomObject.transform.localRotation = Quaternion.identity;
            }

            if (addColliders && roomObject != null)
            {
                AddCollidersToRoom(roomObject);
            }

            if (automaticallyAddLights && roomObject != null)
            {
                var lightsPlacer = new LightsPlacer(gridData);
                lightsPlacer.AddCeilingLights(roomObject, lampPrefab, roomSpacing, corridorSpacing);
            }

            return roomObject;
        }

        /// <summary>
        /// Destroys the currently generated room GameObject.
        /// </summary>
        public void ClearRoom()
        {
            if (roomObject != null)
            {
                if (Application.isPlaying)
                    Destroy(roomObject);
                else
                    DestroyImmediate(roomObject);
                roomObject = null;
            }
        }

        /// <summary>
        /// Gets the currently generated room GameObject.
        /// </summary>
        /// <returns>The room GameObject, or null if no room has been generated.</returns>
        public GameObject GetRoomObject()
        {
            return roomObject;
        }

        /// <summary>
        /// Gets the state of a specific wall on a grid cell.
        /// </summary>
        /// <returns>True if the wall is enabled; false otherwise. Returns true if coordinates are out of bounds.</returns>
        public bool GetCellWall(int x, int y, WallSide wall)
        {
            if (gridData == null) InitializeGrid();

            if (x >= 0 && x < gridData.gridWidth && y >= 0 && y < gridData.gridHeight)
            {
                return GetWallValue(gridData.cells[x, y], wall);
            }
            return true;
        }

        /// <summary>
        /// Sets the state of a specific wall on a grid cell.
        /// </summary>
        public void SetCellWall(int x, int y, WallSide wall, bool enabled)
        {
            if (gridData == null) InitializeGrid();

            if (x >= 0 && x < gridData.gridWidth && y >= 0 && y < gridData.gridHeight)
            {
                SetWallValue(gridData.cells[x, y], wall, enabled);
            }
        }

        /// <summary>
        /// Toggles the state of a specific wall on a grid cell between enabled and disabled.
        /// </summary>
        public void ToggleCellWall(int x, int y, WallSide wall)
        {
            if (gridData == null) InitializeGrid();

            if (x >= 0 && x < gridData.gridWidth && y >= 0 && y < gridData.gridHeight)
            {
                Cell cell = gridData.cells[x, y];
                SetWallValue(cell, wall, !GetWallValue(cell, wall));
            }
        }

        /// <summary>
        /// Creates a simple hollow rectangular room with walls on the perimeter.
        /// Clears the existing grid and generates a new room automatically.
        /// </summary>
        public void CreateSimpleRoom()
        {
            ClearGrid();

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (x == 0 || x == gridWidth - 1 || y == 0 || y == gridHeight - 1)
                    {
                        SetCellHeight(x, y, defaultHeight);
                    }
                }
            }

            GenerateRoom();
        }

        /// <summary>
        /// Loads grid data from a 2D height array.
        /// Resizes the grid to match the array dimensions and copies all height values.
        /// </summary>
        /// <param name="heightData">2D array of height values where [x,y] corresponds to grid cell coordinates.</param>
        public void LoadGridFromArray(float[,] heightData)
        {
            if (heightData == null) return;

            int width = heightData.GetLength(0);
            int height = heightData.GetLength(1);

            ResizeGrid(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gridData.cells[x, y].height = heightData[x, y];
                }
            }
        }

        /// <summary>
        /// Exports the current grid data to a 2D height array.
        /// </summary>
        /// <returns>A 2D float array containing all cell height values.</returns>
        public float[,] ExportGridToArray()
        {
            if (gridData == null) InitializeGrid();

            float[,] heightData = new float[gridData.gridWidth, gridData.gridHeight];

            for (int x = 0; x < gridData.gridWidth; x++)
            {
                for (int y = 0; y < gridData.gridHeight; y++)
                {
                    heightData[x, y] = gridData.cells[x, y].height;
                }
            }

            return heightData;
        }

        private bool GetWallValue(Cell cell, WallSide wall)
        {
            return wall switch
            {
                WallSide.Left => cell.leftWall,
                WallSide.Right => cell.rightWall,
                WallSide.Back => cell.backWall,
                WallSide.Front => cell.frontWall,
                _ => true
            };
        }

        private void SetWallValue(Cell cell, WallSide wall, bool value)
        {
            switch (wall)
            {
                case WallSide.Left:
                    cell.leftWall = value;
                    break;
                case WallSide.Right:
                    cell.rightWall = value;
                    break;
                case WallSide.Back:
                    cell.backWall = value;
                    break;
                case WallSide.Front:
                    cell.frontWall = value;
                    break;
            }
        }

        /// <summary>
        /// Checks if the grid has any cells with height greater than 0.
        /// </summary>
        /// <returns>True if at least one cell is active; false if all cells are empty.</returns>
        private bool HasActiveCells()
        {
            if (gridData == null) return false;

            for (int y = 0; y < gridData.gridHeight; y++)
            {
                for (int x = 0; x < gridData.gridWidth; x++)
                {
                    if (gridData.cells[x, y].height > 0)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds MeshCollider components to the floor and wall meshes of the generated room.
        /// </summary>
        /// <param name="room">The room GameObject to add colliders to.</param>
        private void AddCollidersToRoom(GameObject room)
        {
            foreach (Transform child in room.GetComponentsInChildren<Transform>(true))
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

        /// <summary>
        /// Loads default materials and prefabs from Resources folder if not already assigned.
        /// </summary>
        private void LoadDefaultAssets()
        {
            if (wallMaterial == null)
            {
                wallMaterial = Resources.Load<Material>("EZRoomGen/Materials/wall_material");
                if (wallMaterial == null)
                    Debug.LogWarning("EZ Room Gen: Default wall material not found in Resources/EZRoomGen/Materials/");
            }

            if (floorMaterial == null)
            {
                floorMaterial = Resources.Load<Material>("EZRoomGen/Materials/floor_material");
                if (floorMaterial == null)
                    Debug.LogWarning("EZ Room Gen: Default floor material not found in Resources/EZRoomGen/Materials/");
            }

            if (roofMaterial == null)
            {
                roofMaterial = Resources.Load<Material>("EZRoomGen/Materials/roof_material");
                if (roofMaterial == null)
                {
                    // Fallback to floor material if roof material doesn't exist
                    roofMaterial = floorMaterial;
                }
            }

            if (lampPrefab == null)
            {
                lampPrefab = Resources.Load<GameObject>("EZRoomGen/Prefabs/lamp");
                if (lampPrefab == null)
                    Debug.LogWarning("EZ Room Gen: Default lamp prefab not found in Resources/EZRoomGen/Prefabs/");
            }
        }

        private void OnDestroy()
        {
            ClearRoom();
        }

#if UNITY_EDITOR && USE_FBX_EXPORTER
        /// <summary>
        /// Exports the currently generated room as an FBX file.
        /// Only available in the Unity Editor. Creates the directory if it doesn't exist.
        /// </summary>
        /// <param name="path">Full file path for the exported FBX file (including .fbx extension).</param>
        public void ExportRoomAsFBX(string path)
        {
            if (roomObject == null)
            {
                Debug.LogWarning("EZ Room Gen: No generated room to export.");
                return;
            }

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

