using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;


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
        private int resolution = 1024;

        [Range(0, 1)]
        public float maxShadowIntensity = 1.0f;

        [Range(0, 1)]
        public float varianceShadowExpansion = 0.3f;
        public ShadowType shadowType = ShadowType.HARD;
        public FilterMode filterMode = FilterMode.Bilinear;
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
                targetTexture = null;
            }
            ForAllKeywords(s => Shader.DisableKeyword(ToKeyword(s)));
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        private void Update()
        {
            this.depthShader = this.depthShader ? this.depthShader : Shader.Find("G2Studios/Shadow/ShadowMap");
            UpdateRenderTexture();
            UpdateShadowCameraPos();
            UpdateShaderValues();

            this.shadowCamera.targetTexture = this.targetTexture;
            this.shadowCamera.RenderWithShader(this.depthShader, "");
        }

        private void UpdateShaderValues()
        {
            if (this.shadowCamera == null)
            {
                return;
            }

            ForAllKeywords(s => Shader.DisableKeyword(ToKeyword(s)));
            Shader.EnableKeyword(ToKeyword(this.shadowType));
            Shader.SetGlobalTexture("_ShadowTex", this.targetTexture);
            Shader.SetGlobalMatrix("_LightMatrix", this.shadowCamera.transform.worldToLocalMatrix);
            Shader.SetGlobalFloat("_MaxShadowIntensity", this.maxShadowIntensity);
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
            Camera cam = this.shadowCamera;
            Light l = FindObjectOfType<Light>();
            cam.transform.position = l.transform.position;
            cam.transform.rotation = l.transform.rotation;
            cam.transform.LookAt(cam.transform.position + cam.transform.forward, cam.transform.up);

            Vector3 center, extents;
            List<Renderer> renderers = new List<Renderer>();
            renderers.AddRange(FindObjectsOfType<Renderer>());
            GetRenderersExtents(renderers, cam.transform, out center, out extents);
            center.z -= extents.z / 2;
            cam.transform.position = cam.transform.TransformPoint(center);
            cam.nearClipPlane = 0;
            cam.farClipPlane = extents.z;
            cam.aspect = extents.x / extents.y;
            cam.orthographicSize = extents.y / 2;
        }

        private RenderTexture CreateTargetTexture()
        {
            RenderTexture rt = new RenderTexture(this.resolution, this.resolution, 16, RenderTextureFormat.ARGBFloat);
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
            this.shadowCamera.backgroundColor = new Color(0, 0, 0, 0);
            this.shadowCamera.clearFlags = CameraClearFlags.SolidColor;
        }
    }


}
