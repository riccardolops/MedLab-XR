using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

public class PinchSliderRange : PinchSlider
{
    [SerializeField]
    private float minValue = 0.0f; // Minimum value for the slider's range
    public float MinValue
    {
        get { return minValue; }
        set
        {
            minValue = value;
        }
    }
    [SerializeField]
    private float maxValue = 1.0f; // Maximum value for the slider's range
    public float MaxValue
    {
        get { return maxValue; }
        set
        {
            maxValue = value;
        }
    }

    public override void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        if (eventData.Pointer == ActivePointer && !eventData.used)
        {
            var delta = eventData.Pointer.Position - StartPointerPosition;
            var handDelta = Vector3.Dot(SliderTrackDirection.normalized, delta);

            // Calculate the new slider value
            float newSliderValue = StartSliderValue + handDelta / SliderTrackDirection.magnitude;

            // Ensure the new slider value stays within the defined range
            newSliderValue = Mathf.Clamp(newSliderValue, minValue, maxValue);


            SliderValue = newSliderValue;

            // Mark the pointer data as used to prevent other behaviors from handling input events
            eventData.Use();
        }
    }
    public void OnSliderUpdatedMinValue(SliderEventData eventData)
    {
        MinValue = eventData.NewValue;
    }
    public void OnSliderUpdatedMaxValue(SliderEventData eventData)
    {
        MaxValue = eventData.NewValue;
    }
}