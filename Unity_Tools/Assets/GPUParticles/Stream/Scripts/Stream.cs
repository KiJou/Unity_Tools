using UnityEngine;

namespace GPU_Particle
{
    [ExecuteInEditMode, AddComponentMenu("GPU_Particle/Stream")]
    public class Stream : MonoBehaviour
    {
        [SerializeField]
        int maxParticles = 32768;

        [SerializeField]
        Vector3 emitterPosition = Vector3.forward * 20;

        [SerializeField]
        Vector3 emitterSize = Vector3.one * 40;

        [SerializeField, Range(0, 1)]
        float throttle = 1.0f;

        [SerializeField]
        Vector3 direction = -Vector3.forward;

        [SerializeField]
        float minSpeed = 5.0f;

        [SerializeField]
        float maxSpeed = 10.0f;

        [SerializeField, Range(0, 1)]
        float spread = 0.2f;

        [SerializeField]
        float noiseAmplitude = 0.1f;

        [SerializeField]
        float noiseFrequency = 0.2f;

        [SerializeField]
        float noiseSpeed = 1.0f;

        [SerializeField, ColorUsage(true, true, 0, 8, 0.125f, 3)]
        Color color = Color.white;

        [SerializeField]
        float tail = 1.0f;

        [SerializeField]
        int randomSeed = 0;

        [SerializeField]
        bool debug;

        public int MaxParticles
        {
            get { return BufferWidth * BufferHeight; }
        }

        public float Throttle
        {
            get { return throttle; }
            set { throttle = value; }
        }

        public Vector3 EmitterPosition
        {
            get { return emitterPosition; }
            set { emitterPosition = value; }
        }

        public Vector3 EmitterSize
        {
            get { return emitterSize; }
            set { emitterSize = value; }
        }

        public Vector3 Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        public float MinSpeed
        {
            get { return minSpeed; }
            set { minSpeed = value; }
        }

        public float MaxSpeed
        {
            get { return maxSpeed; }
            set { maxSpeed = value; }
        }

        public float Spread
        {
            get { return spread; }
            set { spread = value; }
        }

        public float NoiseAmplitude
        {
            get { return noiseAmplitude; }
            set { noiseAmplitude = value; }
        }

        public float NoiseFrequency
        {
            get { return noiseFrequency; }
            set { noiseFrequency = value; }
        }

        public float NoiseSpeed
        {
            get { return noiseSpeed; }
            set { noiseSpeed = value; }
        }

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public float Tail
        {
            get { return tail; }
            set { tail = value; }
        }

        Material kernelMaterial;
        Material lineMaterial;
        Material debugMaterial;

        RenderTexture particleBuffer1;
        RenderTexture particleBuffer2;
        Mesh mesh;
        bool needsReset = true;

        int BufferWidth { get { return 256; } }

        int BufferHeight
        {
            get {
                return Mathf.Clamp(maxParticles / BufferWidth + 1, 1, 127);
            }
        }

        static float deltaTime
        {
            get {
                return Application.isPlaying && Time.frameCount > 1 ? Time.deltaTime : 1.0f / 10;
            }
        }

        public void NotifyConfigChange()
        {
            needsReset = true;
        }

        Material CreateMaterial(Shader shader)
        {
            var material = new Material(shader);
            material.hideFlags = HideFlags.DontSave;
            return material;
        }

        RenderTexture CreateBuffer()
        {
            var buffer = new RenderTexture(BufferWidth, BufferHeight, 0, RenderTextureFormat.ARGBFloat);
            buffer.hideFlags = HideFlags.DontSave;
            buffer.filterMode = FilterMode.Point;
            buffer.wrapMode = TextureWrapMode.Repeat;
            return buffer;
        }

        Mesh CreateMesh()
        {
            var Nx = BufferWidth;
            var Ny = BufferHeight;

            // Create vertex arrays.
            var VA = new Vector3[Nx * Ny * 2];
            var TA = new Vector2[Nx * Ny * 2];

            var Ai = 0;
            for (var x = 0; x < Nx; x++)
            {
                for (var y = 0; y < Ny; y++)
                {
                    VA[Ai + 0] = new Vector3(1, 0, 0);
                    VA[Ai + 1] = new Vector3(0, 0, 0);

                    var u = (float)x / Nx;
                    var v = (float)y / Ny;
                    TA[Ai] = TA[Ai + 1] = new Vector2(u, v);

                    Ai += 2;
                }
            }

            // Index array.
            var IA = new int[VA.Length];
            for (Ai = 0; Ai < VA.Length; Ai++)
            {
                IA[Ai] = Ai;
            }

            // Create a mesh object.
            var mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSave;
            mesh.vertices = VA;
            mesh.uv = TA;
            mesh.SetIndices(IA, MeshTopology.Lines, 0);
            mesh.Optimize();

            // Avoid being culled.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
            return mesh;
        }

