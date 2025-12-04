using System.Collections.Generic;
using UnityEngine;

namespace EZRoomGen.Core.Utils
{
    /// <summary>
    /// Utility class for procedural mesh generation and manipulation.
    /// Provides methods for creating subdivided quads and assembling mesh objects.
    /// </summary>
    public static class MeshUtils
    {
        /// <summary>
        /// Adds a subdivided quad to the provided mesh data lists.
        /// The quad is divided into a grid of smaller quads based on meshResolution.
        /// </summary>
        /// <param name="verts">List to append vertex positions to.</param>
        /// <param name="tris">List to append triangle indices to.</param>
        /// <param name="uvs">List to append UV coordinates to.</param>
        /// <param name="v0">Bottom-left corner of the quad.</param>
        /// <param name="v1">Bottom-right corner of the quad.</param>
        /// <param name="v2">Top-right corner of the quad.</param>
        /// <param name="v3">Top-left corner of the quad.</param>
        /// <param name="flip">If true, flips the triangle winding order.</param>
        /// <param name="doubleSided">If true, creates back-facing triangles as well (default: true).</param>
        /// <param name="uvScale">Scale factor for UV coordinates (default: 1f).</param>
        /// <param name="meshResolution">Number of subdivisions per side (default: 2).</param>
        public static void AddSubdividedQuad(
            List<Vector3> verts,
            List<int> tris,
            List<Vector2> uvs,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
            bool flip, bool doubleSided = true,
            float uvScale = 1f,
            int meshResolution = 2)
        {
            int subdiv = meshResolution;
            float width = (v1 - v0).magnitude;
            float height = (v3 - v0).magnitude;

            Vector3 normal = Vector3.Cross(v1 - v0, v3 - v0).normalized;

            for (int row = 0; row < subdiv; row++)
            {
                for (int col = 0; col < subdiv; col++)
                {
                    float u0 = (float)col / subdiv, u1 = (float)(col + 1) / subdiv;
                    float v0f = (float)row / subdiv, v1f = (float)(row + 1) / subdiv;

                    Vector3 p0 = Vector3.Lerp(Vector3.Lerp(v0, v1, u0), Vector3.Lerp(v3, v2, u0), v0f);
                    Vector3 p1 = Vector3.Lerp(Vector3.Lerp(v0, v1, u1), Vector3.Lerp(v3, v2, u1), v0f);
                    Vector3 p2 = Vector3.Lerp(Vector3.Lerp(v0, v1, u1), Vector3.Lerp(v3, v2, u1), v1f);
                    Vector3 p3 = Vector3.Lerp(Vector3.Lerp(v0, v1, u0), Vector3.Lerp(v3, v2, u0), v1f);

                    int idx = verts.Count;

                    verts.Add(p0);
                    verts.Add(p1);
                    verts.Add(p2);
                    verts.Add(p3);

                    uvs.Add(new Vector2(u0 * width * uvScale, v0f * height * uvScale));
                    uvs.Add(new Vector2(u1 * width * uvScale, v0f * height * uvScale));
                    uvs.Add(new Vector2(u1 * width * uvScale, v1f * height * uvScale));
                    uvs.Add(new Vector2(u0 * width * uvScale, v1f * height * uvScale));

                    // Front face triangles
                    if (flip)
                    {
                        tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
                        tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
                    }
                    else
                    {
                        tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 1);
                        tris.Add(idx); tris.Add(idx + 3); tris.Add(idx + 2);
                    }

                    // Double-sided (duplicate verts and triangles for the back face)
                    if (doubleSided)
                    {
                        int idx2 = verts.Count;
                        float offset = 0f;

                        verts.Add(p0 + offset * normal);
                        verts.Add(p1 + offset * normal);
                        verts.Add(p2 + offset * normal);
                        verts.Add(p3 + offset * normal);

                        uvs.Add(new Vector2(u0 * width * uvScale, v0f * height * uvScale));
                        uvs.Add(new Vector2(u1 * width * uvScale, v0f * height * uvScale));
                        uvs.Add(new Vector2(u1 * width * uvScale, v1f * height * uvScale));
                        uvs.Add(new Vector2(u0 * width * uvScale, v1f * height * uvScale));

                        // Reverse winding
                        if (flip)
                        {
                            tris.Add(idx2); tris.Add(idx2 + 2); tris.Add(idx2 + 1);
                            tris.Add(idx2); tris.Add(idx2 + 3); tris.Add(idx2 + 2);
                        }
                        else
                        {
                            tris.Add(idx2); tris.Add(idx2 + 1); tris.Add(idx2 + 2);
                            tris.Add(idx2); tris.Add(idx2 + 2); tris.Add(idx2 + 3);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new GameObject with a mesh constructed from the provided vertex, triangle, and UV data.
        /// Automatically recalculates normals and bounds. Does nothing if vertices list is empty.
        /// </summary>
        /// <param name="name">Name for the created GameObject.</param>
        /// <param name="vertices">List of vertex positions.</param>
        /// <param name="triangles">List of triangle indices.</param>
        /// <param name="uvs">List of UV coordinates.</param>
        /// <param name="mat">Material to apply to the mesh renderer.</param>
        /// <param name="parent">Parent GameObject to attach the new object to.</param>
        public static void CreateMeshObject(string name, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Material mat, GameObject parent)
        {
            if (vertices.Count == 0) return;

            GameObject obj = new GameObject(name);
            obj.transform.parent = parent.transform;

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.mesh = mesh;
            mr.material = mat;
        }
    }
}