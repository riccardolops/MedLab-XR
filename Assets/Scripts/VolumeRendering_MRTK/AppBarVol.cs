using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityVolumeRendering;
using RenderMode = UnityVolumeRendering.RenderMode;

public class AppBarVol : MonoBehaviour
{
    #region Enum Definitions

    [Flags]

    public enum AppBarStateEnum
    {
        None = 0,
        DirectVolumetric,
        MaximumIntensity,
        Isosurface
    }

    #endregion
    public PinchSliderRange minVVR;
    public PinchSliderRange maxVVR;
    public GameObject GVT;
    public GameObject LightingButtonGameObject;
    public GameObject RayButtonGameObject;
    public VolumeRenderedObject volObj = null;
    private bool cubicInterpolBool = false;
    private bool LightingBool = false;
    private bool rayterminationBool = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void SetRenderMode(int mode)
    {
        volObj.SetRenderMode((RenderMode)mode);
    }
    public void SetDVR()
    {
        SetRenderMode(0);
        resetSlider();
        GVT.SetActive(false);
        LightingButtonGameObject.SetActive(true);
        RayButtonGameObject.SetActive(true);
    }
    public void SetMIP()
    {
        SetRenderMode(1);
        resetSlider();
        GVT.SetActive(false);
        LightingButtonGameObject.SetActive(false);
        RayButtonGameObject.SetActive(false);
    }
    public void SetIR()
    {
        SetRenderMode(2);
        resetSlider();
        GVT.SetActive(true);
        LightingButtonGameObject.SetActive(false);
        RayButtonGameObject.SetActive(false);
    }
    public void SetVisibleValueRangeMin(SliderEventData eventData)
    {
        volObj.SetVisibilityWindow(eventData.NewValue, volObj.GetVisibilityWindow().y);
    }
    public void SetVisibleValueRangeMax(SliderEventData eventData)
    {
        volObj.SetVisibilityWindow(volObj.GetVisibilityWindow().x, eventData.NewValue);
    }
    public void SetGradientVisibility(SliderEventData eventData)
    {
        volObj.SetGradientVisibilityThreshold(eventData.NewValue);
    }
    public void SetActive()
    {
        gameObject.SetActive(true);
    }
    private void resetSlider()
    {
        minVVR.SliderValue = 0;
        maxVVR.SliderValue = 1;
    }
    public void ToggleCubicInterpolation()
    {
        cubicInterpolBool = !cubicInterpolBool;
        volObj.SetCubicInterpolationEnabled(cubicInterpolBool);
    }
    public void ToggleLighting()
    {
        LightingBool = !LightingBool;
        volObj.SetLightingEnabled(LightingBool);
    }
    public void ToggleEarlyRayTermination()
    {
        rayterminationBool = !rayterminationBool;
        volObj.SetRayTerminationEnabled(rayterminationBool);
    }
}
