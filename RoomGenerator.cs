using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using System.IO;
#endif

namespace EZRoomGen
{

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
        private RoomMeshGenerator roomMeshGenerator;

        // Editor-only fields for grid interaction
        private int selectedX = -1;
        private int selectedY = -1;

        private void Awake()
        {
            InitializeGrid();
        }

        private void Start()
        {
            if (generateOnStart && HasActiveCells())
            {
                GenerateRoom();
            }
        }

        public void InitializeGrid()
        {
            if (gridData == null || gridData.gridWidth != gridWidth || gridData.gridHeight != gridHeight)
            {
                gridData = new GridData(gridWidth, gridHeight);
            }
        }

        public void SetCellHeight(int x, int y, float height)
        {
            if (gridData == null) InitializeGrid();

            if (x >= 0 && x < gridData.gridWidth && y >= 0 && y < gridData.gridHeight)
            {
                gridData.cells[x, y].height = height;
            }
        }

        public float GetCellHeight(int x, int y)
        {
            if (gridData == null) InitializeGrid();

            if (x >= 0 && x < gridData.gridWidth && y >= 0 && y < gridData.gridHeight)
            {
                return gridData.cells[x, y].height;
            }
            return 0f;
        }

        public void ToggleCell(int x, int y)
        {
            if (gridData == null) InitializeGrid();

            if (x >= 0 && x < gridData.gridWidth && y >= 0 && y < gridData.gridHeight)
            {
                gridData.cells[x, y].height = gridData.cells[x, y].height > 0 ? 0 : defaultHeight;
            }
        }

        public void ClearGrid()
        {
            if (gridData == null) InitializeGrid();
            gridData.ClearGrid();
            selectedX = -1;
            selectedY = -1;
        }

        public void FillGrid()
        {
            if (gridData == null) InitializeGrid();
            gridData.FillGrid(defaultHeight);
        }

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
                gridData.ResizeGrid(gridWidth, gridHeight);
            }

            if (selectedX >= gridData.gridWidth || selectedY >= gridData.gridHeight)
            {
                selectedX = -1;
                selectedY = -1;
            }
        }

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

            if (automaticallyAddLights && roomObject != null && lampPrefab != null)
            {
                var lightsPlacer = new LightsPlacer(gridData);
                lightsPlacer.AddCeilingLights(roomObject, lampPrefab, roomSpacing, corridorSpacing);
            }

            return roomObject;
        }

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

        public GameObject GetRoomObject()
        {
            return roomObject;
        }

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

        private void OnDestroy()
        {
            ClearRoom();
        }

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

#if UNITY_EDITOR
        public void ExportRoomAsFBX(string path)
        {
            if (roomObject == null)
            {
                Debug.LogWarning("No generated room to export.");
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
                    Debug.Log($"✅ Room successfully exported to: {relativePath}");
                }
                else
                {
                    Debug.Log($"✅ Room successfully exported to: {path}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ FBX Export failed: {ex.Message}");
            }
        }
#endif
    }
}

