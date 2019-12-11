using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShaderLib.Imposter
{

    public class BillboardImposter : ScriptableObject
    {
#if UNITY_EDITOR
        public GameObject AssetReference;
#endif
        public int AtlasResolution;

        public Texture2D BaseTexture;

        public BillboardAsset BillboardAsset;

        public int Frames;
        public bool IsHalf;
        public Material Material;
        public Material BillboardRendererMaterial;
        public Mesh Mesh;
        public Vector3 Offset;
        public Texture2D PackTexture;
        public GameObject Prefab;
        public string PrefabSuffix;
        public float Radius;

#if UNITY_EDITOR
        private Mesh MeshSetup()
        {
            var vertices = new[]
            {
                new Vector3(0f, 0.0f, 0f),
                new Vector3(-0.5f, 0.0f, -0.5f),
                new Vector3(0.5f, 0.0f, -0.5f),
                new Vector3(0.5f, 0.0f, 0.5f),
                new Vector3(-0.5f, 0.0f, 0.5f)
            };

            var triangles = new[]
            {
                2, 1, 0,
                3, 2, 0,
                4, 3, 0,
                1, 4, 0
            };

            var uv = new[]
            {
                new Vector2(0.5f, 0.5f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(0.0f, 1.0f)
            };

            var normals = new[]
            {
                new Vector3(0f, 1f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(0f, 1f, 0f)
            };

            var mesh = new Mesh
            {
                vertices = vertices,
                uv = uv,
                normals = normals,
                tangents = new Vector4[5]
            };
            mesh.SetTriangles(triangles, 0);
            mesh.bounds = new Bounds(Vector3.zero + Offset, Vector3.one * Radius * 2f);
            mesh.RecalculateTangents();
            return mesh;
        }

        private static string WriteTexture(Texture2D tex, string path, string name)
        {
            var bytes = tex.EncodeToPNG();

            var fullPath = path + "/" + name + "_" + tex.name + ".png";
            File.WriteAllBytes(fullPath, bytes);

            DestroyImmediate(tex, true);

            return fullPath;
        }

        public void Save(string assetPath, string assetName, bool createBillboardAsset = false)
        {
            var lastSlash = assetPath.LastIndexOf("/", StringComparison.Ordinal);

            var folder = assetPath.Substring(0, lastSlash);

            EditorUtility.SetDirty(this);

            BaseTexture.name = "ImposterBase";
            var baseTexPath = WriteTexture(BaseTexture, folder, assetName);

            PackTexture.name = "ImposterPack";
            var normTexPath = WriteTexture(PackTexture, folder, assetName);

            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(baseTexPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.maxTextureSize = AtlasResolution;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
                BaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexPath);
            }

            importer = AssetImporter.GetAtPath(normTexPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.maxTextureSize = AtlasResolution;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
                PackTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normTexPath);
            }

            var shader = Shader.Find("G2Studios/Imposter/Standard");
            Material = new Material(shader);
            Material.SetTexture("_ImposterBaseTex", BaseTexture);
            Material.SetTexture("_ImposterWorldNormalDepthTex", PackTexture);
            Material.SetFloat("_ImposterFrames", Frames);
            Material.SetFloat("_ImposterSize", Radius);
            Material.SetVector("_ImposterOffset", Offset);
            Material.SetFloat("_ImposterFullSphere", IsHalf ? 0f : 1f);
            Material.name = assetName;
            EditorUtility.SetDirty(Material);

            //create material
            AssetDatabase.CreateAsset(Material, folder + "/" + assetName + "_Imposter_Mat.mat");

            //mesh (not for billboardAsset)
            Mesh = MeshSetup();
            Mesh.name = "ImposterQuad_" + Radius.ToString("F1");
            AssetDatabase.AddObjectToAsset(Mesh, assetPath);

            if (createBillboardAsset)
            {
                BillboardAsset = CreateUnityBillboardAsset(folder, assetName, assetPath);
            }

            AssetDatabase.SaveAssets();
        }


        private GameObject CreatePrefab(bool destroyAfterSpawn = false, string prefName = "")
        {
            var assetPath = AssetDatabase.GetAssetPath(this);
            var folder = assetPath.Substring(0, assetPath.LastIndexOf("/", StringComparison.Ordinal));

            if (string.IsNullOrEmpty(prefName))
            {
                prefName = name;
            }

            if (PrefabSuffix != string.Empty)
            {
                prefName = prefName + "_" + PrefabSuffix;
            }

            var go = new GameObject(prefName);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = Mesh;
            mr.sharedMaterial = Material;

            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            //try to get existing
            var prefabPath = folder + "/" + prefName + ".prefab";
            var existing = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));

            Prefab = existing != null ? 
                PrefabUtility.ReplacePrefab(go, existing, ReplacePrefabOptions.Default) : 
                PrefabUtility.CreatePrefab(prefabPath, go, ReplacePrefabOptions.Default);

            EditorUtility.SetDirty(Prefab);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            if (!destroyAfterSpawn)
            {
                return go;
            }
            DestroyImmediate(go, true);
            return null;
        }

        public GameObject Spawn(Vector3 pos, bool createNew = false, string prefabName = "")
        {
            if (Prefab == null || createNew)
            {
                CreatePrefab(true, prefabName);
            }
            return (GameObject)PrefabUtility.InstantiatePrefab(Prefab);
        }

        private BillboardAsset CreateUnityBillboardAsset(string folder, string assetName, string assetPath)
        {
            var shaderBillboard = Shader.Find("G2Studios/Imposter/UnityBillboard");
            BillboardRendererMaterial = new Material(shaderBillboard);
            BillboardRendererMaterial.SetTexture("_ImposterBaseTex", BaseTexture);
            BillboardRendererMaterial.SetTexture("_ImposterWorldNormalDepthTex", PackTexture);

            BillboardRendererMaterial.SetFloat("_ImposterFrames", Frames);
            BillboardRendererMaterial.SetFloat("_ImposterSize", Radius);
            BillboardRendererMaterial.SetVector("_ImposterOffset", Offset);
            BillboardRendererMaterial.SetFloat("_ImposterFullSphere", IsHalf ? 0f : 1f);
            BillboardRendererMaterial.name = assetName + "_BR";
            EditorUtility.SetDirty(BillboardRendererMaterial);

            AssetDatabase.CreateAsset(BillboardRendererMaterial, folder + "/" + assetName + "_Imposter_BillboardMat.mat");
            BillboardAsset = new BillboardAsset
            {
                material = BillboardRendererMaterial
            };

            BillboardAsset.SetVertices(new Vector2[]
            {
                new Vector2(0.5f, 0.5f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(0.0f, 1.0f)
            });
            BillboardAsset.SetIndices(new ushort[]
            {
                2, 1, 0,
                3, 2, 0,
                4, 3, 0,
                1, 4, 0
            });
            BillboardAsset.SetImageTexCoords(new Vector4[]
            {
                new Vector4(0.5f, 0.5f, 0f, 0f),
                new Vector4(0.0f, 0.0f, 0f, 0f),
                new Vector4(1.0f, 0.0f, 0f, 0f),
                new Vector4(1.0f, 1.0f, 0f, 0f),
                new Vector4(0.0f, 1.0f, 0f, 0f)
            });

            BillboardAsset.width = Radius;
            BillboardAsset.height = Radius;
            BillboardAsset.bottom = Radius * 0.5f;
            BillboardAsset.name = "BillboardAsset";
            EditorUtility.SetDirty(BillboardAsset);
            AssetDatabase.AddObjectToAsset(BillboardAsset, assetPath);
            return BillboardAsset;
        }
#endif
    }

}

