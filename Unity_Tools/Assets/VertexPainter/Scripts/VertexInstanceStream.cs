using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace ShaderLib.VertexPainter
{
    [ExecuteInEditMode]
    public class VertexInstanceStream : MonoBehaviour
    {
        public bool keepRuntimeData = false;

        [HideInInspector]
        [SerializeField]
        private Color[] _colors;

        [HideInInspector]
        [SerializeField]
        private List<Vector4> _uv0;

        [HideInInspector]
        [SerializeField]
        private List<Vector4> _uv1;

        [HideInInspector]
        [SerializeField]
        private List<Vector4> _uv2;

        [HideInInspector]
        [SerializeField]
        private List<Vector4> _uv3;

        [HideInInspector]
        [SerializeField]
        private Vector3[] _positions;

        [HideInInspector]
        [SerializeField]
        private Vector3[] _normals;

        [HideInInspector]
        [SerializeField]
        private Vector4[] _tangents;

        public Color[] colors
        {
            get
            {
                return _colors;
            }
            set
            {
                enforcedColorChannels = (!(_colors == null || (value != null && _colors.Length != value.Length)));
                _colors = value;
                Apply();
            }
        }

        public List<Vector4> uv0 { get { return _uv0; } set { _uv0 = value; Apply(); } }
        public List<Vector4> uv1 { get { return _uv1; } set { _uv1 = value; Apply(); } }
        public List<Vector4> uv2 { get { return _uv2; } set { _uv2 = value; Apply(); } }
        public List<Vector4> uv3 { get { return _uv3; } set { _uv3 = value; Apply(); } }
        public Vector3[] positions { get { return _positions; } set { _positions = value; Apply(); } }
        public Vector3[] normals { get { return _normals; } set { _normals = value; Apply(); } }
        public Vector4[] tangents { get { return _tangents; } set { _tangents = value; Apply(); } }

#if UNITY_EDITOR
        Vector3[] cachedPositions;
        public Vector3 GetSafePosition(int index)
        {
            if (_positions != null && index < _positions.Length)
            {
                return _positions[index];
            }
            if (cachedPositions == null)
            {
                MeshFilter mf = GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    Debug.LogError("No Mesh Filter or Mesh available");
                    return Vector3.zero;
                }
                cachedPositions = mf.sharedMesh.vertices;
            }
            if (index < cachedPositions.Length)
            {
                return cachedPositions[index];
            }
            return Vector3.zero;
        }

        Vector3[] cachedNormals;
        public Vector3 GetSafeNormal(int index)
        {
            if (_normals != null && index < _normals.Length)
            {
                return _normals[index];
            }
            if (cachedPositions == null)
            {
                MeshFilter mf = GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    Debug.LogError("No Mesh Filter or Mesh available");
                    return Vector3.zero;
                }
                cachedNormals = mf.sharedMesh.normals;
            }
            if (cachedNormals != null && index < cachedNormals.Length)
            {
                return cachedNormals[index];
            }
            return new Vector3(0, 0, 1);
        }

        Vector4[] cachedTangents;
        public Vector4 GetSafeTangent(int index)
        {
            if (_tangents != null && index < _tangents.Length)
            {
                return _tangents[index];
            }
            if (cachedTangents == null)
            {
                MeshFilter mf = GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    Debug.LogError("No Mesh Filter or Mesh available");
                    return Vector3.zero;
                }
                cachedTangents = mf.sharedMesh.tangents;
            }
            if (cachedTangents != null && index < cachedTangents.Length)
            {
                return cachedTangents[index];
            }
            return new Vector4(0, 1, 0, 1);
        }

#endif

#if UNITY_EDITOR
        [HideInInspector]
        public Material[] originalMaterial;
        public static Material vertexShaderMat;


        void Awake()
        {

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (mr.sharedMaterials != null && mr.sharedMaterial == vertexShaderMat && originalMaterial != null
                   && originalMaterial.Length == mr.sharedMaterials.Length && originalMaterial.Length > 1)
                {
                    Material[] mats = new Material[mr.sharedMaterials.Length];
                    for (int i = 0; i < mr.sharedMaterials.Length; ++i)
                    {
                        if (originalMaterial[i] != null)
                        {
                            mats[i] = originalMaterial[i];
                        }
                    }
                    mr.sharedMaterials = mats;
                }
                else if (originalMaterial != null && originalMaterial.Length > 0)
                {
                    if (originalMaterial[0] != null)
                    {
                        mr.sharedMaterial = originalMaterial[0];
                    }
                }
            }
        }
