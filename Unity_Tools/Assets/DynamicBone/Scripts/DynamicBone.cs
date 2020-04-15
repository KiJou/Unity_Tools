using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

[AddComponentMenu("Dynamic Bone/Dynamic Bone")]
public class DynamicBone : MonoBehaviour
{

#region Props
    public Transform m_Root = null;
	
    public float m_UpdateRate = 60.0f;
	
    public enum UpdateMode
    {
        Normal,
        AnimatePhysics,
        UnscaledTime
    }
    public UpdateMode m_UpdateMode = UpdateMode.Normal;
	
    [Range(0, 1)]
    public float m_Damping = 0.1f;
    public AnimationCurve m_DampingDistrib = null;
	
    [Range(0, 1)]
    public float m_Elasticity = 0.1f;
    public AnimationCurve m_ElasticityDistrib = null;
	
    [Range(0, 1)]
    public float m_Stiffness = 0.1f;
    public AnimationCurve m_StiffnessDistrib = null;
	
    [Range(0, 1)]
    public float m_Inert = 0;
    public AnimationCurve m_InertDistrib = null;
	
    public float m_Radius = 0;
    public AnimationCurve m_RadiusDistrib = null;

    public float m_EndLength = 0;
	
    public Vector3 m_EndOffset = Vector3.zero;
	
    public Vector3 m_Gravity = Vector3.zero;
	
    public Vector3 m_Force = Vector3.zero;
	
    public List<DynamicBoneColliderBase> m_Colliders = null;
	
    public List<Transform> m_Exclusions = null;
	
    public enum FreezeAxis
    {
        None, X, Y, Z
    }
    public FreezeAxis m_FreezeAxis = FreezeAxis.None;
    public bool m_DistantDisable = false;
    public Transform m_ReferenceObject = null;
    public float m_DistanceToObject = 20;

    [Header("Wind Options")]
    public bool m_UseWind = false;
    public float m_WindSpeed = 2f;
    public float m_WindScale = 1f;
    public Vector3 m_WindDirection = Vector3.forward;

    Vector3 m_LocalGravity = Vector3.zero;
    Vector3 m_ObjectMove = Vector3.zero;
    Vector3 m_ObjectPrevPosition = Vector3.zero;
    float m_BoneTotalLength = 0;
    float m_ObjectScale = 1.0f;
    float m_Time = 0;
    float m_Weight = 1.0f;
    bool m_DistantDisabled = false;
#endregion

    public class Particle
    {
        public Transform m_Transform = null;
        public int m_ParentIndex = -1;
        public float m_Damping = 0;
        public float m_Elasticity = 0;
        public float m_Stiffness = 0;
        public float m_Inert = 0;
        public float m_Radius = 0;
        public float m_BoneLength = 0;

        public Vector3 m_Position = Vector3.zero;
        public Vector3 m_PrevPosition = Vector3.zero;
        public Vector3 m_EndOffset = Vector3.zero;
        public Vector3 m_InitLocalPosition = Vector3.zero;
        public Quaternion m_InitLocalRotation = Quaternion.identity;
    }

    List<Particle> m_Particles = new List<Particle>();
    public ReadOnlyCollection<Particle> Particles
    {
        get { return m_Particles.AsReadOnly(); }
    }

#region UnityAPI
    private void Start()
    {
        SetupParticles();
    }

    private void Update()
    {
        if (m_UpdateMode != UpdateMode.AnimatePhysics)
        {
            PreUpdate();
        }
    }

    private void FixedUpdate()
    {
        if (m_UpdateMode == UpdateMode.AnimatePhysics)
        {
            PreUpdate();
        }
    }

    private void OnEnable()
    {
        ResetParticlesPosition();
    }

    private void OnDisable()
    {
        InitTransforms();
    }

    /// <summary>
    /// Editor Value Change
    /// </summary>
    private void OnValidate()
    {
        m_UpdateRate = Mathf.Max(m_UpdateRate, 0);
        m_Damping = Mathf.Clamp01(m_Damping);
        m_Elasticity = Mathf.Clamp01(m_Elasticity);
        m_Stiffness = Mathf.Clamp01(m_Stiffness);
        m_Inert = Mathf.Clamp01(m_Inert);
        m_Radius = Mathf.Max(m_Radius, 0);

        if (Application.isEditor && Application.isPlaying)
        {
            InitTransforms();
            SetupParticles();
        }
    }

    private void LateUpdate()
    {
        if (m_DistantDisable)
        {
            CheckDistance();
        }

        if (m_Weight > 0 && !(m_DistantDisable && m_DistantDisabled))
        {
            float dt = Time.deltaTime;
            UpdateDynamicBones(dt);
        }
    }
#endregion

    private void PreUpdate()
    {
        if (m_Weight > 0 && !(m_DistantDisable && m_DistantDisabled))
        {
            InitTransforms();
        }
    }

