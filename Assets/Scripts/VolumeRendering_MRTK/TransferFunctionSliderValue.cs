using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using UnityVolumeRendering.UI;

public class ShowSliderValue : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro textMesh = null;
    public ColorSlider colorSlider = null;

    public void OnSliderUpdated(SliderEventData eventData)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        if (textMesh != null)
        {
            int minDataset = (int)colorSlider._volRendObject.dataset.GetMinDataValue();
            int maxDataset = (int)colorSlider._volRendObject.dataset.GetMaxDataValue();
            int display = (int)((maxDataset - minDataset) * eventData.NewValue + minDataset);
            textMesh.text = $"{display}";
        }
    }
}
