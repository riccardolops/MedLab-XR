using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityVolumeRendering.UI;
using Image = UnityEngine.UI.Image;


namespace UnityVolumeRendering.MRTK
{
    using RenderMode = UnityEngine.RenderMode;
    public class TransferFunctionEditorWorld : MonoBehaviour
    {
        #region Variables
        // Private fields
        private Texture2D histTex = null;
        private Material tfGUIMat = null;
        private Material tfPaletteGUIMat = null;
        public FlexibleColorPicker FCP = null;
        public TransferFunction tf = null;
        public Canvas canvas = null;
        [SerializeField]
        private Image[] pickers;
        private enum PickerType
        {
            histogram, colorpalette
        }
        private Image focusedImage;
        private PickerType focusedPickerType;
        private int PickedAlphaControlPoint = -1;


        [Header("References")]
        public RectTransform _fillArea;
        [SerializeField]
        private MenuImportHandler menuImportHandler;
        [SerializeField]
        private GameObject ColorPrefab;
        [SerializeField]
        private GameObject AlphaPrefab;
        [SerializeField]
        private GameObject WindowingPrefab;
        public enum State{
            Coloring,
            Windowing
        }
        public State state = State.Coloring;
        public Transform parentColor;
        public RectTransform parentAlpha;
        public int ColorPointActiveIndex = -1;
        public FileBrowserMRTK FB = null;
        public SliceRendererWorld slicerendererworld;
        public GameObject saveButton;
        public GameObject loadButton;
        public GameObject clearButton;
        public GameObject wwText;
        public GameObject wlText;
        public GameObject regionGrowingButton;

        [Header("Configuration")]
        /// <summary>
        /// Select Volume Rendered Object.
        /// </summary>
        public VolumeRenderedObject volRendObject = null;
        [SerializeField] private bool _setupOnStart;

        private List<GameObject> _slidersColor = new List<GameObject>();
        private List<GameObject> _slidersAlpha = new List<GameObject>();
        #endregion
        public int wmin;
        public int wmax;
        private int wlevel;
        private int wwidth;
        private int mindataValue;
        private int maxdataValue;


        private void Start()
        {
            if (!_setupOnStart) { return; }
            Setup();
        }
        private void Update()
        {
            if (ColorPointActiveIndex != -1)
            {
                TFColourControlPoint colPoint = volRendObject.transferFunction.colourControlPoints[ColorPointActiveIndex];
                colPoint.colourValue = FCP.GetColor();
                volRendObject.transferFunction.colourControlPoints[ColorPointActiveIndex] = colPoint;
            }
            renderTextures();
        }

        private void Setup()
        {
            tf = volRendObject.transferFunction;
            renderTextures();

            // Draw colour control points
            for (int iCol = 0; iCol < tf.colourControlPoints.Count; iCol++)
            {
                TFColourControlPoint colPoint = tf.colourControlPoints[iCol];
                _slidersColor.Add(Instantiate(ColorPrefab, parentColor));
                _slidersColor[iCol].GetComponent<ColorSlider>().Setup(colPoint.dataValue, iCol, this);
            }
            for (int iAlp = 0; iAlp < tf.alphaControlPoints.Count; iAlp++)
            {
                TFAlphaControlPoint alphaPoint = tf.alphaControlPoints[iAlp];
                _slidersAlpha.Add(Instantiate(AlphaPrefab, parentAlpha));
                _slidersAlpha[iAlp].transform.localPosition = moveAlphaPoint(alphaPoint.dataValue, alphaPoint.alphaValue);
            }
            slicerendererworld.ReloadPlanes();

        }
        private Vector3 moveAlphaPoint(float x, float y)
        {
            float xPos = (parentAlpha.rect.width * x) - (parentAlpha.rect.width / 2);
            float yPos = (parentAlpha.rect.height * y) - (parentAlpha.rect.height / 2);
            return new Vector3(xPos, yPos, -0.2f);
        }
        public void SetPointerFocus(int i)
        {
            if (i < 0 || i >= pickers.Length)
                Debug.LogWarning("No picker image available of type " + (PickerType)i +
                    ". Did you assign all the picker images in the editor?");
            else
                focusedImage = pickers[i];
            focusedPickerType = (PickerType)i;
        }