    private void InitTransforms()
    {
        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            if (p.m_Transform == null)
            {
                continue;
            }
            p.m_Transform.localPosition = p.m_InitLocalPosition;
            p.m_Transform.localRotation = p.m_InitLocalRotation;
        }
    }

    private void CheckDistance()
    {
        Transform obj = m_ReferenceObject;
        if (obj == null && Camera.main != null)
        {
            obj = Camera.main.transform;
        }
        if (obj)
        {
            float d = (obj.position - transform.position).sqrMagnitude;
            bool disable = d > m_DistanceToObject * m_DistanceToObject;
            if (disable != m_DistantDisabled)
            {
                if (!disable)
                {
                    ResetParticlesPosition();
                }
                m_DistantDisabled = disable;
            }
        }
    }

    private void UpdateDynamicBones(float t)
    {
        if (m_Root == null)
        {
            return;
        }

        m_ObjectScale = Mathf.Abs(transform.lossyScale.x);
        m_ObjectMove = transform.position - m_ObjectPrevPosition;
        m_ObjectPrevPosition = transform.position;

        int loop = 1;
        int max = 3;
        if (m_UpdateRate > 0)
        {
            float dt = 1.0f / m_UpdateRate;
            m_Time += t;
            loop = 0;

            while (m_Time >= dt)
            {
                m_Time -= dt;
                if (++loop >= max)
                {
                    m_Time = 0;
                    break;
                }
            }
        }

        if (loop > 0)
        {
            for (int i = 0; i < loop; ++i)
            {
                UpdateParticles1();
                UpdateParticles2();
                m_ObjectMove = Vector3.zero;
            }
        }
        else
        {
            SkipUpdateParticles();
        }
        ApplyParticlesToTransforms();
    }

    private void SetupParticles()
    {
        m_Particles.Clear();
        if (m_Root == null)
        {
            return;
        }

        m_LocalGravity = m_Root.InverseTransformDirection(m_Gravity);
        m_ObjectScale = Mathf.Abs(transform.lossyScale.x);
        m_ObjectPrevPosition = transform.position;
        m_ObjectMove = Vector3.zero;
        m_BoneTotalLength = 0;
        AppendParticles(m_Root, -1, 0);
        UpdateParameters();
    }

    private void AppendParticles(Transform bone, int parentIndex, float boneLength)
    {
        Particle p = new Particle();
        p.m_Transform = bone;
        p.m_ParentIndex = parentIndex;
        if (bone != null)
        {
            p.m_Position = p.m_PrevPosition = bone.position;
            p.m_InitLocalPosition = bone.localPosition;
            p.m_InitLocalRotation = bone.localRotation;
        }
        else
        {
            Transform pb = m_Particles[parentIndex].m_Transform;
            if (m_EndLength > 0)
            {
                Transform ppb = pb.parent;
                p.m_EndOffset = (ppb) ? pb.InverseTransformPoint((pb.position * 2 - ppb.position)) * m_EndLength : new Vector3(m_EndLength, 0, 0);
            }
            else
            {
                p.m_EndOffset = pb.InverseTransformPoint(transform.TransformDirection(m_EndOffset) + pb.position);
            }
            p.m_Position = p.m_PrevPosition = pb.TransformPoint(p.m_EndOffset);
        }

        if (parentIndex >= 0)
        {
            boneLength += (m_Particles[parentIndex].m_Transform.position - p.m_Position).magnitude;
            p.m_BoneLength = boneLength;
            m_BoneTotalLength = Mathf.Max(m_BoneTotalLength, boneLength);
        }

        int index = m_Particles.Count;
        m_Particles.Add(p);

        if (bone != null)
        {
            for (int i = 0; i < bone.childCount; ++i)
            {
                bool exclude = false;
                if (m_Exclusions != null)
                {
                    for (int j = 0; j < m_Exclusions.Count; ++j)
                    {
                        Transform e = m_Exclusions[j];
                        if (e == bone.GetChild(i))
                        {
                            exclude = true;
                            break;
                        }
                    }
                }
                if (!exclude)
                {
                    AppendParticles(bone.GetChild(i), index, boneLength);
                }
                else if (m_EndLength > 0 || m_EndOffset != Vector3.zero)
                {
                    AppendParticles(null, index, boneLength);
                }
            }

            if (bone.childCount == 0 && (m_EndLength > 0 || m_EndOffset != Vector3.zero))
            {
                AppendParticles(null, index, boneLength);
            }
        }
    }

