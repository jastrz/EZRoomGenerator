using System.Collections.Generic;
using EZRoomGen.Core.Utils;
using UnityEngine;

namespace EZRoomGen.Core
{
    /// <summary>
    /// Generates procedural room meshes from grid data.
    /// Creates separate mesh objects for floors, walls, and roofs with configurable materials and subdivision.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the RoomMeshGenerator class.
        /// </summary>
        /// <param name="gridData">The grid data containing cell heights and wall information.</param>
        /// <param name="uvScale">Scale factor for UV coordinates.</param>
        /// <param name="meshResolution">Number of subdivisions per mesh quad side.</param>
        /// <param name="cellWinding">Triangle winding order mode (normal, flipped, or double-sided).</param>
        /// <param name="floorMaterial">Material to apply to floor meshes. Uses default if null.</param>
        /// <param name="wallMaterial">Material to apply to wall meshes. Uses default if null.</param>
        /// <param name="roofMaterial">Material to apply to roof meshes. Uses default if null.</param>
        /// <param name="invertRoof">If true, creates inverted roof covering empty cells instead of room tiles.</param>
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

        /// <summary>
        /// Generates the complete room as a GameObject hierarchy with separate child meshes for floor, walls, and roof.
        /// Walls are only created where there's a height difference between adjacent cells or at grid boundaries.
        /// </summary>
        /// <returns>A new GameObject containing the generated room meshes as children.</returns>
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

                    // Floor
                    MeshUtils.AddSubdividedQuad(
                        floorVerts, floorTris, floorUVs,
                        new Vector3(x, 0, y),
                        new Vector3(x + 1, 0, y),
                        new Vector3(x + 1, 0, y + 1),
                        new Vector3(x, 0, y + 1),
                        flipped, doubleSided, uvScale, meshResolution
                    );

                    // Ceiling 
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

                    // Walls
                    float leftHeight = x > 0 ? gridData.cells[x - 1, y].height : 0;
                    float rightHeight = x < gridData.gridWidth - 1 ? gridData.cells[x + 1, y].height : 0;
                    float backHeight = y > 0 ? gridData.cells[x, y - 1].height : 0;
                    float frontHeight = y < gridData.gridHeight - 1 ? gridData.cells[x, y + 1].height : 0;

                    // Left Wall (at x, facing negative X)
                    if (leftHeight < height && gridData.cells[x, y].leftWall)
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

                    // Right Wall (at x+1, facing positive X)
                    if (rightHeight < height && gridData.cells[x, y].rightWall)
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

                    // Back Wall (at y, facing negative Y)
                    if (backHeight < height && gridData.cells[x, y].backWall)
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

                    // Front Wall (at y+1, facing positive Y)
                    if (frontHeight < height && gridData.cells[x, y].frontWall)
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

        /// <summary>
        /// Creates an inverted roof that covers empty (height == 0) cells instead of room cells.
        /// The roof is placed at the maximum height found in the grid.
        /// </summary>
        /// <param name="verts">List to append roof vertex positions to.</param>
        /// <param name="tris">List to append roof triangle indices to.</param>
        /// <param name="uvs">List to append roof UV coordinates to.</param>
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

            float roofHeight = maxHeight;

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

        /// <summary>
        /// Gets a default material appropriate for the current render pipeline.
        /// </summary>
        /// <returns>A new Material instance using a pipeline-appropriate shader.</returns>
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
