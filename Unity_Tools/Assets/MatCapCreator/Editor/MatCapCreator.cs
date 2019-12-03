using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ShaderLib.MatCap
{
    public class MatCapCreator
    {

        private const float SPHERE_RADIUS = 0.5f;
        private const int SPHERE_LONGITUDE = 96;
        private const int SPHERE_LATITUDE = 64;

        // この値(0～1)が大きいほどSphereがカメラからはみ出して描画されるため
        // 精度は落ちるがエッジの部分のサンプリング色が不自然にならなくなる
        private const float SPHERE_SIZE_FACTOR = 0.25f;

        private static Vector3[] vertices = new Vector3[(SPHERE_LONGITUDE + 1) * SPHERE_LATITUDE + 2];

        [MenuItem("G2Studios/MatCap/Create MatCap 128x128")]
        public static void CreateMatCap128()
        {
            CreateMatCap(128);
        }

        [MenuItem("G2Studios/MatCap/Create MatCap 256x256")]
        public static void CreateMatCap256()
        {
            CreateMatCap(256);
        }

        [MenuItem("G2Studios/MatCap/Create MatCap 512x512")]
        public static void CreateMatCap512()
        {
            CreateMatCap(512);
        }

        [MenuItem("G2Studios/MatCap/Create MatCap 1024x1024")]
        public static void CreateMatCap1024()
        {
            CreateMatCap(1024);
        }

        private static void CreateMatCap(int px)
        {
            var selected = Selection.activeObject;
            if (!(selected is Material))
            {
                Debug.LogError("Materialにのみ有効です");
                return;
            }

            // カメラを配置
            var cameraGo = new GameObject("MatCap Capture Camera");
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.transform.position = new Vector3(0, 0, -10);
            cameraGo.transform.rotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = 0.5f * Mathf.Lerp(0.99f, 1.0f, SPHERE_SIZE_FACTOR);
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.Color;

            // Sphereを配置
            var targetGo = new GameObject();
            var meshRenderer = targetGo.AddComponent<MeshRenderer>();
            var meshFilter = targetGo.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateSphereMesh();
            meshRenderer.material = selected as Material;
            targetGo.transform.position = Vector3.zero;

            // RenderTextureにレンダリング
            var rt = RenderTexture.GetTemporary(px, px);
            camera.targetTexture = rt;
            camera.Render();

            // RenderTextureの内容をpngに焼きこむ
            var currentRT = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(px, px, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, px, px), 0, 0);
            texture.Apply();
            RenderTexture.active = currentRT;

            // pngを保存する
            var filePath = EditorUtility.SaveFilePanel("Save", "Assets", "matcap", "png");
            if (!string.IsNullOrEmpty(filePath))
            {
                System.IO.File.WriteAllBytes(filePath, texture.EncodeToPNG());
                AssetDatabase.Refresh();
            }

            // 削除・解放処理
            GameObject.DestroyImmediate(targetGo);
            GameObject.DestroyImmediate(cameraGo);
            RenderTexture.ReleaseTemporary(rt);
        }

        /// <summary>
        /// SphereのMeshを生成する
        /// </summary>
        private static Mesh CreateSphereMesh()
        {
            // Vertices
            vertices[0] = Vector3.up * SPHERE_RADIUS;
            for (int lat = 0; lat < SPHERE_LATITUDE; lat++)
            {
                float a1 = Mathf.PI * (float)(lat + 1) / (SPHERE_LATITUDE + 1);
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);

                for (int lon = 0; lon <= SPHERE_LONGITUDE; lon++)
                {
                    float a2 = Mathf.PI * 2.0f * (float)(lon == SPHERE_LONGITUDE ? 0 : lon) / SPHERE_LONGITUDE;
                    float sin2 = Mathf.Sin(a2);
                    float cos2 = Mathf.Cos(a2);

                    vertices[lon + lat * (SPHERE_LONGITUDE + 1) + 1] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * SPHERE_RADIUS;
                }
            }
            vertices[vertices.Length - 1] = Vector3.up * -SPHERE_RADIUS;

            // Normals
            Vector3[] normales = new Vector3[vertices.Length];
            for (int n = 0; n < vertices.Length; n++)
            {
                normales[n] = vertices[n].normalized;
            }

            // UVs
            Vector2[] uvs = new Vector2[vertices.Length];
            uvs[0] = Vector2.up;
            uvs[uvs.Length - 1] = Vector2.zero;
            for (int lat = 0; lat < SPHERE_LATITUDE; lat++)
            {
                for (int lon = 0; lon <= SPHERE_LONGITUDE; lon++)
                {
                    uvs[lon + lat * (SPHERE_LONGITUDE + 1) + 1] = new Vector2(
                        (float)lon / SPHERE_LONGITUDE, 
                        1f - (float)(lat + 1) / (SPHERE_LATITUDE + 1));
                }
            }

            // Triangles
            int[] triangles = new int[vertices.Length * 2 * 3];
            //Top
            int i = 0;
            for (int lon = 0; lon < SPHERE_LONGITUDE; lon++)
            {
                triangles[i++] = lon + 2;
                triangles[i++] = lon + 1;
                triangles[i++] = 0;
            }
            //Middle
            for (int lat = 0; lat < SPHERE_LATITUDE - 1; lat++)
            {
                for (int lon = 0; lon < SPHERE_LONGITUDE; lon++)
                {
                    int current = lon + lat * (SPHERE_LONGITUDE + 1) + 1;
                    int next = current + SPHERE_LONGITUDE + 1;

                    triangles[i++] = current;
                    triangles[i++] = current + 1;
                    triangles[i++] = next + 1;

                    triangles[i++] = current;
                    triangles[i++] = next + 1;
                    triangles[i++] = next;
                }
            }
            //Bottom
            for (int lon = 0; lon < SPHERE_LONGITUDE; lon++)
            {
                triangles[i++] = vertices.Length - 1;
                triangles[i++] = vertices.Length - (lon + 2) - 1;
                triangles[i++] = vertices.Length - (lon + 1) - 1;
            }

            // Create Mesh
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.normals = normales;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }

}

