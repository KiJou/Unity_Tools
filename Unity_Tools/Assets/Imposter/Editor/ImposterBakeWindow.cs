using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderLib.Imposter
{
    public class ImposterBakeWindow : EditorWindow
    {
        private static string _suffix = "Imposter"; //when creating prefabs

        private static int _atlasResolution = 2048;

        private static readonly string[] ResNames = { "1024", "2048", "4096", "8192" };
        private static readonly int[] ResSizes = { 1024, 2048, 4096, 8192 };

        private static int _frames = 12;
        private static bool _isHalf = true;
        private static Transform _lightingRig;
        private static Transform _root;
        private static readonly List<Transform> Roots = new List<Transform>();
        private static Material _processingMat;

        private static bool _createUnityBillboard = false;
        
        private Mesh _cameraRig;
        private BillboardImposter _imposterAsset;
        private Vector3 _origin;

        [SerializeField] private Shader _processingShader;
        [SerializeField] private Shader _bakeWorldNormalDepthShader;
        [SerializeField] private Shader _bakeAlbedoShader;
        [SerializeField] private ComputeShader _processingCompute;

        private Snapshots[] _snapshots;

        private static GUIContent _labelFrames = new GUIContent("Frames", "Too many frames = low texel density, Too few frames = distortion");
        private static GUIContent _labelHemisphere = new GUIContent("Hemisphere", "Full Sphere or Hemisphere capture, sphere is useful for objects seen at all angles");
        private static GUIContent _labelCustomLighting = new GUIContent("Custom Lighting Root", "transform of custom light rig, lit object rendered in place of albedo");
        private static GUIContent _labelSuffix = new GUIContent("Prefab Suffix", "Appended to Imposter Prefab(s), useful for LOD naming");
        private static GUIContent _labelUnityBillboard = new GUIContent("Create Unity Billboard", "Creates additional Unity Billboard Renderer Asset (WIP)");
        private static GUIContent _labelPreviewSnapshot = new GUIContent("Preview Snapshot Locations", "Draw rays showing camera positions");

        [MenuItem("G2Studios/Imposter", priority = 9000)]
        public static void ShowWindow()
        {
            GetWindow<ImposterBakeWindow>("Imposter Baker");
        }

        private static bool IsEven(int n)
        {
            return n % 2 == 0;
        }

        private void CheckMaterial()
        {
            if (_processingShader == null)
            {
                _processingShader = Shader.Find("Hidden/G2Studios/Imposter/ImposterProcessing");
            }
            if (_processingMat == null && _processingShader != null)
            {
                _processingMat = new Material(_processingShader);
            }

            if (_bakeWorldNormalDepthShader == null)
            {
                _bakeWorldNormalDepthShader = Shader.Find("Hidden/G2Studios/Imposter/ImposterBakeWorldNormalDepth");
            }

            if (_bakeAlbedoShader == null)
            {
                _bakeAlbedoShader = Shader.Find("Hidden/G2Studios/Imposter/ImposterBakeAlbedo");
            }
        }

        private void OnGUI()
        {
            try
            {
                CheckMaterial();
                Draw();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void Draw()
        {
            if (_processingMat == null)
            {
                Shader shader = Shader.Find("Hidden/G2Studios/Imposter/ImposterProcessing");
                if (shader != null)
                {
                    _processingMat = new Material(shader);
                }
                else
                {
                    Debug.LogError("Imposter Baker Material NULL!");
                }
            }
            var noSelection = Selection.activeTransform == null;

            EditorGUI.BeginChangeCheck(); //check for settings change

            _atlasResolution = EditorGUILayout.IntPopup("Resolution", _atlasResolution, ResNames, ResSizes);

            _frames = EditorGUILayout.IntField(_labelFrames, Mathf.Clamp(_frames, 4, 32));

            if (!IsEven(_frames)) _frames -= 1;

            _frames = Mathf.Max(2, _frames);
            _isHalf = EditorGUILayout.Toggle(_labelHemisphere, _isHalf);

            EditorGUILayout.LabelField(_labelCustomLighting);
            _lightingRig = (Transform)EditorGUILayout.ObjectField(_lightingRig, typeof(Transform), true);

            var settingsChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.LabelField(_labelSuffix);
            _suffix = EditorGUILayout.TextField(_suffix);
            _createUnityBillboard = EditorGUILayout.Toggle(_labelUnityBillboard, _createUnityBillboard);

            if (_root != Selection.activeTransform || settingsChanged)
            {
                Roots.Clear();
            }

            _root = Selection.activeTransform;
            if (noSelection)
            {
                return;
            }

            BillboardImposter imposterAsset = null;
            Snapshots[] snapshots = null;

            if (GUILayout.Button(_labelPreviewSnapshot))
            {
                if (SetupRig(_root, out imposterAsset, out snapshots))
                {
                    DebugSnapshots(snapshots, imposterAsset.Radius);
                }
            }

            if (Selection.gameObjects != null && Selection.gameObjects.Length > 1 && Selection.gameObjects.Length != Roots.Count)
            {
                for (var i = 0; i < Selection.gameObjects.Length; i++)
                {
                    if (Selection.gameObjects[i].transform.parent == null)
                    {
                        Roots.Add(Selection.gameObjects[i].transform);
                    }
                }
            }

            if (Roots.Count > 1)
            {
                if (GUILayout.Button("Capture Multiple"))
                {
                    for (var i = 0; i < Roots.Count; i++)
                    {
                        EditorUtility.DisplayProgressBar("Capturing", "Capturing " + Roots[i].name, (i + 1f) / Roots.Count);
                        if (SetupRig(Roots[i], out imposterAsset, out snapshots))
                        {
                            CaptureViews(Roots[i], imposterAsset, snapshots, _lightingRig, _bakeAlbedoShader, _bakeWorldNormalDepthShader, _processingCompute);
                        }
                    }
                    EditorUtility.ClearProgressBar();
                }
            }
            else if (_root != null)
            {
                if (GUILayout.Button("Capture"))
                {
                    EditorUtility.DisplayProgressBar("Capturing", "Capturing " + _root.name, 1f);
                    if (SetupRig(_root, out imposterAsset, out snapshots))
                    {
                        CaptureViews(_root, imposterAsset, snapshots, _lightingRig, _bakeAlbedoShader, _bakeWorldNormalDepthShader, _processingCompute);
                    }
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private static void DebugSnapshots(Snapshots[] snapshots, float rayScale)
        {
            for (var i = 0; i < snapshots.Length; i++)
            {
                Debug.DrawRay(snapshots[i].Position, snapshots[i].Ray * rayScale, Color.green, 0.5f);
            }
        }

        private static bool SetupRig(Transform root, out BillboardImposter imposterAsset, out Snapshots[] snapshots)
        {
            var mrs = root.GetComponentsInChildren<MeshRenderer>();
            imposterAsset = null;
            snapshots = null;
            if (mrs == null || mrs.Length == 0)
            {
                return false;
            }

            imposterAsset = CreateInstance<BillboardImposter>();
            var bounds = new Bounds(root.position, Vector3.zero);
            for (var i = 0; i < mrs.Length; i++)
            {
                if (!mrs[i].enabled)
                {
                    continue;
                }
                var mf = mrs[i].GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertices == null)
                {
                    continue;
                }
                var verts = mf.sharedMesh.vertices;
                for (var v = 0; v < verts.Length; v++)
                {
                    var meshWorldVert = mf.transform.localToWorldMatrix.MultiplyPoint3x4(verts[v]);
                    var meshLocalToRoot = root.worldToLocalMatrix.MultiplyPoint3x4(meshWorldVert);
                    var worldVert = root.localToWorldMatrix.MultiplyPoint3x4(meshLocalToRoot);
                    bounds.Encapsulate(worldVert);
                }
            }

            //the bounds will fit within the sphere
            var radius = Vector3.Distance(bounds.min, bounds.max) * 0.5f;
            imposterAsset.Radius = radius;
            imposterAsset.Frames = _frames;
            imposterAsset.IsHalf = _isHalf;
            imposterAsset.AtlasResolution = _atlasResolution;
            imposterAsset.Offset = bounds.center - root.position;

            Debug.DrawLine(bounds.min, bounds.max, Color.cyan, 1f);
            imposterAsset.AssetReference = (GameObject)PrefabUtility.GetCorrespondingObjectFromSource(root.gameObject);
            snapshots = UpdateSnapshots(_frames, radius, root.position + imposterAsset.Offset, _isHalf);
            DebugSnapshots(snapshots, radius * 0.1f);
            return true;
        }

        private static Snapshots[] UpdateSnapshots(int frames, float radius, Vector3 origin, bool isHalf = true)
        {
            var snapshots = new Snapshots[frames * frames];

            float framesMinusOne = frames - 1;

            var i = 0;
            for (var y = 0; y < frames; y++)
            {
                for (var x = 0; x < frames; x++)
                {
                    var vec = new Vector2(x / framesMinusOne * 2f - 1f, y / framesMinusOne * 2f - 1f);
                    var ray = isHalf ? OctahedralCoordToVectorHemisphere(vec) : OctahedralCoordToVector(vec);

                    ray = ray.normalized;

                    snapshots[i].Position = origin + ray * radius;
                    snapshots[i].Ray = -ray;
                    i++;
                }
            }

            return snapshots;
        }

        private static Vector2 Get2DIndex(int i, int res)
        {
            float x = i % res;
            float y = (i - x) / res;
            return new Vector2(x, y);
        }

        private static bool CaptureViews(
            Transform root, 
            BillboardImposter imposter, 
            Snapshots[] snapshots,
            Transform lightingRoot, 
            Shader albedoBake, 
            Shader normalBake, 
            ComputeShader processCompute)
        {
            Vector3 originalScale = root.localScale;

            root.localScale = Vector3.one;
            var prevRt = RenderTexture.active;
            var baseAtlas = RenderTexture.GetTemporary(_atlasResolution, _atlasResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            baseAtlas.enableRandomWrite = true;
            baseAtlas.Create();

            var packAtlas = RenderTexture.GetTemporary(_atlasResolution, _atlasResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            packAtlas.enableRandomWrite = true;
            packAtlas.Create();
            var tempAtlas = RenderTexture.GetTemporary(baseAtlas.descriptor);
            tempAtlas.Create();

            var frameReso = _atlasResolution / imposter.Frames;
            var frame = RenderTexture.GetTemporary(frameReso, frameReso, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            frame.enableRandomWrite = true;
            frame.Create();

            var packFrame = RenderTexture.GetTemporary(frameReso, frameReso, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            packFrame.Create();

            var tempFrame = RenderTexture.GetTemporary(frame.descriptor);
            tempFrame.Create();

            var frameResUpscale = frameReso * 4;
            var superSizedFrame = RenderTexture.GetTemporary(frameResUpscale, frameResUpscale, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            superSizedFrame.enableRandomWrite = true;
            superSizedFrame.Create();

            var superSizedFrameTemp = RenderTexture.GetTemporary(superSizedFrame.descriptor);
            var superSizedAlphaMask = RenderTexture.GetTemporary(superSizedFrame.descriptor);
            superSizedAlphaMask.Create();

            imposter.BaseTexture = new Texture2D(baseAtlas.width, baseAtlas.height, TextureFormat.ARGB32, true, true);
            imposter.PackTexture = new Texture2D(baseAtlas.width, baseAtlas.height, TextureFormat.ARGB32, true, true);

            ComputeBuffer minDistancesBuffer = new ComputeBuffer(frame.width * frame.height, sizeof(float));
            ComputeBuffer maxDistanceBuffer = new ComputeBuffer(1, sizeof(float));

            const int layer = 30;
            var clearColor = Color.clear;

            var camera = new GameObject().AddComponent<Camera>();
            camera.gameObject.hideFlags = HideFlags.DontSave;
            camera.cullingMask = 1 << layer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = clearColor;
            camera.orthographic = true;
            camera.nearClipPlane = 0f;
            camera.farClipPlane = imposter.Radius * 2f;
            camera.orthographicSize = imposter.Radius;
            camera.allowMSAA = false;
            camera.enabled = false;

            var frameCount = imposter.Frames * imposter.Frames;

            var originalLayers = new Dictionary<GameObject, int>();
            StoreLayers(root, layer, ref originalLayers);

            var originalLights = new Dictionary<Light, bool>();
            var customLit = lightingRoot != null;
            if (customLit)
            {
                var lights = FindObjectsOfType<Light>();
                for (var i = 0; i < lights.Length; i++)
                {
                    if (!lights[i].transform.IsChildOf(lightingRoot))
                    {
                        if (originalLights.ContainsKey(lights[i])) continue;
                        originalLights.Add(lights[i], lights[i].enabled);
                        lights[i].enabled = false;
                    }
                    else
                    {
                        lights[i].enabled = true;
                        if (!originalLights.ContainsKey(lights[i]))
                        {
                            originalLights.Add(lights[i], false);
                        }
                    }
                }
            }

            var tempMinMaxRT = RenderTexture.GetTemporary(frame.width, frame.height, 0, RenderTextureFormat.ARGB32);
            tempMinMaxRT.Create();

            Graphics.SetRenderTarget(tempMinMaxRT);
            GL.Clear(true, true, Color.clear);

            camera.clearFlags = CameraClearFlags.Nothing;
            camera.backgroundColor = clearColor;
            camera.targetTexture = tempMinMaxRT;

            var min = Vector2.one * frame.width;
            var max = Vector2.zero;

            for (var i = 0; i < frameCount; i++)
            {
                if (i > snapshots.Length - 1)
                {
                    Debug.LogError("[Imposter] snapshot data length less than frame count! this shouldn't happen!");
                    continue;
                }

                //position camera with the current snapshot info
                var snap = snapshots[i];
                camera.transform.position = snap.Position;
                camera.transform.rotation = Quaternion.LookRotation(snap.Ray, Vector3.up);

                //render alpha only
                Shader.SetGlobalFloat("_ImposterRenderAlpha", 1f);
                camera.RenderWithShader(albedoBake, "");
                camera.ResetReplacementShader();

                //render without clearing (accumulating filled pixels)
                camera.Render();

                //supply the root position taken into camera space
                //this is for the min max, in the case root is further from opaque pixels
                var viewPos = camera.WorldToViewportPoint(root.position);
                var texPos = new Vector2(viewPos.x, viewPos.y) * frame.width;
                texPos.x = Mathf.Clamp(texPos.x, 0f, frame.width);
                texPos.y = Mathf.Clamp(texPos.y, 0f, frame.width);
                min.x = Mathf.Min(min.x, texPos.x);
                min.y = Mathf.Min(min.y, texPos.y);
                max.x = Mathf.Max(max.x, texPos.x);
                max.y = Mathf.Max(max.y, texPos.y);
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = clearColor;
            camera.targetTexture = null;

            //now read render texture
            var tempMinMaxTex = new Texture2D(tempMinMaxRT.width, tempMinMaxRT.height, TextureFormat.ARGB32, false);
            RenderTexture.active = tempMinMaxRT;
            tempMinMaxTex.ReadPixels(new Rect(0f, 0f, tempMinMaxRT.width, tempMinMaxRT.height), 0, 0);
            tempMinMaxTex.Apply();

            var tempTexC = tempMinMaxTex.GetPixels32();

            //loop pixels get min max
            for (var c = 0; c < tempTexC.Length; c++)
            {
                if (tempTexC[c].r != 0x00)
                {
                    var texPos = Get2DIndex(c, tempMinMaxRT.width);
                    min.x = Mathf.Min(min.x, texPos.x);
                    min.y = Mathf.Min(min.y, texPos.y);
                    max.x = Mathf.Max(max.x, texPos.x);
                    max.y = Mathf.Max(max.y, texPos.y);
                }
            }

            DestroyImmediate(tempMinMaxTex, true);
            RenderTexture.ReleaseTemporary(tempMinMaxRT);

            //rescale radius
            var len = new Vector2(max.x - min.x, max.y - min.y);
            var maxR = Mathf.Max(len.x, len.y);
            var ratio = maxR / frame.width;

            imposter.Radius = imposter.Radius * ratio;
            camera.farClipPlane = imposter.Radius * 2f;
            camera.orthographicSize = imposter.Radius;

            Vector3 scaleFactor = new Vector3(root.localScale.x / originalScale.x, root.localScale.y / originalScale.y, root.localScale.z / originalScale.z);
            imposter.Offset = Vector3.Scale(imposter.Offset, scaleFactor);

            snapshots = UpdateSnapshots(imposter.Frames, imposter.Radius, root.position + imposter.Offset, imposter.IsHalf);


            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                if (frameIndex > snapshots.Length - 1)
                {
                    Debug.LogError("[Imposter] snapshot data length less than frame count! this shouldn't happen!");
                    continue;
                }

                var snap = snapshots[frameIndex];
                camera.transform.position = snap.Position;
                camera.transform.rotation = Quaternion.LookRotation(snap.Ray, Vector3.up);
                clearColor = Color.clear;

                //target and clear base frame
                Graphics.SetRenderTarget(superSizedFrame);
                GL.Clear(true, true, clearColor);
                Graphics.SetRenderTarget(superSizedFrameTemp);
                GL.Clear(true, true, clearColor);

                camera.targetTexture = superSizedFrameTemp;
                camera.backgroundColor = clearColor;

                if (!customLit)
                {
                    Shader.SetGlobalFloat("_ImposterRenderAlpha", 0f);
                    camera.RenderWithShader(albedoBake, "");
                    camera.ResetReplacementShader();
                }
                else
                {
                    camera.Render();
                }

                camera.targetTexture = superSizedAlphaMask;
                camera.backgroundColor = clearColor;
                camera.Render();

                Graphics.Blit(superSizedAlphaMask, superSizedFrame, _processingMat, 3);
                Graphics.Blit(superSizedFrame, superSizedAlphaMask);

                _processingMat.SetTexture("_MainTex", superSizedFrameTemp);
                _processingMat.SetTexture("_MainTex2", superSizedAlphaMask);
                _processingMat.SetFloat("_Step", 1f);

                Graphics.Blit(superSizedFrameTemp, superSizedFrame, _processingMat, 1);

                Graphics.SetRenderTarget(frame);
                GL.Clear(true, true, clearColor);
                Graphics.Blit(superSizedFrame, frame);

                Graphics.SetRenderTarget(superSizedFrameTemp);
                GL.Clear(true, true, clearColor);
                Graphics.SetRenderTarget(superSizedFrame);
                GL.Clear(true, true, clearColor);

                clearColor = new Color(0.0f, 0.0f, 0.0f, 0.5f);
                camera.targetTexture = superSizedFrame;
                camera.backgroundColor = clearColor;
                camera.RenderWithShader(normalBake, "");
                camera.ResetReplacementShader();

                Graphics.SetRenderTarget(packFrame);
                GL.Clear(true, true, clearColor);
                Graphics.Blit(superSizedFrame, packFrame);


                Graphics.SetRenderTarget(tempFrame);
                GL.Clear(true, true, Color.clear);

                int threadsX, threadsY, threadsZ;
                CalcWorkSize(packFrame.width * packFrame.height, out threadsX, out threadsY, out threadsZ);
                processCompute.SetTexture(0, "Source", packFrame);
                processCompute.SetTexture(0, "SourceMask", frame);
                processCompute.SetTexture(0, "Result", tempFrame);
                processCompute.SetBool("AllChannels", true);
                processCompute.SetBool("NormalsDepth", true);
                processCompute.Dispatch(0, threadsX, threadsY, threadsZ);

                Graphics.Blit(tempFrame, packFrame);

                Graphics.SetRenderTarget(tempFrame);
                GL.Clear(true, true, Color.clear);

                CalcWorkSize(frame.width * frame.height, out threadsX, out threadsY, out threadsZ);
                processCompute.SetTexture(0, "Source", frame);
                processCompute.SetTexture(0, "SourceMask", frame);
                processCompute.SetTexture(0, "Result", tempFrame);
                processCompute.SetBool("AllChannels", false);
                processCompute.SetBool("NormalsDepth", false);
                processCompute.Dispatch(0, threadsX, threadsY, threadsZ);

                Graphics.Blit(tempFrame, frame);

                Graphics.SetRenderTarget(tempFrame);
                GL.Clear(true, true, Color.clear);

                CalcWorkSize(frame.width * frame.height, out threadsX, out threadsY, out threadsZ);
                processCompute.SetTexture(1, "Source", frame);
                processCompute.SetTexture(1, "SourceMask", frame);
                processCompute.SetBuffer(1, "MinDistances", minDistancesBuffer);
                processCompute.Dispatch(1, threadsX, threadsY, threadsZ);

                processCompute.SetInt("MinDistancesLength", minDistancesBuffer.count);
                processCompute.SetBuffer(2, "MaxOfMinDistances", maxDistanceBuffer);
                processCompute.SetBuffer(2, "MinDistances", minDistancesBuffer);
                processCompute.Dispatch(2, 1, 1, 1);

                CalcWorkSize(frame.width * frame.height, out threadsX, out threadsY, out threadsZ);
                processCompute.SetTexture(3, "Source", frame);
                processCompute.SetTexture(3, "SourceMask", frame);
                processCompute.SetTexture(3, "Result", tempFrame);
                processCompute.SetBuffer(3, "MinDistances", minDistancesBuffer);
                processCompute.SetBuffer(3, "MaxOfMinDistances", maxDistanceBuffer);
                processCompute.Dispatch(3, threadsX, threadsY, threadsZ);

                Graphics.Blit(tempFrame, frame);

                int x;
                int y;
                XYFromIndex(frameIndex, imposter.Frames, out x, out y);

                x *= frame.width;
                y *= frame.height;

                Graphics.CopyTexture(frame, 0, 0, 0, 0, frame.width, frame.height, baseAtlas, 0, 0, x, y);
                Graphics.CopyTexture(packFrame, 0, 0, 0, 0, packFrame.width, packFrame.height, packAtlas, 0, 0, x, y);
            }

            Graphics.SetRenderTarget(packAtlas);
            imposter.PackTexture.ReadPixels(new Rect(0f, 0f, packAtlas.width, packAtlas.height), 0, 0);

            Graphics.SetRenderTarget(baseAtlas);
            imposter.BaseTexture.ReadPixels(new Rect(0f, 0f, baseAtlas.width, baseAtlas.height), 0, 0);

            RenderTexture.active = prevRt;
            baseAtlas.Release();
            frame.Release();
            packAtlas.Release();
            packFrame.Release();

            RenderTexture.ReleaseTemporary(baseAtlas);
            RenderTexture.ReleaseTemporary(packAtlas);
            RenderTexture.ReleaseTemporary(tempAtlas);

            RenderTexture.ReleaseTemporary(frame);
            RenderTexture.ReleaseTemporary(packFrame);
            RenderTexture.ReleaseTemporary(tempFrame);

            RenderTexture.ReleaseTemporary(superSizedFrame);
            RenderTexture.ReleaseTemporary(superSizedAlphaMask);
            RenderTexture.ReleaseTemporary(superSizedFrameTemp);

            minDistancesBuffer.Dispose();
            maxDistanceBuffer.Dispose();

            DestroyImmediate(camera.gameObject, true);

            //restore layers
            RestoreLayers(originalLayers);

            //restore lights
            var enumerator2 = originalLights.Keys.GetEnumerator();
            while (enumerator2.MoveNext())
            {
                var light = enumerator2.Current;
                if (light != null)
                {
                    light.enabled = originalLights[light];
                }
            }

            enumerator2.Dispose();
            originalLights.Clear();

            var savePath = "";
            var file = "";
            var filePrefab = "";
            if (imposter.AssetReference != null)
            {
                savePath = AssetDatabase.GetAssetPath(imposter.AssetReference);
                var lastSlash = savePath.LastIndexOf("/", StringComparison.Ordinal);
                var folder = savePath.Substring(0, lastSlash);
                file = savePath.Substring(lastSlash + 1, savePath.LastIndexOf(".", StringComparison.Ordinal) - lastSlash - 1);
                filePrefab = file;
                savePath = folder + "/" + file + "_Imposter" + ".asset";
            }
            else //no prefab, ask where to save
            {
                file = root.name;
                savePath = EditorUtility.SaveFilePanelInProject("Save Billboard Imposter", file + "_Imposter", "asset", "Select save location");
            }

            imposter.PrefabSuffix = _suffix;
            imposter.name = file;
            AssetDatabase.CreateAsset(imposter, savePath);

            imposter.Save(savePath, file, _createUnityBillboard);

            //spawn 
            var spawned = imposter.Spawn(root.position, true, filePrefab);
            spawned.transform.position = root.position + new Vector3(2f, 0f, 2f);
            spawned.transform.rotation = root.rotation;
            spawned.transform.localScale = originalScale;

            root.localScale = originalScale;

            return true;
        }

        private static void DrawQuad()
        {
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.QUADS);
            GL.TexCoord2(0.0f, 0.0f);
            GL.Vertex3(-1.0f, -1.0f, 0.0f);
            GL.TexCoord2(1.0f, 0.0f);
            GL.Vertex3(-1.0f, 1.0f, 0.0f);
            GL.TexCoord2(1.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, 0.0f);
            GL.TexCoord2(0.0f, 1.0f);
            GL.Vertex3(1.0f, -1.0f, 0.0f);
            GL.End();
            GL.PopMatrix();
        }

        private static void StoreLayers(Transform root, int layer, ref Dictionary<GameObject, int> store)
        {
            store.Add(root.gameObject, root.gameObject.layer);
            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
            {
                var t = root.GetChild(i);
                StoreLayers(t, layer, ref store);
            }
        }

        private static void RestoreLayers(Dictionary<GameObject, int> store)
        {
            var enumerator = store.Keys.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current != null)
                {
                    enumerator.Current.layer = store[enumerator.Current];
                }
            }
            enumerator.Dispose();
            store.Clear();
        }

        private static Vector3 OctahedralCoordToVectorHemisphere(Vector2 coord)
        {
            coord = new Vector2(coord.x + coord.y, coord.x - coord.y) * 0.5f;
            var vec = new Vector3(
                coord.x,
                1.0f - Vector2.Dot(Vector2.one, new Vector2(Mathf.Abs(coord.x), Mathf.Abs(coord.y))),
                coord.y
            );
            return Vector3.Normalize(vec);
        }

        private static Vector3 OctahedralCoordToVector(Vector2 f)
        {
            var n = new Vector3(f.x, 1f - Mathf.Abs(f.x) - Mathf.Abs(f.y), f.y);
            var t = Mathf.Clamp01(-n.y);
            n.x += n.x >= 0f ? -t : t;
            n.z += n.z >= 0f ? -t : t;
            return n;
        }

        private static void XYFromIndex(int index, int dims, out int x, out int y)
        {
            x = index % dims;
            y = (index - x) / dims;
        }

        private static Mesh CreateOctahedron(int frames, float radius = 1f, bool isHalf = true)
        {
            var verts = frames + 1;
            var lenVerts = verts * verts;
            var lenFrames = frames * frames;

            var vertices = new Vector3[lenVerts];
            var normals = new Vector3[lenVerts];
            var uvs = new Vector2[lenVerts];

            var indices = new int[lenFrames * 2 * 3];
            for (var i = 0; i < lenVerts; i++)
            {
                int x;
                int y;

                XYFromIndex(i, verts, out x, out y);
                var vec = new Vector2(x / (float)frames, y / (float)frames);
                var vecSigned = new Vector2(vec.x * 2f - 1f, vec.y * 2f - 1f);

                uvs[i] = vec;
                var vertex = isHalf ? OctahedralCoordToVectorHemisphere(vecSigned) : OctahedralCoordToVector(vecSigned);

                normals[i] = vertex.normalized;
                vertices[i] = normals[i] * radius;
            }

            for (var i = 0; i < indices.Length / 6; i++)
            {
                int x;
                int y;

                XYFromIndex(i, frames, out x, out y);

                var corner = x + y * verts;
                var v0 = corner;
                var v1 = corner + verts;
                var v2 = corner + 1;
                var v3 = corner + 1 + verts;

                var vec = uvs[corner];
                vec = new Vector2(vec.x * 2f - 1f, vec.y * 2f - 1f);
                var sameSign = Math.Abs(Mathf.Sign(vec.x) - Mathf.Sign(vec.y)) < Mathf.Epsilon;

                if (isHalf)
                {
                    sameSign = !sameSign;
                }

                if (sameSign)
                {
                    // LL / UR
                    // 1---3
                    // | \ |
                    // 0---2
                    indices[i * 6 + 0] = v0;
                    indices[i * 6 + 1] = v1;
                    indices[i * 6 + 2] = v2;
                    indices[i * 6 + 3] = v2;
                    indices[i * 6 + 4] = v1;
                    indices[i * 6 + 5] = v3;
                }
                else
                {
                    // UL / LR
                    // 1---3
                    // | / |
                    // 0---2
                    indices[i * 6 + 0] = v2;
                    indices[i * 6 + 1] = v0;
                    indices[i * 6 + 2] = v3;
                    indices[i * 6 + 3] = v3;
                    indices[i * 6 + 4] = v0;
                    indices[i * 6 + 5] = v1;
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                normals = normals,
                uv = uvs
            };
            mesh.SetTriangles(indices, 0);
            return mesh;
        }

        private struct Snapshots
        {
            public Vector3 Position;
            public Vector3 Ray;
        }

        private const int GROUP_SIZE = 256;
        private const int MAX_DIM_GROUPS = 1024;
        private const int MAX_DIM_THREADS = (GROUP_SIZE * MAX_DIM_GROUPS);

        private static void CalcWorkSize(int length, out int x, out int y, out int z)
        {
            if (length <= MAX_DIM_THREADS)
            {
                x = (length - 1) / GROUP_SIZE + 1;
                y = z = 1;
            }
            else
            {
                x = MAX_DIM_GROUPS;
                y = (length - 1) / MAX_DIM_THREADS + 1;
                z = 1;
            }
        }
    }
}

