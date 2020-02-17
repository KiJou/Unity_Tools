using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderLib.Graphic
{
    public class ShadowMapReceiver : MonoBehaviour
    {
        private Renderer[] meshs;

        public Renderer[] GetRenderer
        {
            get { return meshs; }
        }

        [SerializeField]
        private bool shadowEnable = false;

        public void SetShadowEnable(bool newShadowEnable)
        {
            if (this.shadowEnable != newShadowEnable)
            {
                this.shadowEnable = newShadowEnable;
                SetShadowEnableMesh();
            }
        }

        private void Start()
        {
            this.meshs = gameObject.GetComponentsInChildren<Renderer>();
            SetShadowEnableMesh();
        }

        private void SetShadowEnableMesh()
        {
            foreach (var mesh in this.meshs)
            {
                if (mesh == null)
                {
                    continue;
                }
                mesh.shadowCastingMode = this.shadowEnable ? ShadowCastingMode.On : ShadowCastingMode.Off;
                mesh.receiveShadows = this.shadowEnable;
            }
        }
    }

}

