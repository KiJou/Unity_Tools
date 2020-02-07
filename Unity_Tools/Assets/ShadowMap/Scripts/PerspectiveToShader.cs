using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PerspectiveToShader : MonoBehaviour
{

    public Material materialToModify;
    public RenderTexture ShadowMap_Light, ShadowMap_View;

    Camera mainCam;
    Camera lightCam;

    private void OnEnable()
    {
        mainCam = GetComponent<Camera>();
        lightCam = GameObject.FindGameObjectWithTag("Light").GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        var lightVMatrix = mainCam.worldToCameraMatrix;
        var lightPMatrix = GL.GetGPUProjectionMatrix(mainCam.projectionMatrix, false);
        var lightVP = lightPMatrix * lightVMatrix;

        var viewProjection = mainCam.nonJitteredProjectionMatrix * transform.worldToLocalMatrix;
        var LightProjection = lightCam.nonJitteredProjectionMatrix * lightCam.gameObject.transform.worldToLocalMatrix;
        var View_Mat_LightP = lightCam.worldToCameraMatrix;

        materialToModify.SetMatrix("WorldToCamera", viewProjection);
        materialToModify.SetMatrix("WorldToLight", LightProjection);
        materialToModify.SetVector("_LightDir1", lightCam.gameObject.transform.position);
        materialToModify.SetTexture("SahdowMap_Light", ShadowMap_Light);
        materialToModify.SetTexture("SahdowMap_View", ShadowMap_View);
        Shader.SetGlobalMatrix("_LV_Mat", View_Mat_LightP);
    }
}