        /// <summary>
        /// Update color based on the pointer position in the currently focused picker.
        /// </summary>
        /// <param name="e">Pointer event</param>
        public void PointerUpdate(BaseEventData e)
        {
            Vector2 v = GetNormalizedPointerPosition(canvas, focusedImage.rectTransform, e);
            switch (focusedPickerType)
            {
                case PickerType.histogram:
                    Debug.LogWarning("Undefined");
                    // Mouse down => Move or remove selected alpha control point
                    PickedAlphaControlPoint = PickAlphaControlPoint(v);
                    // Move selected alpha control point
                    if (PickedAlphaControlPoint != -1)
                    {
                        TFAlphaControlPoint alphaPoint = tf.alphaControlPoints[PickedAlphaControlPoint];
                        alphaPoint.dataValue = v.x;
                        alphaPoint.alphaValue = v.y;
                        tf.alphaControlPoints[PickedAlphaControlPoint] = alphaPoint;
                    }

                    break;
                case PickerType.colorpalette:
                    switch (state)
                    {
                        case State.Coloring:
                            CreateColorPoint(v.x);
                            break;
                        case State.Windowing:
                            break;
                    }
                    break;
                default:
                    throw new Exception("Picker type " + focusedPickerType + " is not associated with an image.");
            }
        }
        public void PointerUpdateClick(BaseEventData e)
        {
            Vector2 v = GetNormalizedPointerPosition(canvas, focusedImage.rectTransform, e);
            PickedAlphaControlPoint = PickAlphaControlPoint(v);
            if (PickedAlphaControlPoint == -1 && state == State.Coloring)
            {
                CreateAlphaPoint(v);
            }
        }
        public void PointerUpdateDrag(BaseEventData e)
        {
            Vector2 v = GetNormalizedPointerPosition(canvas, focusedImage.rectTransform, e);
            if (PickedAlphaControlPoint != -1)
            {
                TFAlphaControlPoint alphaPoint = tf.alphaControlPoints[PickedAlphaControlPoint];
                alphaPoint.dataValue = v.x;
                alphaPoint.alphaValue = v.y;
                tf.alphaControlPoints[PickedAlphaControlPoint] = alphaPoint;
                _slidersAlpha[PickedAlphaControlPoint].transform.localPosition = moveAlphaPoint(alphaPoint.dataValue, alphaPoint.alphaValue);
            }
        }
        public void PointerUpdateUnClick(BaseEventData e)
        {
            Vector2 v = GetNormalizedPointerPosition(canvas, focusedImage.rectTransform, e);
            PickedAlphaControlPoint = -1;
        }
        public void SetActive()
        {
            gameObject.SetActive(true);
            Setup();
        }
        private int PickAlphaControlPoint(Vector2 position, float maxDistance = 0.05f)
        {
            TransferFunction tf = volRendObject.transferFunction;
            int nearestPointIndex = -1;
            float nearestDist = 1000.0f;
            for (int i = 0; i < tf.alphaControlPoints.Count; i++)
            {
                TFAlphaControlPoint ctrlPoint = tf.alphaControlPoints[i];
                Vector2 ctrlPos = new Vector2(ctrlPoint.dataValue, ctrlPoint.alphaValue);
                float dist = (ctrlPos - position).magnitude;
                if (dist < maxDistance && dist < nearestDist)
                {
                    nearestPointIndex = i;
                    nearestDist = dist;
                }
            }
            return nearestPointIndex;
        }
        public void Clear()
        {
            ColorPointActiveIndex = -1;
            tf = ScriptableObject.CreateInstance<TransferFunction>();
            tf.alphaControlPoints.Add(new TFAlphaControlPoint(0.2f, 0.0f));
            tf.alphaControlPoints.Add(new TFAlphaControlPoint(0.8f, 1.0f));
            tf.colourControlPoints.Add(new TFColourControlPoint(0.5f, new UnityEngine.Color(0.469f, 0.354f, 0.223f, 1.0f)));
            volRendObject.SetTransferFunction(tf);
            foreach (GameObject slider in _slidersColor)
            {
                Destroy(slider);
            }
            _slidersColor.Clear();
            foreach (GameObject slider in _slidersAlpha)
            {
                Destroy(slider);
            }
            _slidersAlpha.Clear();
            
            Setup();
        }
        private void SetWindowingMode()
        {
            menuImportHandler.Set2DMode();
            state = State.Windowing;
            ColorPointActiveIndex = -1;
            tf = ScriptableObject.CreateInstance<TransferFunction>();
            tf.alphaControlPoints.Add(new TFAlphaControlPoint(1.0f, 0.0f));
            tf.colourControlPoints.Add(new TFColourControlPoint(0.0f, UnityEngine.Color.black));
            tf.colourControlPoints.Add(new TFColourControlPoint(1.0f, UnityEngine.Color.white));
            volRendObject.SetTransferFunction(tf);
            foreach (GameObject slider in _slidersColor)
            {
                Destroy(slider);
            }
            _slidersColor.Clear();
            foreach (GameObject slider in _slidersAlpha)
            {
                Destroy(slider);
            }
            _slidersAlpha.Clear();
            renderTextures();


            _slidersColor.Add(Instantiate(WindowingPrefab, parentColor));
            
            mindataValue = (int)volRendObject.dataset.GetMinDataValue();
            maxdataValue = (int)volRendObject.dataset.GetMaxDataValue();

            Transform child1 = _slidersColor[0].transform.Find("TouchSliderMin");
            Transform child2 = _slidersColor[0].transform.Find("TouchSliderMax");
            TFColourControlPoint colPoint = tf.colourControlPoints[0];
            child1.GetComponent<ColorSlider>().Setup(colPoint.dataValue, 0, this);
            wmin = (int)((maxdataValue - mindataValue) * colPoint.dataValue + mindataValue);
            
            colPoint = tf.colourControlPoints[1];
            child2.GetComponent<ColorSlider>().Setup(colPoint.dataValue, 1, this);
            wmax = (int)((maxdataValue - mindataValue) * colPoint.dataValue + mindataValue);
            wwidth = wmax - wmin;
            wlevel = (int)((wmax + wmin)/2);
            slicerendererworld.ReloadPlanes();
            regionGrowingButton.SetActive(true);
            wwText.SetActive(true);
            wwText.GetComponent<TextMeshPro>().text = "Window Width: " + wwidth.ToString();
            wlText.SetActive(true);
            wlText.GetComponent<TextMeshPro>().text = "Window Level: " + wlevel.ToString();
            saveButton.SetActive(false);
            loadButton.SetActive(false);
            clearButton.SetActive(false);
        }
        public void UpdateWMin(float value)
        {
            wmin = (int)((maxdataValue - mindataValue) * value + mindataValue);
            wwidth = wmax - wmin;
            wlevel = (int)((wmax + wmin)/2);
            wwText.GetComponent<TextMeshPro>().text = "Window Width: " + wwidth.ToString();
            wlText.GetComponent<TextMeshPro>().text = "Window Level: " + wlevel.ToString();
        }
        public void UpdateWMax(float value)
        {
            wmax = (int)((maxdataValue - mindataValue) * value + mindataValue);
            wwidth = wmax - wmin;
            wlevel = (int)((wmax + wmin)/2);
            wwText.GetComponent<TextMeshPro>().text = "Window Width: " + wwidth.ToString();
            wlText.GetComponent<TextMeshPro>().text = "Window Level: " + wlevel.ToString();
        }
        private void UnsetWindowingMode()
        {
            state = State.Coloring;
            Clear();
            regionGrowingButton.SetActive(false);
            wwText.SetActive(false);
            wlText.SetActive(false);
            saveButton.SetActive(true);
            loadButton.SetActive(true);
            clearButton.SetActive(true);
        }
        public void ToggleWindowingMode()
        {
            if (state == State.Coloring)
            {
                SetWindowingMode();
            }
            else
            {
                UnsetWindowingMode();
            }
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
                        Debug.LogError("FCP in world space render mode requires an event camera to be set up on the parent canvas!");
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

        public void CreateColorPoint(float value)
        {
            TFColourControlPoint newColPoint = new TFColourControlPoint();
            tf.colourControlPoints.Add(newColPoint);
            newColPoint.dataValue = value;
            _slidersColor.Add(Instantiate(ColorPrefab, parentColor));
            _slidersColor[_slidersColor.Count - 1].GetComponent<ColorSlider>().Setup(newColPoint.dataValue, tf.colourControlPoints.Count - 1, this);
        }
        public void CreateAlphaPoint(Vector2 values)
        {
            tf.alphaControlPoints.Add(new TFAlphaControlPoint(values.x, values.y));
            
            _slidersAlpha.Add(Instantiate(AlphaPrefab, parentAlpha));
            _slidersAlpha[_slidersAlpha.Count - 1].transform.localPosition = moveAlphaPoint(values.x, values.y);
        }


        public void SaveTransferFunction()
        {
            FB.ShowSaveTFDialog(OnSaveTransferFunctionResultAsync);
        }
        private void OnSaveTransferFunctionResultAsync(FileBrowserMRTK.DialogResult result)
        {
            if (!result.cancelled)
            {
                string filepath = result.path;
                if(filepath != "")
                    TransferFunctionDatabase.SaveTransferFunction(tf, filepath);
            }
        }
        public void LoadTransferFunction()
        {
            FB.ShowOpenTFDialog(OnLoadTransferFunctionResultAsync);
        }
        private void OnLoadTransferFunctionResultAsync(FileBrowserMRTK.DialogResult result)
        {
            if (!result.cancelled)
            {
                string filepath = result.path;
                if(filepath != "")
                {
                    TransferFunction newTF = TransferFunctionDatabase.LoadTransferFunction(filepath);
                    if(newTF != null)
                    {
                        tf = newTF;
                        volRendObject.SetTransferFunction(tf);
                        foreach (GameObject slider in _slidersColor)
                        {
                            Destroy(slider);
                        }
                        _slidersColor.Clear();
                        foreach (GameObject slider in _slidersAlpha)
                        {
                            Destroy(slider);
                        }
                        _slidersAlpha.Clear();
                        Setup();
                    }
                }
            }
        }
        public void renderTextures()
        {
            if (volRendObject == null)
                return;
            tfGUIMat = Resources.Load<Material>("TransferFunctionGUIMat");
            tfPaletteGUIMat = Resources.Load<Material>("TransferFunctionPaletteGUIMat");
            tf.GenerateTexture();
            if (histTex == null)
            {
                if (SystemInfo.supportsComputeShaders)
                    histTex = HistogramTextureGenerator.GenerateHistogramTextureOnGPU(volRendObject.dataset);
                else
                    histTex = HistogramTextureGenerator.GenerateHistogramTexture(volRendObject.dataset);
            }

            // Draw histogram
            tfGUIMat.SetTexture("_TFTex", tf.GetTexture());
            tfGUIMat.SetTexture("_HistTex", histTex);
            tfPaletteGUIMat.SetTexture("_TFTex", tf.GetTexture());
        }
    }
}