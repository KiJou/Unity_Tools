//
// Custom editor class for Stream
//
using UnityEngine;
using UnityEditor;

namespace GPU_Particle
{
    [CustomEditor(typeof(Stream)), CanEditMultipleObjects]
    public class StreamEditor : Editor
    {
        SerializedProperty _maxParticles;
        SerializedProperty _emitterPosition;
        SerializedProperty _emitterSize;
        SerializedProperty _throttle;

        SerializedProperty _direction;
        SerializedProperty _minSpeed;
        SerializedProperty _maxSpeed;
        SerializedProperty _spread;

        SerializedProperty _noiseAmplitude;
        SerializedProperty _noiseFrequency;
        SerializedProperty _noiseSpeed;

        SerializedProperty _color;
        SerializedProperty _tail;
        SerializedProperty _randomSeed;
        SerializedProperty _debug;

        static GUIContent _textCenter = new GUIContent("Center");
        static GUIContent _textSize = new GUIContent("Size");
        static GUIContent _textSpeed = new GUIContent("Speed");
        static GUIContent _textAmplitude = new GUIContent("Amplitude");
        static GUIContent _textFrequency = new GUIContent("Frequency");

        void OnEnable()
        {
            _maxParticles = serializedObject.FindProperty("maxParticles");
            _emitterPosition = serializedObject.FindProperty("emitterPosition");
            _emitterSize = serializedObject.FindProperty("emitterSize");
            _throttle = serializedObject.FindProperty("throttle");

            _direction = serializedObject.FindProperty("direction");
            _minSpeed = serializedObject.FindProperty("minSpeed");
            _maxSpeed = serializedObject.FindProperty("maxSpeed");
            _spread = serializedObject.FindProperty("spread");

            _noiseAmplitude = serializedObject.FindProperty("noiseAmplitude");
            _noiseFrequency = serializedObject.FindProperty("noiseFrequency");
            _noiseSpeed = serializedObject.FindProperty("noiseSpeed");

            _color = serializedObject.FindProperty("color");
            _tail = serializedObject.FindProperty("tail");
            _randomSeed = serializedObject.FindProperty("randomSeed");
            _debug = serializedObject.FindProperty("debug");
        }

        public override void OnInspectorGUI()
        {
            var targetStream = target as Stream;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_maxParticles);
            if (!_maxParticles.hasMultipleDifferentValues)
            {
                EditorGUILayout.LabelField(" ", "Allocated: " + targetStream.MaxParticles, EditorStyles.miniLabel);
            }

            if (EditorGUI.EndChangeCheck())
            {
                targetStream.NotifyConfigChange();
            }

            EditorGUILayout.LabelField("Emitter", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_emitterPosition, _textCenter);
            EditorGUILayout.PropertyField(_emitterSize, _textSize);
            EditorGUILayout.PropertyField(_throttle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Velocity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_direction);
            MinMaxSlider(_textSpeed, _minSpeed, _maxSpeed, 0.0f, 50.0f);
            EditorGUILayout.PropertyField(_spread);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Turbulent Noise", EditorStyles.boldLabel);
            EditorGUILayout.Slider(_noiseAmplitude, 0.0f, 50.0f, _textAmplitude);
            EditorGUILayout.Slider(_noiseFrequency, 0.01f, 1.0f, _textFrequency);
            EditorGUILayout.Slider(_noiseSpeed, 0.0f, 10.0f, _textSpeed);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_color);
            EditorGUILayout.Slider(_tail, 0.0f, 20.0f);
            EditorGUILayout.PropertyField(_randomSeed);
            EditorGUILayout.PropertyField(_debug);

            serializedObject.ApplyModifiedProperties();
        }

        private void MinMaxSlider(
            GUIContent label,
            SerializedProperty propMin,
            SerializedProperty propMax,
            float minLimit,
            float maxLimit)
        {
            var min = propMin.floatValue;
            var max = propMax.floatValue;

            EditorGUI.BeginChangeCheck();

            // Min-max slider.
            EditorGUILayout.MinMaxSlider(label, ref min, ref max, minLimit, maxLimit);

            var prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Float value boxes.
            var rect = EditorGUILayout.GetControlRect();
            rect.x += EditorGUIUtility.labelWidth;
            rect.width = (rect.width - EditorGUIUtility.labelWidth) / 2 - 2;

            if (EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.labelWidth = 28;
                min = Mathf.Clamp(EditorGUI.FloatField(rect, "min", min), minLimit, max);
                rect.x += rect.width + 4;
                max = Mathf.Clamp(EditorGUI.FloatField(rect, "max", max), min, maxLimit);
                EditorGUIUtility.labelWidth = 0;
            }
            else
            {
                min = Mathf.Clamp(EditorGUI.FloatField(rect, min), minLimit, max);
                rect.x += rect.width + 4;
                max = Mathf.Clamp(EditorGUI.FloatField(rect, max), min, maxLimit);
            }

            EditorGUI.indentLevel = prevIndent;

            if (EditorGUI.EndChangeCheck())
            {
                propMin.floatValue = min;
                propMax.floatValue = max;
            }
        }
    }
}
