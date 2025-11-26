using System.Collections.Generic;
using UnityEngine;

namespace EZRoomGen
{
    public class RoomMeshGenerator
    {
        private GridData gridData;
        private float uvScale;
        private int meshResolution;
        private CellWinding cellWinding;
        private Material floorMaterial;
        private Material wallMaterial;
        private Material roofMaterial;
        private bool invertRoof;

        // Slight lift to prevent clipping
        private float roofOffset = 0.0f;

        public RoomMeshGenerator(GridData gridData, float uvScale, int meshResolution, CellWinding cellWinding,
            Material floorMaterial, Material wallMaterial, Material roofMaterial, bool invertRoof)
        {
            this.gridData = gridData;
            this.uvScale = uvScale;
            this.meshResolution = meshResolution;
            this.cellWinding = cellWinding;
            this.floorMaterial = floorMaterial;
            this.wallMaterial = wallMaterial;
            this.roofMaterial = roofMaterial;
            this.invertRoof = invertRoof;
        }

        public GameObject GenerateRoom()
        {
            GameObject roomObject = new GameObject("Generated Room");

            List<Vector3> floorVerts = new List<Vector3>();
            List<int> floorTris = new List<int>();
            List<Vector2> floorUVs = new List<Vector2>();

            List<Vector3> wallVerts = new List<Vector3>();
            List<int> wallTris = new List<int>();
            List<Vector2> wallUVs = new List<Vector2>();

            List<Vector3> roofVerts = new List<Vector3>();
            List<int> roofTris = new List<int>();
            List<Vector2> roofUVs = new List<Vector2>();

            for (int y = 0; y < gridData.gridHeight; y++)
            {
                for (int x = 0; x < gridData.gridWidth; x++)
                {
                    float height = gridData.cells[x, y].height;
                    if (height <= 0) continue;

                    bool flipped = cellWinding == CellWinding.Flipped;
                    bool doubleSided = cellWinding == CellWinding.DoubleSided;

                    // FLOOR
                    MeshUtils.AddSubdividedQuad(
                        floorVerts, floorTris, floorUVs,
                        new Vector3(x, 0, y),
                        new Vector3(x + 1, 0, y),
                        new Vector3(x + 1, 0, y + 1),
                        new Vector3(x, 0, y + 1),
                        flipped, doubleSided, uvScale, meshResolution
                    );

                    // CEILING 
                    if (!invertRoof)
                    {
                        MeshUtils.AddSubdividedQuad(
                            roofVerts, roofTris, roofUVs,
                            new Vector3(x, height, y),
                            new Vector3(x + 1, height, y),
                            new Vector3(x + 1, height, y + 1),
                            new Vector3(x, height, y + 1),
                            !flipped, doubleSided, uvScale, meshResolution
                        );
                    }

                    // WALLS
                    float leftHeight = x > 0 ? gridData.cells[x - 1, y].height : 0;
                    float rightHeight = x < gridData.gridWidth - 1 ? gridData.cells[x + 1, y].height : 0;
                    float backHeight = y > 0 ? gridData.cells[x, y - 1].height : 0;
                    float frontHeight = y < gridData.gridHeight - 1 ? gridData.cells[x, y + 1].height : 0;

                    if (leftHeight < height)
                    {
                        MeshUtils.AddSubdividedQuad(
                            wallVerts, wallTris, wallUVs,
                            new Vector3(x, leftHeight, y),
                            new Vector3(x, leftHeight, y + 1),
                            new Vector3(x, height, y + 1),
                            new Vector3(x, height, y),
                            flipped, doubleSided, uvScale, meshResolution
                        );
                    }

                    if (rightHeight < height)
                    {
                        MeshUtils.AddSubdividedQuad(
                            wallVerts, wallTris, wallUVs,
                            new Vector3(x + 1, rightHeight, y + 1),
                            new Vector3(x + 1, rightHeight, y),
                            new Vector3(x + 1, height, y),
                            new Vector3(x + 1, height, y + 1),
                            flipped, doubleSided, uvScale, meshResolution
                        );
                    }

                    if (backHeight < height)
                    {
                        MeshUtils.AddSubdividedQuad(
                            wallVerts, wallTris, wallUVs,
                            new Vector3(x + 1, backHeight, y),
                            new Vector3(x, backHeight, y),
                            new Vector3(x, height, y),
                            new Vector3(x + 1, height, y),
                            flipped, doubleSided, uvScale, meshResolution
                        );
                    }

                    if (frontHeight < height)
                    {
                        MeshUtils.AddSubdividedQuad(
                            wallVerts, wallTris, wallUVs,
                            new Vector3(x, frontHeight, y + 1),
                            new Vector3(x + 1, frontHeight, y + 1),
                            new Vector3(x + 1, height, y + 1),
                            new Vector3(x, height, y + 1),
                            flipped, doubleSided, uvScale, meshResolution
                        );
                    }
                }
            }

            // INVERTED ROOF
            if (invertRoof)
            {
                CreateExteriorRoof(roofVerts, roofTris, roofUVs);
            }

            Material defaultMaterial = GetDefaultMaterial();

            MeshUtils.CreateMeshObject("Floor", floorVerts, floorTris, floorUVs,
                floorMaterial != null ? floorMaterial : defaultMaterial, roomObject);

            MeshUtils.CreateMeshObject("Walls", wallVerts, wallTris, wallUVs,
                wallMaterial != null ? wallMaterial : defaultMaterial, roomObject);

            MeshUtils.CreateMeshObject("Roof", roofVerts, roofTris, roofUVs,
                roofMaterial != null ? roofMaterial : defaultMaterial, roomObject);

            return roomObject;
        }

        // Inverted roof
        private void CreateExteriorRoof(List<Vector3> verts, List<int> tris, List<Vector2> uvs)
        {
            float maxHeight = 0;

            // Find max height
            for (int y = 0; y < gridData.gridHeight; y++)
            {
                for (int x = 0; x < gridData.gridWidth; x++)
                {
                    float h = gridData.cells[x, y].height;
                    if (h > maxHeight) maxHeight = h;
                }
            }

            float roofHeight = maxHeight + roofOffset;

            // Generate roof only on cells that have height == 0
            for (int y = 0; y < gridData.gridHeight; y++)
            {
                for (int x = 0; x < gridData.gridWidth; x++)
                {
                    if (gridData.cells[x, y].height != 0)
                        continue; // skip solid tiles

                    MeshUtils.AddSubdividedQuad(
                        verts, tris, uvs,
                        new Vector3(x, roofHeight, y),
                        new Vector3(x + 1, roofHeight, y),
                        new Vector3(x + 1, roofHeight, y + 1),
                        new Vector3(x, roofHeight, y + 1),
                        false, false, uvScale, meshResolution
                    );
                }
            }
        }

        private Material GetDefaultMaterial()
        {
            Shader hdrpShader = Shader.Find("HDRP/Lit");
            if (hdrpShader != null)
                return new Material(hdrpShader);

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader != null)
                return new Material(urpShader);

            return new Material(Shader.Find("Standard"));
        }
    }
}
