using UnityEngine;
using UnityEditor;

namespace GPUParticles
{
    public class AssetsModule : Module
    {

        SerializedProperty computeShader;
        SerializedProperty renderMaterial;

        public AssetsModule(SerializedObject serializedObject)
        {
            computeShader = serializedObject.FindProperty("computeShader");
            renderMaterial = serializedObject.FindProperty("renderMaterial");
        }

        public override void Draw()
        {
            DrawGUI("Assets", DrawContent);
        }

        private void DrawContent()
        {
            EditorGUILayout.PropertyField(computeShader, new GUIContent("Compute Shader"));
            EditorGUILayout.PropertyField(renderMaterial, new GUIContent("Render Material"));
            EditorGUI.BeginChangeCheck();
        }
    }
}