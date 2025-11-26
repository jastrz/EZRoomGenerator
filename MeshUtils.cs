using System.Collections.Generic;
using UnityEngine;

namespace EZRoomGen
{
    public static class MeshUtils
    {
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