#region Public
    public void SetWeight(float w)
    {
        if (m_Weight != w)
        {
            if (w == 0)
            {
                InitTransforms();
            }
            else if (m_Weight == 0)
            {
                ResetParticlesPosition();
            }
            m_Weight = w;
        }
    }

    public float GetWeight()
    {
        return m_Weight;
    }

    public void UpdateParameters()
    {
        if (m_Root == null)
        {
            return;
        }

        m_LocalGravity = m_Root.InverseTransformDirection(m_Gravity);

        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            p.m_Damping = m_Damping;
            p.m_Elasticity = m_Elasticity;
            p.m_Stiffness = m_Stiffness;
            p.m_Inert = m_Inert;
            p.m_Radius = m_Radius;

            if (m_BoneTotalLength > 0)
            {
                float a = p.m_BoneLength / m_BoneTotalLength;
                if (m_DampingDistrib != null && m_DampingDistrib.keys.Length > 0)
                {
                    p.m_Damping *= m_DampingDistrib.Evaluate(a);
                }
                if (m_ElasticityDistrib != null && m_ElasticityDistrib.keys.Length > 0)
                {
                    p.m_Elasticity *= m_ElasticityDistrib.Evaluate(a);
                }
                if (m_StiffnessDistrib != null && m_StiffnessDistrib.keys.Length > 0)
                {
                    p.m_Stiffness *= m_StiffnessDistrib.Evaluate(a);
                }
                if (m_InertDistrib != null && m_InertDistrib.keys.Length > 0)
                {
                    p.m_Inert *= m_InertDistrib.Evaluate(a);
                }
                if (m_RadiusDistrib != null && m_RadiusDistrib.keys.Length > 0)
                {
                    p.m_Radius *= m_RadiusDistrib.Evaluate(a);
                }
            }

            p.m_Damping = Mathf.Clamp01(p.m_Damping);
            p.m_Elasticity = Mathf.Clamp01(p.m_Elasticity);
            p.m_Stiffness = Mathf.Clamp01(p.m_Stiffness);
            p.m_Inert = Mathf.Clamp01(p.m_Inert);
            p.m_Radius = Mathf.Max(p.m_Radius, 0);
        }
    }