#endif

        void Start()
        {
            Apply(!keepRuntimeData);
            if (keepRuntimeData)
            {
                var mf = GetComponent<MeshFilter>();
                _positions = mf.sharedMesh.vertices;
            }
        }

        void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                MeshRenderer mr = GetComponent<MeshRenderer>();
                if (mr != null) {
                    mr.additionalVertexStreams = null;
                }
            }
        }

        bool enforcedColorChannels = false;
        void EnforceOriginalMeshHasColors(Mesh stream)
        {
            if (enforcedColorChannels == true)
                return;
            enforcedColorChannels = true;
            MeshFilter mf = GetComponent<MeshFilter>();
            Color[] origColors = mf.sharedMesh.colors;
            if (stream != null && stream.colors.Length > 0 && (origColors == null || origColors.Length == 0))
            {
                mf.sharedMesh.colors = stream.colors;
            }
        }

#if UNITY_EDITOR
        public void SetColor(Color c, int count) { _colors = new Color[count]; for (int i = 0; i < count; ++i) { _colors[i] = c; } Apply(); }
        public void SetUV0(Vector4 uv, int count) { _uv0 = new List<Vector4>(count); for (int i = 0; i < count; ++i) { _uv0.Add(uv); } Apply(); }
        public void SetUV1(Vector4 uv, int count) { _uv1 = new List<Vector4>(count); for (int i = 0; i < count; ++i) { _uv1.Add(uv); } Apply(); }
        public void SetUV2(Vector4 uv, int count) { _uv2 = new List<Vector4>(count); for (int i = 0; i < count; ++i) { _uv2.Add(uv); } Apply(); }
        public void SetUV3(Vector4 uv, int count) { _uv3 = new List<Vector4>(count); for (int i = 0; i < count; ++i) { _uv3.Add(uv); } Apply(); }

        public void SetUV0_XY(Vector2 uv, int count)
        {
            if (_uv0 == null || _uv0.Count != count)
            {
                _uv0 = new List<Vector4>(count);
                for (int i = 0; i < count; ++i)
                {
                    _uv0[i] = Vector4.zero;
                }
            }

            for (int i = 0; i < count; ++i)
            {
                Vector4 v = _uv0[i];
                v.x = uv.x;
                v.y = uv.y;
                _uv0[i] = v;
            }
            Apply();
        }

        public void SetUV0_ZW(Vector2 uv, int count)
        {
            if (_uv0 == null || _uv0.Count != count)
            {
                _uv0 = new List<Vector4>(count);
                for (int i = 0; i < count; ++i)
                {
                    _uv0[i] = Vector4.zero;
                }
            }

            for (int i = 0; i < count; ++i)
            {
                Vector4 v = _uv0[i];
                v.z = uv.x;
                v.w = uv.y;
                _uv0[i] = v;
            }
            Apply();
        }

        public void SetUV1_XY(Vector2 uv, int count)
        {
            if (_uv1 == null || _uv1.Count != count)
            {
                _uv1 = new List<Vector4>(count);
                for (int i = 0; i < count; ++i)
                {
                    _uv1[i] = Vector4.zero;
                }
            }

            for (int i = 0; i < count; ++i)
            {
                Vector4 v = _uv1[i];
                v.x = uv.x;
                v.y = uv.y;
                _uv1[i] = v;
            }
            Apply();
        }

        public void SetUV1_ZW(Vector2 uv, int count)
        {
            if (_uv1 == null || _uv1.Count != count)
            {
                _uv1 = new List<Vector4>(count);
                for (int i = 0; i < count; ++i)
                {
                    _uv1[i] = Vector4.zero;
                }
            }

            for (int i = 0; i < count; ++i)
            {
                Vector4 v = _uv1[i];
                v.z = uv.x;
                v.w = uv.y;
                _uv1[i] = v;
            }
            Apply();
        }

        public void SetUV2_XY(Vector2 uv, int count)
        {
            if (_uv2 == null || _uv2.Count != count)
            {
                _uv2 = new List<Vector4>(count);
                for (int i = 0; i < count; ++i)
                {
                    _uv2[i] = Vector4.zero;
                }
            }

            for (int i = 0; i < count; ++i)
            {
                Vector4 v = _uv2[i];
                v.x = uv.x;
                v.y = uv.y;
                _uv2[i] = v;
            }
            Apply();
        }

        public void SetUV2_ZW(Vector2 uv, int count)
        {
            if (_uv2 == null || _uv2.Count != count)
            {
                _uv2 = new List<Vector4>(count);
                for (int i = 0; i < count; ++i)
                {
                    _uv2[i] = Vector4.zero;
                }
            }

            for (int i = 0; i < count; ++i)
            {
                Vector4 v = _uv2[i];
                v.z = uv.x;
                v.w = uv.y;
                _uv2[i] = v;
            }
            Apply();
        }

        public void SetUV3_XY(Vector2 uv, int count)
        {
            if (_uv3 == null || _uv3.Count != count)
            {
                _uv3 = new List<Vector4>(count);
                for (int i = 0; i < count; ++i)
                {
                    _uv3[i] = Vector4.zero;
                }
            }

            for (int i = 0; i < count; ++i)
            {
                Vector4 v = _uv3[i];
                v.x = uv.x;
                v.y = uv.y;
                _uv3[i] = v;
            }
            Apply();
        }

        public void SetUV3_ZW(Vector2 uv, int count)
        {
            if (_uv3 == null || _uv3.Count != count)
            {
                _uv3 = new List<Vector4>(count);
                for (int i = 0; i < count; ++i)
                {
                    _uv3[i] = Vector4.zero;
                }
            }

            for (int i = 0; i < count; ++i)
            {
                Vector4 v = _uv3[i];
                v.z = uv.x;
                v.w = uv.y;
                _uv3[i] = v;
            }
            Apply();
        }

        public void SetColorRG(Vector2 rg, int count)
        {
            if (_colors == null || _colors.Length != count)
            {
                _colors = new Color[count];
                enforcedColorChannels = false;
            }
            for (int i = 0; i < count; ++i)
            {
                _colors[i].r = rg.x;
                _colors[i].g = rg.y;
            }
            Apply();
        }

        public void SetColorBA(Vector2 ba, int count)
        {
            if (_colors == null || _colors.Length != count)
            {
                _colors = new Color[count];
                enforcedColorChannels = false;
            }
            for (int i = 0; i < count; ++i)
            {
                _colors[i].r = ba.x;
                _colors[i].g = ba.y;
            }
            Apply();
        }
