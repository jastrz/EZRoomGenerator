using System.Collections.Generic;
using UnityEngine;

namespace EZRoomGen
{
    public class LightsPlacer
    {
        private enum LightPlaceMode
        {
            Light,
            Prefab
        }

        private List<Vector3> placedLights = new List<Vector3>();
        private List<GameObject> placedLightsObjects = new List<GameObject>();
        private GridData gridData;
        private LightPlaceMode lightPlaceMode;

        public LightsPlacer(GridData gridData)
        {
            this.gridData = gridData;
            placedLightsObjects = new List<GameObject>();
            placedLights = new List<Vector3>();
            lightPlaceMode = LightPlaceMode.Prefab;
        }

        public void AddCeilingLights(GameObject parent, GameObject lampPrefab, float roomSpacing, float corridorSpacing)
        {
            placedLights = new List<Vector3>();

            if (placedLightsObjects.Count > 0)
            {
                for (int i = placedLights.Count - 1; i >= 0; i--)
                {
                    GameObject.DestroyImmediate(placedLightsObjects[i]);
                }
            }

            for (int y = 0; y < gridData.gridHeight; y++)
            {
                for (int x = 0; x < gridData.gridWidth; x++)
                {
                    float height = gridData.cells[x, y].height;
                    if (height <= 0) continue;

                    // Determine lighting rules
                    bool inRoom = IsRoom(x, y);
                    bool inCorridor = IsCorridor(x, y);

                    if (!inRoom && !inCorridor)
                        continue;

                    float spacing = inRoom ? roomSpacing : corridorSpacing;

                    // Compute world position of the light (center of the tile, at the roof)
                    Vector3 lightPos = new Vector3(x + 0.5f, height, y + 0.5f) + parent.transform.position;

                    // Check if too close to an already placed light
                    bool tooClose = false;
                    foreach (var l in placedLights)
                    {
                        if (Vector3.Distance(l, lightPos) < spacing)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (tooClose)
                        continue;

                    GameObject lightObject;

                    if (lightPlaceMode == LightPlaceMode.Light || lampPrefab == null)
                    {
                        // Create point light
                        lightObject = new GameObject("CeilingLight");
                        lightObject.transform.parent = parent.transform;
                        lightObject.transform.position = lightPos;

                        Light light = lightObject.AddComponent<Light>();
                        light.type = LightType.Point;
                        light.range = inRoom ? 6f : 4f;
                        light.intensity = inRoom ? 1.7f : 1.2f;
                    }
                    else
                    {
                        lightObject = GameObject.Instantiate(lampPrefab, lightPos, Quaternion.Euler(90, 90, 0), parent.transform);
                    }

                    placedLights.Add(lightPos);
                    placedLightsObjects.Add(lightObject);
                }
            }
        }

        private bool IsWalkable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= gridData.gridWidth || y >= gridData.gridHeight)
                return false;

            return gridData.cells[x, y].height > 0;
        }

        private int CountWalkableNeighbors(int x, int y)
        {
            int count = 0;

            if (IsWalkable(x + 1, y)) count++;
            if (IsWalkable(x - 1, y)) count++;
            if (IsWalkable(x, y + 1)) count++;
            if (IsWalkable(x, y - 1)) count++;

            return count;
        }

        /// <summary>
        /// A ROOM tile has 3 or 4 walkable neighbors.
        /// </summary>
        private bool IsRoom(int x, int y)
        {
            if (!IsWalkable(x, y)) return false;

            int n = CountWalkableNeighbors(x, y);

            return n >= 3;  // Wide open → room
        }

        /// <summary>
        /// A CORRIDOR tile has exactly 1 or 2 walkable neighbors.
        /// </summary>
        private bool IsCorridor(int x, int y)
        {
            if (!IsWalkable(x, y)) return false;

            int n = CountWalkableNeighbors(x, y);

            return n == 1 || n == 2; // Thin path → corridor
        }

    }
}