#endregion

    private void ResetParticlesPosition()
    {
        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            if (p.m_Transform != null)
            {
                p.m_Position = p.m_PrevPosition = p.m_Transform.position;
            }
            else
            {
                Transform pb = m_Particles[p.m_ParentIndex].m_Transform;
                p.m_Position = p.m_PrevPosition = pb.TransformPoint(p.m_EndOffset);
            }
        }
        m_ObjectPrevPosition = transform.position;
    }

    private void UpdateParticles1()
    {
        Vector3 currentForce = m_Force;
        // wip
        if (m_UseWind)
        {
            Vector3 inverse = m_Root ? m_Root.InverseTransformVector(m_WindDirection) : Vector3.zero;
            Vector3 windVelocity = m_WindDirection * m_WindSpeed * m_WindScale;
            currentForce += windVelocity * Time.deltaTime;
        }

        Vector3 force = m_Gravity;
        Vector3 fdir = m_Gravity.normalized;
        Vector3 rf = m_Root.TransformDirection(m_LocalGravity);
        Vector3 pf = fdir * Mathf.Max(Vector3.Dot(rf, fdir), 0);
        force -= pf;
        force = (force + currentForce) * m_ObjectScale;

        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            if (p.m_ParentIndex >= 0)
            {
                Vector3 v = p.m_Position - p.m_PrevPosition;
                Vector3 rmove = m_ObjectMove * p.m_Inert;
                p.m_PrevPosition = p.m_Position + rmove;
                p.m_Position += v * (1 - p.m_Damping) + force + rmove;
            }
            else
            {
                p.m_PrevPosition = p.m_Position;
                p.m_Position = p.m_Transform.position;
            }
        }
    }

    private void UpdateParticles2()
    {
        Plane movePlane = new Plane();

        for (int i = 1; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            Particle p0 = m_Particles[p.m_ParentIndex];

            float restLen = (p.m_Transform != null) ? 
                (p0.m_Transform.position - p.m_Transform.position).magnitude : 
                p0.m_Transform.localToWorldMatrix.MultiplyVector(p.m_EndOffset).magnitude;

            // keep shape
            float stiffness = Mathf.Lerp(1.0f, p.m_Stiffness, m_Weight);
            if (stiffness > 0 || p.m_Elasticity > 0)
            {
                Matrix4x4 m0 = p0.m_Transform.localToWorldMatrix;
                m0.SetColumn(3, p0.m_Position);
                Vector3 restPos = (p.m_Transform != null) ? 
                    m0.MultiplyPoint3x4(p.m_Transform.localPosition) : 
                    m0.MultiplyPoint3x4(p.m_EndOffset);
                Vector3 d = restPos - p.m_Position;
                p.m_Position += d * p.m_Elasticity;

                if (stiffness > 0)
                {
                    d = restPos - p.m_Position;
                    float len = d.magnitude;
                    float maxlen = restLen * (1 - stiffness) * 2;
                    if (len > maxlen)
                    {
                        p.m_Position += d * ((len - maxlen) / len);
                    }
                }
            }

            // collide
            if (m_Colliders != null)
            {
                float particleRadius = p.m_Radius * m_ObjectScale;
                for (int j = 0; j < m_Colliders.Count; ++j)
                {
                    DynamicBoneColliderBase c = m_Colliders[j];
                    if (c != null && c.enabled)
                    {
                        c.Collide(ref p.m_Position, particleRadius);
                    }
                }
            }

            // freeze axis, project to plane 
            if (m_FreezeAxis != FreezeAxis.None)
            {
                switch (m_FreezeAxis)
                {
                    case FreezeAxis.X:
                        movePlane.SetNormalAndPosition(p0.m_Transform.right, p0.m_Position);
                        break;
                    case FreezeAxis.Y:
                        movePlane.SetNormalAndPosition(p0.m_Transform.up, p0.m_Position);
                        break;
                    case FreezeAxis.Z:
                        movePlane.SetNormalAndPosition(p0.m_Transform.forward, p0.m_Position);
                        break;
                }
                p.m_Position -= movePlane.normal * movePlane.GetDistanceToPoint(p.m_Position);
            }

            // keep length
            Vector3 dd = p0.m_Position - p.m_Position;
            float leng = dd.magnitude;
            if (leng > 0)
            {
                p.m_Position += dd * ((leng - restLen) / leng);
            }
        }
    }

    // only update stiffness and keep bone length
    private void SkipUpdateParticles()
    {
        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            if (p.m_ParentIndex >= 0)
            {
                p.m_PrevPosition += m_ObjectMove;
                p.m_Position += m_ObjectMove;

                Particle p0 = m_Particles[p.m_ParentIndex];

                float restLen = (p.m_Transform != null) ? 
                    (p0.m_Transform.position - p.m_Transform.position).magnitude : 
                    p0.m_Transform.localToWorldMatrix.MultiplyVector(p.m_EndOffset).magnitude;

                // keep shape
                float stiffness = Mathf.Lerp(1.0f, p.m_Stiffness, m_Weight);
                if (stiffness > 0)
                {
                    Matrix4x4 m0 = p0.m_Transform.localToWorldMatrix;
                    m0.SetColumn(3, p0.m_Position);
                    Vector3 restPos = (p.m_Transform != null) ? 
                        m0.MultiplyPoint3x4(p.m_Transform.localPosition) : 
                        m0.MultiplyPoint3x4(p.m_EndOffset);
                    Vector3 d = restPos - p.m_Position;
                    float len = d.magnitude;
                    float maxlen = restLen * (1 - stiffness) * 2;
                    if (len > maxlen)
                    {
                        p.m_Position += d * ((len - maxlen) / len);
                    }
                }

                // keep length
                Vector3 dd = p0.m_Position - p.m_Position;
                float leng = dd.magnitude;
                if (leng > 0)
                {
                    p.m_Position += dd * ((leng - restLen) / leng);
                }
            }
            else
            {
                p.m_PrevPosition = p.m_Position;
                p.m_Position = p.m_Transform.position;
            }
        }
    }

    private void ApplyParticlesToTransforms()
    {
        for (int i = 1; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            Particle p0 = m_Particles[p.m_ParentIndex];

            if (p0.m_Transform.childCount <= 1)	
            {
                Vector3 v = (p.m_Transform != null) ? p.m_Transform.localPosition : p.m_EndOffset;
                Vector3 v2 = p.m_Position - p0.m_Position;
                Quaternion rot = Quaternion.FromToRotation(p0.m_Transform.TransformDirection(v), v2);
                p0.m_Transform.rotation = rot * p0.m_Transform.rotation;
            }

            if (p.m_Transform != null)
            {
                p.m_Transform.position = p.m_Position;
            }
        }
    }

    static Vector3 MirrorVector(Vector3 v, Vector3 axis)
    {
        return v - axis * (Vector3.Dot(v, axis) * 2);
    }

#region Editor
    private void OnDrawGizmosSelected()
    {
        if (!enabled || m_Root == null)
        {
            return;
        }

        if (Application.isEditor && !Application.isPlaying && transform.hasChanged)
        {
            InitTransforms();
            SetupParticles();
        }

        Gizmos.color = Color.white;
        for (int i = 0; i < m_Particles.Count; ++i)
        {
            Particle p = m_Particles[i];
            if (p.m_ParentIndex >= 0)
            {
                Particle p0 = m_Particles[p.m_ParentIndex];
                Gizmos.DrawLine(p.m_Position, p0.m_Position);
            }
            if (p.m_Radius > 0)
            {
                Gizmos.DrawWireSphere(p.m_Position, p.m_Radius * m_ObjectScale);
            }
        }
    }
#endregion

}