#endif

        public Mesh Apply(bool markNoLongerReadable = true)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            MeshFilter mf = GetComponent<MeshFilter>();

            if (mr != null && mf != null && mf.sharedMesh != null)
            {
                int vertexCount = mf.sharedMesh.vertexCount;
                Mesh stream = meshStream;
                if (stream == null || vertexCount != stream.vertexCount)
                {
                    if (stream != null)
                    {
                        DestroyImmediate(stream);
                    }
                    stream = new Mesh();

                    stream.vertices = new Vector3[mf.sharedMesh.vertexCount];
                    stream.vertices = mf.sharedMesh.vertices;
                    stream.MarkDynamic();
                    stream.triangles = mf.sharedMesh.triangles;
                    meshStream = stream;

                    stream.hideFlags = HideFlags.HideAndDontSave;
                }
                if (_positions != null && _positions.Length == vertexCount) { stream.vertices = _positions; }
                if (_normals != null && _normals.Length == vertexCount) { stream.normals = _normals; } else { stream.normals = null; }
                if (_tangents != null && _tangents.Length == vertexCount) { stream.tangents = _tangents; } else { stream.tangents = null; }
                if (_colors != null && _colors.Length == vertexCount) { stream.colors = _colors; } else { stream.colors = null; }
                if (_uv0 != null && _uv0.Count == vertexCount) { stream.SetUVs(0, _uv0); } else { stream.uv = null; }
                if (_uv1 != null && _uv1.Count == vertexCount) { stream.SetUVs(1, _uv1); } else { stream.uv2 = null; }
                if (_uv2 != null && _uv2.Count == vertexCount) { stream.SetUVs(2, _uv2); } else { stream.uv3 = null; }
                if (_uv3 != null && _uv3.Count == vertexCount) { stream.SetUVs(3, _uv3); } else { stream.uv4 = null; }

                EnforceOriginalMeshHasColors(stream);

                if (!Application.isPlaying || Application.isEditor)
                {
                    markNoLongerReadable = false;
                }

                stream.UploadMeshData(markNoLongerReadable);
                mr.additionalVertexStreams = stream;
                return stream;
            }
            return null;
        }

        private Mesh meshStream;

#if UNITY_EDITOR

        public Mesh GetModifierMesh() { return meshStream; }
        private MeshRenderer meshRend = null;
        void Update()
        {
            if (meshRend == null)
            {
                meshRend = GetComponent<MeshRenderer>();
            }
            //if (!Application.isPlaying)
            {
                meshRend.additionalVertexStreams = meshStream;
            }
        }
#endif
    }
}
