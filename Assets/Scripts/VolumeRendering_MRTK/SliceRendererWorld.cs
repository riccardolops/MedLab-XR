using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Dummiesman;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityVolumeRendering;
using UnityVolumeRendering.MRTK;
using RenderMode = UnityEngine.RenderMode;

public class SliceRendererWorld : MonoBehaviour
{
    #region Variables
    public SlicingPlane planeXY = null;
    public GameObject imageXY;
    public TextMeshPro textXY;
    public SlicingPlane planeXZ = null;
    public GameObject imageXZ;
    public TextMeshPro textXZ;
    public SlicingPlane planeZY = null;
    public GameObject imageZY;
    public TextMeshPro textZY;
    public VolumeRenderedObject volRendObject = null;
    public RectTransform contentRect;
    public Canvas canvas;
    public TransferFunctionEditorWorld tfew;
    private GameObject focusedImage;
    private TextMeshPro focusedtextMesh;
    private SlicingPlane focusedPlane;

    private InputMode inputMode;

    [SerializeField]
    private Material RGMaterial;

    private enum InputMode
    {
        Inspect,
        Measure,
        RegionGrowing
    }
    #endregion
    void Start()
    {
        
    }
    void Update()
    {
        
    }
    public void SetMaterials()
    {
        float maxh = contentRect.rect.height;
        float maxw = contentRect.rect.width/3;
        float newWidth;
        float newHight;

        imageXY.GetComponent<Image>().material = planeXY.GetComponent<MeshRenderer>().material;
        imageXZ.GetComponent<Image>().material = planeXZ.GetComponent<MeshRenderer>().material;
        imageZY.GetComponent<Image>().material = planeZY.GetComponent<MeshRenderer>().material;
        Vector3 planeXYScale = planeXY.transform.lossyScale;
        float heightWidthRatioXY = Mathf.Abs(planeXYScale.z / planeXYScale.x);
        if (heightWidthRatioXY > 1)
        {
            newWidth = maxh / heightWidthRatioXY;
            newHight = maxh;
        }
        else if (heightWidthRatioXY < 1)
        {
            newWidth = maxw;
            newHight = maxw * heightWidthRatioXY;
        }
        else
        {
            newWidth = maxw;
            newHight = maxh;
        }
        imageXY.GetComponent<RectTransform>().sizeDelta = new Vector2(newWidth, newHight);


        Vector3 planeXZScale = planeXZ.transform.lossyScale;
        float heightWidthRatioXZ = Mathf.Abs(planeXZScale.z / planeXZScale.x);
        if (heightWidthRatioXZ > 1)
        {
            newWidth = maxh / heightWidthRatioXZ;
            newHight = maxh;
        }
        else if (heightWidthRatioXZ < 1)
        {
            newWidth = maxw;
            newHight = maxw * heightWidthRatioXZ;
        }
        else
        {
            newWidth = maxw;
            newHight = maxh;
        }
        imageXZ.GetComponent<RectTransform>().sizeDelta = new Vector2(newWidth, newHight);



        Vector3 planeZYScale = planeZY.transform.lossyScale;
        float heightWidthRatioZY = Mathf.Abs(planeZYScale.z / planeZYScale.x);
        if (heightWidthRatioZY > 1)
        {
            newWidth = maxh / heightWidthRatioZY;
            newHight = maxh;
        }
        else if (heightWidthRatioZY < 1)
        {
            newWidth = maxw;
            newHight = maxw * heightWidthRatioZY;
        }
        else
        {
            newWidth = maxw;
            newHight = maxh;
        }

        imageZY.GetComponent<RectTransform>().sizeDelta = new Vector2(newWidth, newHight);
    }
    public void ReloadPlanes()
    {
        if (planeXY != null)
            Destroy(planeXY.gameObject);
        if (planeXZ != null)
            Destroy(planeXZ.gameObject);
        if (planeZY != null)
            Destroy(planeZY.gameObject);
        planeXY = volRendObject.CreateSlicingPlaneXY();
        planeXZ = volRendObject.CreateSlicingPlaneXZ();
        planeZY = volRendObject.CreateSlicingPlaneZY();
        SetMaterials();
    }
    public void SetFocusedImage(string image)
    {
        if (image == "XY")
        {
            focusedImage = imageXY;
            focusedtextMesh = textXY;
            focusedPlane = planeXY;
        }
        else if (image == "XZ")
        {
            focusedImage = imageXZ;
            focusedtextMesh = textXZ;
            focusedPlane = planeXZ;
        }
        else if (image == "ZY")
        {
            focusedImage = imageZY;
            focusedtextMesh = textZY;
            focusedPlane = planeZY;
        }
        else
        {
            focusedImage = null;
            focusedtextMesh = null;
            focusedPlane = null;
        }
    }
    public void SetValueInputMode()
    {
        inputMode = InputMode.Inspect;
    }
    public void SetValueMeasureMode()
    {
        inputMode = InputMode.Measure;
    }
    public void SetValueRGMode()
    {
        inputMode = InputMode.RegionGrowing;
    }
    public void OnSliderXYChanged(SliderEventData e)
    {
        planeXY.transform.localPosition = new Vector3(0, e.NewValue - 0.5f, 0);
    }
    public void OnSliderXZChanged(SliderEventData e)
    {
        planeXZ.transform.localPosition = new Vector3(0, 0, e.NewValue - 0.5f);
    }
    public void OnSliderZYChanged(SliderEventData e)
    {
        planeZY.transform.localPosition = new Vector3(e.NewValue - 0.5f, 0, 0);
    }
    public async void MouseDown(BaseEventData e)
    {
        if (focusedImage != null)
        {
            if (inputMode == InputMode.Inspect)
            {
                Vector2 v = GetNormalizedPointerPosition(canvas, focusedImage.GetComponent<RectTransform>(), e);
                float value = GetValueAtPosition(v, focusedPlane);
                Vector3Int position = GetPositionAtPosition(v, focusedPlane);
                focusedtextMesh.text = $"{position}: {value}";
            }
            else if (inputMode == InputMode.RegionGrowing)
            {
                Vector2 v = GetNormalizedPointerPosition(canvas, focusedImage.GetComponent<RectTransform>(), e);
                uint[] seed = GetSeedAtPosition(v, focusedPlane);
                RegionGrowing regionGrowing = new RegionGrowing();
                string dir = await regionGrowing.regiongrowing(volRendObject.dataset.filePath, tfew.wmin, tfew.wmax, seed);
                GameObject loadedObj = new OBJLoader().Load(dir + "/mesh.obj", dir + "/mesh.mtl");
                GameObject childObject = loadedObj.transform.GetChild(0).gameObject;
                childObject.GetComponent<MeshRenderer>().material = RGMaterial;
                childObject.transform.localScale *= 0.001f;
                childObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f) * childObject.transform.rotation;
                Camera playerCamera = Camera.main;
                childObject.transform.position = playerCamera.transform.position + playerCamera.transform.forward * 0.7f;
                childObject.AddComponent<BoxCollider>();
                childObject.AddComponent<NearInteractionGrabbable>();
                childObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
                childObject.AddComponent<CursorContextObjectManipulator>();
                childObject.AddComponent<AudioSource>();
                
            }
        }
    }
    public void OnPointerEnter(BaseEventData e)
    {
        Vector2 v = GetNormalizedPointerPosition(canvas, focusedImage.GetComponent<RectTransform>(), e);
        float value = GetValueAtPosition(v, focusedPlane);
        Vector3Int position = GetPositionAtPosition(v, focusedPlane);
        UnityEngine.Debug.Log(value + " " + position);
    }

    private float GetValueAtPosition(Vector2 relativeMousePosition, SlicingPlane slicingPlane)
    {
        Vector3 worldSpacePosition = GetWorldPosition(relativeMousePosition, slicingPlane);
        Vector3 objSpacePoint = slicingPlane.targetObject.volumeContainerObject.transform.InverseTransformPoint(worldSpacePosition);
        VolumeDataset dataset = slicingPlane.targetObject.dataset;
        // Convert to texture coordinates.
        Vector3 uvw = objSpacePoint + Vector3.one * 0.5f;
        // Look up data value at current position.
        Vector3Int index = new Vector3Int((int)(uvw.x * dataset.dimX), (int)(uvw.y * dataset.dimY), (int)(uvw.z * dataset.dimZ));
        index.x = Mathf.Clamp(index.x, 0, dataset.dimX - 1);
        index.y = Mathf.Clamp(index.y, 0, dataset.dimY - 1);
        index.z = Mathf.Clamp(index.z, 0, dataset.dimZ - 1);
        return dataset.GetData(index.x, index.y, index.z);
    }
    private Vector3Int GetPositionAtPosition(Vector2 relativeMousePosition, SlicingPlane slicingPlane)
    {
        Vector3 worldSpacePosition = GetWorldPosition(relativeMousePosition, slicingPlane);
        Vector3 objSpacePoint = slicingPlane.targetObject.volumeContainerObject.transform.InverseTransformPoint(worldSpacePosition);
        VolumeDataset dataset = slicingPlane.targetObject.dataset;
        // Convert to texture coordinates.
        Vector3 uvw = objSpacePoint + Vector3.one * 0.5f;
        // Look up data value at current position.
        Vector3Int index = new Vector3Int((int)(uvw.x * dataset.dimX), (int)(uvw.y * dataset.dimY), (int)(uvw.z * dataset.dimZ));
        index.x = Mathf.Clamp(index.x, 0, dataset.dimX - 1);
        index.y = Mathf.Clamp(index.y, 0, dataset.dimY - 1);
        index.z = Mathf.Clamp(index.z, 0, dataset.dimZ - 1);
        return index;
    }
    private uint[] GetSeedAtPosition(Vector2 relativeMousePosition, SlicingPlane slicingPlane)
    {
        Vector3 worldSpacePosition = GetWorldPosition(relativeMousePosition, slicingPlane);
        Vector3 objSpacePoint = slicingPlane.targetObject.volumeContainerObject.transform.InverseTransformPoint(worldSpacePosition);
        VolumeDataset dataset = slicingPlane.targetObject.dataset;
        // Convert to texture coordinates.
        Vector3 uvw = objSpacePoint + Vector3.one * 0.5f;
        // Look up data value at current position.
        Vector3Int index = new Vector3Int((int)(uvw.x * dataset.dimX), (int)(uvw.y * dataset.dimY), (int)(uvw.z * dataset.dimZ));
        index.x = Mathf.Clamp(index.x, 0, dataset.dimX - 1);
        index.y = Mathf.Clamp(index.y, 0, dataset.dimY - 1);
        index.z = Mathf.Clamp(index.z, 0, dataset.dimZ - 1);
        return new uint[] { (uint)index.x, (uint)index.y, (uint)index.z };
    }
    private Vector3 GetWorldPosition(Vector2 relativeMousePosition, SlicingPlane slicingPlane)
    {
        Vector3 planePoint = new Vector3(0.5f - relativeMousePosition.x, 0.0f, relativeMousePosition.y - 0.5f) * 10.0f;
        return slicingPlane.transform.TransformPoint(planePoint);
    }
    private static Vector2 GetNormalizedPointerPosition(Canvas canvas, RectTransform rect, BaseEventData e)
    {
        switch (canvas.renderMode)
        {

            case RenderMode.ScreenSpaceCamera:
                if (canvas.worldCamera == null)
                    return GetNormScreenSpace(rect, e);
                else
                    return GetNormWorldSpace(canvas, rect, e);

            case RenderMode.ScreenSpaceOverlay:
                return GetNormScreenSpace(rect, e);

            case RenderMode.WorldSpace:
                if (canvas.worldCamera == null)
                {
                    UnityEngine.Debug.LogError("FCP in world space render mode requires an event camera to be set up on the parent canvas!");
                    return Vector2.zero;
                }
                return GetNormWorldSpace(canvas, rect, e);

            default: return Vector2.zero;

        }
    }
    /// <summary>
    /// Get normalized position in the case of a screen space (overlay) 
    /// type canvas render mode
    /// </summary>
    private static Vector2 GetNormScreenSpace(RectTransform rect, BaseEventData e)
    {
        Vector2 screenPoint = ((PointerEventData)e).position;
        Vector2 localPos = rect.worldToLocalMatrix.MultiplyPoint(screenPoint);
        float x = Mathf.Clamp01((localPos.x / rect.rect.size.x) + rect.pivot.x);
        float y = Mathf.Clamp01((localPos.y / rect.rect.size.y) + rect.pivot.y);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Get normalized position in the case of a world space (or screen space camera) 
    /// type cavnvas render mode.
    /// </summary>
    private static Vector2 GetNormWorldSpace(Canvas canvas, RectTransform rect, BaseEventData e)
    {
        Vector2 screenPoint = ((PointerEventData)e).position;
        Ray pointerRay = canvas.worldCamera.ScreenPointToRay(screenPoint);
        Plane canvasPlane = new Plane(canvas.transform.forward, canvas.transform.position);
        float enter;
        canvasPlane.Raycast(pointerRay, out enter);
        Vector3 worldPoint = pointerRay.origin + enter * pointerRay.direction;
        Vector2 localPoint = rect.worldToLocalMatrix.MultiplyPoint(worldPoint);

        float x = Mathf.Clamp01((localPoint.x / rect.rect.size.x) + rect.pivot.x);
        float y = Mathf.Clamp01((localPoint.y / rect.rect.size.y) + rect.pivot.y);
        return new Vector2(x, y);
    }
}