        void UpdateKernelShader()
        {
            var m = kernelMaterial;

            m.SetVector("_EmitterPos", emitterPosition);
            m.SetVector("_EmitterSize", emitterSize);

            var dir = new Vector4(direction.x, direction.y, direction.z, spread);
            m.SetVector("_Direction", dir);

            m.SetVector("_SpeedParams", new Vector2(minSpeed, maxSpeed));

            if (noiseAmplitude > 0)
            {
                var np = new Vector3(noiseFrequency, noiseAmplitude, noiseSpeed);
                m.SetVector("_NoiseParams", np);
                m.EnableKeyword("NOISE_ON");
            }
            else
            {
                m.DisableKeyword("NOISE_ON");
            }
            var life = 2.0f;
            m.SetVector("_Config", new Vector4(throttle, life, randomSeed, deltaTime));
        }

        void ResetResources()
        {
            // Mesh object.
            if (mesh == null)
            {
                mesh = CreateMesh();
            }

            // Particle buffers.
            if (particleBuffer1)
            {
                DestroyImmediate(particleBuffer1);
            }
            if (particleBuffer2)
            {
                DestroyImmediate(particleBuffer2);
            }

            particleBuffer1 = CreateBuffer();
            particleBuffer2 = CreateBuffer();

            // Shader materials.
            if (!kernelMaterial)
            {
                kernelMaterial = CreateMaterial(Shader.Find("Hidden/Kvant/Stream/Kernel"));
            }
            if (!lineMaterial)
            {
                lineMaterial = CreateMaterial(Shader.Find("Hidden/Kvant/Stream/Line"));
            }
            if (!debugMaterial)
            {
                debugMaterial = CreateMaterial(Shader.Find("Hidden/Kvant/Stream/Debug"));
            }

            // Warming up.
            UpdateKernelShader();
            InitializeAndPrewarmBuffers();
            needsReset = false;
        }

        void InitializeAndPrewarmBuffers()
        {
            // Initialization.
            Graphics.Blit(null, particleBuffer2, kernelMaterial, 0);

            // Execute the kernel shader repeatedly.
            for (var i = 0; i < 8; i++)
            {
                Graphics.Blit(particleBuffer2, particleBuffer1, kernelMaterial, 1);
                Graphics.Blit(particleBuffer1, particleBuffer2, kernelMaterial, 1);
            }
        }


        void Reset()
        {
            needsReset = true;
        }

        private void OnDestroy()
        {
            if (mesh)
            {
                DestroyImmediate(mesh);
            }
            if (particleBuffer1)
            {
                DestroyImmediate(particleBuffer1);
            }
            if (particleBuffer2)
            {
                DestroyImmediate(particleBuffer2);
            }
            if (kernelMaterial)
            {
                DestroyImmediate(kernelMaterial);
            }
            if (lineMaterial)
            {
                DestroyImmediate(lineMaterial);
            }
            if (debugMaterial)
            {
                DestroyImmediate(debugMaterial);
            }
        }

        private void Update()
        {
            if (needsReset)
            {
                ResetResources();
            }

            UpdateKernelShader();

            if (Application.isPlaying)
            {
                // Swap the particle buffers.
                var temp = particleBuffer1;
                particleBuffer1 = particleBuffer2;
                particleBuffer2 = temp;

                // Execute the kernel shader.
                Graphics.Blit(particleBuffer1, particleBuffer2, kernelMaterial, 1);
            }
            else
            {
                InitializeAndPrewarmBuffers();
            }

            // Draw particles.
            lineMaterial.SetTexture("_ParticleTex1", particleBuffer1);
            lineMaterial.SetTexture("_ParticleTex2", particleBuffer2);
            lineMaterial.SetColor("_Color", color);
            lineMaterial.SetFloat("_Tail", tail / deltaTime / 60);
            Graphics.DrawMesh(mesh, transform.position, transform.rotation, lineMaterial, gameObject.layer);
        }

        private void OnGUI()
        {
            if (debug && Event.current.type.Equals(EventType.Repaint))
            {
                if (debugMaterial && particleBuffer2)
                {
                    var rect = new Rect(0, 0, 256, 64);
                    Graphics.DrawTexture(rect, particleBuffer2, debugMaterial);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(emitterPosition, emitterSize);
        }
    }
}
