using System.Collections;
using UnityEditor;
using System.Collections.Generic;


namespace ShaderLib.VertexPainter
{
    public partial class VertexPainterWindow : EditorWindow
    {
        [MenuItem("G2Studios/Vertex Painter")]
        public static void ShowWindow()
        {
            var window = GetWindow<ShaderLib.VertexPainter.VertexPainterWindow>();
            window.InitMeshes();
            window.Show();
        }
    }
}
