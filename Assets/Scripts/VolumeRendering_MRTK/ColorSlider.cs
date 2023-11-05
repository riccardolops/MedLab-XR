#region Includes
using Microsoft.MixedReality.Toolkit.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityVolumeRendering.MRTK;
#endregion

namespace UnityVolumeRendering.UI
{
    [RequireComponent(typeof(PinchSliderRange))]
    public class ColorSlider : MonoBehaviour
    {
        #region Variables

        private PinchSliderRange _slider;
        private int _index;
        public VolumeRenderedObject _volRendObject;
        private TransferFunctionEditorWorld _transferFunctionWorld;

        public float Value
        {
            get { return _slider.SliderValue; }
            set
            {
                _slider.SliderValue = value;
            }
        }
        private FlexibleColorPicker _FCP = null;

        #endregion

        private void Awake()
        {
            if (!TryGetComponent<PinchSliderRange>(out _slider))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("Missing PinchSliderRange Component");
#endif
            }
        }

        public void Setup(float value, int index, TransferFunctionEditorWorld transferFunctionWorld)
        {
            _transferFunctionWorld = transferFunctionWorld;
            _index = index;
            _volRendObject = transferFunctionWorld.volRendObject;
            _FCP = transferFunctionWorld.FCP;
            Value = value;
        }

        public void Slider_OnValueChanged(SliderEventData eventData)
        {
            _transferFunctionWorld.ColorPointActiveIndex = _index;
            TransferFunction tf = _volRendObject.transferFunction;
            TFColourControlPoint colPoint = tf.colourControlPoints[_index];
            _FCP.SetColor(colPoint.colourValue);
            colPoint.dataValue = eventData.NewValue;
            tf.colourControlPoints[_index] = colPoint;
            _volRendObject.transferFunction = tf;
            if (_transferFunctionWorld.state == TransferFunctionEditorWorld.State.Windowing)
            {
                if (_index == 0)
                {
                    _transferFunctionWorld.UpdateWMin(eventData.NewValue);
                }
                else if (_index == 1)
                {
                    _transferFunctionWorld.UpdateWMax(eventData.NewValue);
                }
                
            }
        }

    }
}