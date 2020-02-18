using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderLib.ShadowMapping
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class ShadowMapController : MonoBehaviour
    {
        public enum ShadowType
        {
            NONE,
            HARD,
            VARIANCE
        }

        private Shader depthShader;

        [SerializeField]
        private int resolution = 512;

        [SerializeField][Range(0, 1)]
        private float varianceShadowExpansion = 0.3f;

        [SerializeField]
        private ShadowType shadowType = ShadowType.HARD;

        [SerializeField]
        private FilterMode filterMode = FilterMode.Bilinear;

        public LayerMask casterLayer;

        private Camera shadowCamera;
        private RenderTexture targetTexture;
        private List<Graphic.ShadowMapReceiver> shadowMapReceiverList = new List<Graphic.ShadowMapReceiver>();



        private void OnEnable()
        {
            SetUpShadowCam();
        }

        private void OnDisable()
        {
            if (this.shadowCamera)
            {
                DestroyImmediate(this.shadowCamera.gameObject);
                this.shadowCamera = null;
            }
            if (this.targetTexture)
            {
                DestroyImmediate(this.targetTexture);
                this.targetTexture = null;
            }
            ForAllKeywords(s => Shader.DisableKeyword(ToKeyword(s)));
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        public void SetUpShadowCam()
        {
            if (this.shadowCamera)
            {
                return;
            }
            GameObject go = new GameObject("ShadowCamera");
            go.hideFlags = HideFlags.DontSave;

            this.shadowCamera = go.AddComponent<Camera>();
            this.shadowCamera.orthographic = true;
            this.shadowCamera.nearClipPlane = 0;
            this.shadowCamera.enabled = false;
            this.shadowCamera.backgroundColor = Color.black;
            this.shadowCamera.clearFlags = CameraClearFlags.SolidColor;
            this.shadowCamera.depth = -2;
            this.shadowCamera.renderingPath = RenderingPath.Forward;
            this.shadowCamera.depthTextureMode = DepthTextureMode.Depth;
            this.shadowCamera.cullingMask = this.casterLayer;
        }

        private void Update()
        {
            this.depthShader = this.depthShader ? this.depthShader : Shader.Find("G2Studios/Shadow/ShadowMap");

            UpdateRenderTexture();
            UpdateShadowCameraPos();
            UpdateShaderValues();

            this.shadowCamera.targetTexture = this.targetTexture;
            this.shadowCamera.RenderWithShader(this.depthShader, "RenderType");
        }

        private void UpdateShaderValues()
        {
            if (this.shadowCamera == null)
            {
                return;
            }

            ForAllKeywords(s => Shader.DisableKeyword(ToKeyword(s)));
            Shader.EnableKeyword(ToKeyword(this.shadowType));

            var worldToView = this.shadowCamera.worldToCameraMatrix;
            var localToView = this.shadowCamera.transform.worldToLocalMatrix;
            var projection = GL.GetGPUProjectionMatrix(this.shadowCamera.projectionMatrix, false);
            var lightVP = projection * worldToView;
            var biasMat = new Matrix4x4();
            biasMat.SetRow(0, new Vector4(0.5f, 0.0f, 0.0f, 0.5f));
            biasMat.SetRow(1, new Vector4(0.0f, 0.5f, 0.0f, 0.5f));
            biasMat.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
            biasMat.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            Shader.SetGlobalTexture("_ShadowTex", this.targetTexture);
            Shader.SetGlobalMatrix("_LightMatrix", localToView);
            Shader.SetGlobalMatrix("_LightVP", lightVP);
            Shader.SetGlobalFloat("_VarianceShadowExpansion", this.varianceShadowExpansion);

            Vector4 size = Vector4.zero;
            size.y = this.shadowCamera.orthographicSize * 2;
            size.x = this.shadowCamera.aspect * size.y;
            size.z = this.shadowCamera.farClipPlane;
            size.w = 1.0f / this.resolution;
            Shader.SetGlobalVector("_ShadowTexScale", size);
        }

        private void UpdateRenderTexture()
        {
            if (this.targetTexture != null && 
                (this.targetTexture.width != this.resolution || this.targetTexture.filterMode != this.filterMode))
            {
                DestroyImmediate(this.targetTexture);
                this.targetTexture = null;
            }

            if (this.targetTexture == null)
            {
                this.targetTexture = CreateTargetTexture();
            }
        }

        private void UpdateShadowCameraPos()
        {
            if (this.shadowCamera == null)
            {
                return;
            }
            Light l = FindObjectOfType<Light>();
            var trans = this.shadowCamera.transform;
            trans.position = l.transform.position;
            trans.rotation = l.transform.rotation;
            trans.LookAt(trans.position + trans.forward, trans.up);

            Vector3 center, extents;
            List<Renderer> renderers = new List<Renderer>();
            renderers.AddRange(FindObjectsOfType<Renderer>());
            GetRenderersExtents(renderers, trans, out center, out extents);
            center.z -= extents.z / 2;
            trans.position = trans.TransformPoint(center);
            this.shadowCamera.nearClipPlane = 0;
            this.shadowCamera.farClipPlane = extents.z;
            this.shadowCamera.aspect = extents.x / extents.y;
            this.shadowCamera.orthographicSize = extents.y / 2;
        }

        private RenderTexture CreateTargetTexture()
        {
            RenderTexture rt = new RenderTexture(this.resolution, this.resolution, 16, RenderTextureFormat.ARGB32);
            rt.filterMode = this.filterMode;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }

        private void ForAllKeywords(Action<ShadowType> func)
        {
            func(ShadowType.HARD);
            func(ShadowType.VARIANCE);
        }

        private string ToKeyword(ShadowType shadowType)
        {
            if (shadowType == ShadowType.HARD)
            {
                return "HARD_SHADOWS";
            }
            if (shadowType == ShadowType.VARIANCE)
            {
                return "VARIANCE_SHADOWS";
            }
            return "";
        }

        private void GetRenderersExtents(List<Renderer> renderers, Transform frame, out Vector3 center, out Vector3 extents)
        {
            Vector3[] arr = new Vector3[8];
            Vector3 min = Vector3.one * Mathf.Infinity;
            Vector3 max = Vector3.one * Mathf.NegativeInfinity;
            foreach (var renderer in renderers)
            {
                GetBoundsPoints(renderer.bounds, arr, frame.worldToLocalMatrix);

                foreach (var p in arr)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        min[i] = Mathf.Min(p[i], min[i]);
                        max[i] = Mathf.Max(p[i], max[i]);
                    }
                }
            }
            extents = max - min;
            center = (max + min) / 2;
        }

        private void GetBoundsPoints(Bounds b, Vector3[] points, System.Nullable<Matrix4x4> mat = null)
        {
            Matrix4x4 trans = mat ?? Matrix4x4.identity;
            int count = 0;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 v = b.extents;
                        v.x *= x;
                        v.y *= y;
                        v.z *= z;
                        v += b.center;
                        v = trans.MultiplyPoint(v);
                        points[count++] = v;
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (this.targetTexture != null)
            {
                GUI.DrawTextureWithTexCoords(new Rect(0, 0, 256, 256), this.targetTexture, new Rect(0, 0, 1, 1), false);
            }

            if (GUI.Button(new Rect(Screen.width - 150, 0, 150, 100), "HARD"))
            {
                this.shadowType = ShadowType.HARD;
                ForAllKeywords(s => Shader.DisableKeyword(ToKeyword(s)));
                Shader.EnableKeyword(ToKeyword(this.shadowType));
            }

            if (GUI.Button(new Rect(Screen.width - 150, 100, 150, 100), "VARIANCE"))
            {
                this.shadowType = ShadowType.VARIANCE;
                ForAllKeywords(s => Shader.DisableKeyword(ToKeyword(s)));
                Shader.EnableKeyword(ToKeyword(this.shadowType));
            }
        }

    }


}
