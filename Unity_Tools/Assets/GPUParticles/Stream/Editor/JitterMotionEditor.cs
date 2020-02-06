
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace GPU_Particle
{

    [CustomEditor(typeof(JitterMotion)), CanEditMultipleObjects]
    public class JitterMotionEditor : Editor
    {
        SerializedProperty propPositionFrequency;
        SerializedProperty propRotationFrequency;

        SerializedProperty propPositionAmount;
        SerializedProperty propRotationAmount;

        SerializedProperty propPositionComponents;
        SerializedProperty propRotationComponents;

        SerializedProperty propPositionOctave;
        SerializedProperty propRotationOctave;

        GUIContent labelFrequency;
        GUIContent labelAmount;
        GUIContent labelOctave;

        void OnEnable()
        {
            propPositionFrequency = serializedObject.FindProperty("positionFrequency");
            propRotationFrequency = serializedObject.FindProperty("rotationFrequency");

            propPositionAmount = serializedObject.FindProperty("positionAmount");
            propRotationAmount = serializedObject.FindProperty("rotationAmount");

            propPositionComponents = serializedObject.FindProperty("positionComponents");
            propRotationComponents = serializedObject.FindProperty("rotationComponents");

            propPositionOctave = serializedObject.FindProperty("positionOctave");
            propRotationOctave = serializedObject.FindProperty("rotationOctave");

            labelFrequency = new GUIContent("Frequency");
            labelAmount = new GUIContent("Noise Strength");
            labelOctave = new GUIContent("Fractal Level");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Position");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(propPositionAmount, labelAmount);
            EditorGUILayout.PropertyField(propPositionComponents, GUIContent.none);
            EditorGUILayout.PropertyField(propPositionFrequency, labelFrequency);
            EditorGUILayout.IntSlider(propPositionOctave, 1, 8, labelOctave);
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Rotation");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(propRotationAmount, labelAmount);
            EditorGUILayout.PropertyField(propRotationComponents, GUIContent.none);
            EditorGUILayout.PropertyField(propRotationFrequency, labelFrequency);
            EditorGUILayout.IntSlider(propRotationOctave, 1, 8, labelOctave);
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }

}
