using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityVolumeRendering;


public class MRTKProgressView : MonoBehaviour, IProgressView
{
    [SerializeField]
    private GameObject progresObject;
    [SerializeField]
    private ProgressIndicatorLoadingBar indicator;

    public void StartProgress(string title, string description)
    {
        // Activate the loading progress bar game object and handle the start status.
        progresObject.SetActive(true);
        indicator.OpenAsync();
    }

    public void FinishProgress(ProgressStatus status = ProgressStatus.Succeeded)
    {
        // Deactivate the loading progress bar game object and handle the completion status.
        progresObject.SetActive(false);
        indicator.CloseAsync();
    }

    public void UpdateProgress(float totalProgress, float currentStageProgress, string description)
    {
        // Update the progress bar percentage and description based on the provided values.
        indicator.Progress = totalProgress;
        indicator.Message = description;
    }
}