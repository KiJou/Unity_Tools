using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GPUParticles
{
    [CustomEditor(typeof(GPUParticleEmitter)), CanEditMultipleObjects]
    public class GPUParticleEmitterEditor : Editor
    {
        private static GPUParticleEmitterEditor instance;

        private List<Module> modules;

        public static GPUParticleEmitterEditor GetInstance()
        {
            return instance;
        }

        private void OnEnable()
        {
            instance = this;

            modules = new List<Module>
            {
                new SystemModule(targets),
                new GeneralModule(serializedObject, targets),
                new ColorModule(serializedObject, targets),
                new SizeModule(serializedObject, targets),
                new LifetimeModule(serializedObject),
                new EmissionModule(serializedObject),
                new InheritVelocityModule(serializedObject),
                new NoiseModule(serializedObject),
                new ConstantInfluenceModule(serializedObject),
                new AssetsModule(serializedObject),
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUI.color = Color.white;

            foreach (Module cur in modules)
            {
                cur.Draw();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}