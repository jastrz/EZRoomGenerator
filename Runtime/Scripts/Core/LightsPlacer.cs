using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EZRoomGen.Core
{
    /// <summary>
    /// Handles the placement of ceiling lights in procedurally generated rooms and corridors.
    /// Automatically distributes lights based on tile type and configurable spacing rules.
    /// </summary>
    [System.Serializable]
    public class LightsPlacer
    {
        private enum LightPlaceMode
        {
            Light,
            Prefab
        }

        [SerializeField] private List<GameObject> placedLightsObjects = new List<GameObject>();
        [SerializeField] private LightPlaceMode lightPlacementMode = LightPlaceMode.Prefab;
        [SerializeField] private GameObject objectHolder;
        private GridData gridData;
        private GameObject cachedPrefab;

        /// <summary>
        /// Places ceiling lights throughout the grid based on tile type (room vs corridor).
        /// Clears any previously placed lights and distributes new lights with specified spacing.
        /// </summary>
        public void AddCeilingLights(GameObject parent, GameObject lampPrefab, GridData gridData, float roomSpacing, float corridorSpacing)
        {
            EnsureResourcesInitialized(parent, lampPrefab);

            this.gridData = gridData;

            List<Vector3> newLightPositions = new List<Vector3>();
            List<bool> newLightIsInRoom = new List<bool>();

            if (placedLightsObjects.Any(o => o == null))
            {
                ClearLights();
            }

            // Determine where lights should be placed
            for (int y = 0; y < gridData.gridHeight; y++)
            {
                for (int x = 0; x < gridData.gridWidth; x++)
                {
                    float height = gridData.GetCellHeight(x, y);
                    if (height <= 0) continue;

                    // Determine lighting rules
                    bool inRoom = IsRoom(x, y);
                    bool inCorridor = IsCorridor(x, y);

                    if (!inRoom && !inCorridor)
                        continue;

                    float spacing = inRoom ? roomSpacing : corridorSpacing;

                    // Compute world position of the light (center of the tile, at the roof)
                    Vector3 localLightPos = new Vector3(x + 0.5f, height, y + 0.5f);
                    Vector3 lightPos = parent.transform.TransformPoint(localLightPos);

                    // Check if too close to an already planned light
                    bool tooClose = false;
                    foreach (var pos in newLightPositions)
                    {
                        if (Vector3.Distance(pos, lightPos) < spacing)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (tooClose)
                        continue;

                    newLightPositions.Add(lightPos);
                    newLightIsInRoom.Add(inRoom);
                }
            }

            // Reuse existing lights, update their positions
            int reuseCount = Mathf.Min(placedLightsObjects.Count, newLightPositions.Count);

            for (int i = 0; i < reuseCount; i++)
            {
                GameObject lightObject = placedLightsObjects[i];
                lightObject.transform.position = newLightPositions[i];
            }

            // Create new lights
            for (int i = reuseCount; i < newLightPositions.Count; i++)
            {
                GameObject lightObject;

                if (lightPlacementMode == LightPlaceMode.Prefab && lampPrefab != null)
                {
                    lightObject = CreateLampPrefab(lampPrefab, parent, newLightPositions[i]);
                }
                else
                {
                    lightObject = CreatePointLight(newLightIsInRoom[i], newLightPositions[i]);
                }

                placedLightsObjects.Add(lightObject);
            }

            // Destroy excess lights
            for (int i = placedLightsObjects.Count - 1; i >= newLightPositions.Count; i--)
            {
                GameObject.DestroyImmediate(placedLightsObjects[i]);
                placedLightsObjects.RemoveAt(i);
            }
        }

        /// <summary>
        /// Removes existing lights.
        /// </summary>
        public void Clear()
        {
            ClearLights();
        }

        private void ClearLights()
        {
            foreach (var obj in placedLightsObjects)
            {
                if (obj != null)
                {
                    GameObject.DestroyImmediate(obj);
                }
            }

            placedLightsObjects.Clear();
        }

        /// <summary>
        /// Checks if required resources are available and initializes them if they're not.
        /// </summary>
        private void EnsureResourcesInitialized(GameObject parent, GameObject lampPrefab)
        {
            if (lampPrefab != cachedPrefab)
            {
                ClearLights();
                cachedPrefab = lampPrefab;
            }

            if (placedLightsObjects == null)
            {
                placedLightsObjects = new List<GameObject>();
            }

            if (objectHolder == null)
            {
                objectHolder = new GameObject(Constants.DefaultObjectHolderName);
                objectHolder.transform.parent = parent.transform;
                objectHolder.transform.localPosition = Vector3.zero;
            }
        }

        private GameObject CreatePointLight(bool inRoom, Vector3 lightPos)
        {
            GameObject lightObject = new GameObject(Constants.DefaultNonPrefabLightName);
            lightObject.transform.parent = objectHolder.transform;
            lightObject.transform.position = lightPos;

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = inRoom ? 6f : 4f;
            light.intensity = inRoom ? 1.7f : 1.2f;
            return lightObject;
        }

        private GameObject CreateLampPrefab(GameObject lampPrefab, GameObject parent, Vector3 lightPos)
        {
#if UNITY_EDITOR
            GameObject instance = PrefabUtility.InstantiatePrefab(lampPrefab, objectHolder.transform) as GameObject;
            instance.transform.position = lightPos;
            instance.transform.rotation = Quaternion.Euler(90, 90, 0);
            return instance;
#else
            return GameObject.Instantiate(lampPrefab, lightPos, Quaternion.Euler(90, 90, 0), objectHolder.transform);
#endif
        }

        /// <summary>
        /// Checks if the specified grid cell is walkable (has positive height).
        /// </summary>
        private bool IsWalkable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= gridData.gridWidth || y >= gridData.gridHeight)
                return false;

            return gridData.cells[x, y].height > 0;
        }

        /// <summary>
        /// Counts the number of walkable neighbors for a given grid cell.
        /// </summary>
        private int CountWalkableNeighbors(int x, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (IsWalkable(x + dx, y + dy)) count++;
                }
            }
            return count;
        }

        private bool IsRoom(int x, int y)
        {
            if (!IsWalkable(x, y)) return false;

            int n = CountWalkableNeighbors(x, y);

            return n >= 4;
        }

        private bool IsCorridor(int x, int y)
        {
            if (!IsWalkable(x, y)) return false;

            int n = CountWalkableNeighbors(x, y);

            return n >= 1 && n < 3;
        }
    }
}