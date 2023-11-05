using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityVolumeRendering;
using UnityVolumeRendering.MRTK;
using static Microsoft.MixedReality.Toolkit.UI.ObjectManipulator;

public class MenuImportHandler : MonoBehaviour
{
    [SerializeField]
    private FileBrowserMRTK fileBrowser;
    
    [SerializeField]
    private AppBarVol appBarVo;
    [SerializeField]
    private TransferFunctionEditorWorld trasnsferfunctionworld;
    [SerializeField]
    private SliceRendererWorld slicerendererworld;

    [SerializeField]
    private MRTKProgressView progressView;

    [SerializeField]
    private GameObject controlsVolumeInported;
    [SerializeField]
    private ControllerMenuHandler controllerHandler;

    private VolumeRenderedObject volObj = null;
    private enum CurrentMode
    {
        threeD,
        twoD
    }
    private CurrentMode currentMode = CurrentMode.threeD;
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void importDICOMdataset()
    {
        fileBrowser.ShowOpenDirectoryDialog(OnOpenDICOMDatasetResultAsync);
    }
    public void importNIFTIdataset()
    {
        fileBrowser.ShowOpenNIFTIFileDialog(OnOpenNIFTIDatasetResultAsync);
    }
    public void importNRRDdataset()
    {
        fileBrowser.ShowOpenNRRDFileDialog(OnOpenNRRDDatasetResultAsync);
    }

    private async void OnOpenDICOMDatasetResultAsync(FileBrowserMRTK.DialogResult result)
    {
        if (!result.cancelled)
        {
            Debug.Log("Async dataset load. Hold on.");
            DespawnAllDatasets();
            using (ProgressHandler progressHandler = new ProgressHandler(progressView, "DICOM import"))
            {
                // Read all files
                bool recursive = true;
                IEnumerable<string> fileCandidates = Directory.EnumerateFiles(result.path, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(p => p.EndsWith(".dcm", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicom", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicm", StringComparison.InvariantCultureIgnoreCase));
                // Import the dataset
                IImageSequenceImporter importer = ImporterFactory.CreateImageSequenceImporter(ImageSequenceFormat.DICOM);
                IEnumerable<IImageSequenceSeries> seriesList = await importer.LoadSeriesAsync(fileCandidates);
                float numVolumesCreated = 0;
                int fileIndex = 0, numFiles = seriesList.Count();
                foreach (IImageSequenceSeries series in seriesList)
                {
                    progressHandler.ReportProgress(fileIndex, numFiles, $"Loading DICOM file {fileIndex} of {numFiles}");
                    VolumeDataset dataset = await importer.ImportSeriesAsync(series);
                    // Spawn the object
                    if (dataset != null)
                    {
                        VolumeRenderedObject obj = await VolumeObjectFactory.CreateObjectAsync(dataset);
                        obj.transform.position = new Vector3(numVolumesCreated, 0, 0);
                        numVolumesCreated++;
                    }
                }
            }
        }
    }

    private async void OnOpenNIFTIDatasetResultAsync(FileBrowserMRTK.DialogResult result)
    {
        if (!result.cancelled)
        {
            Debug.Log("Async dataset load. Hold on.");
            DespawnAllDatasets();
            using (ProgressHandler progressHandler = new ProgressHandler(progressView, "NIFTI import"))
            {
                progressHandler.ReportProgress(0.0f, "Importing NIfTI dataset");
                IImageFileImporter importer = ImporterFactory.CreateImageFileImporter(ImageFileFormat.NIFTI);
                VolumeDataset dataset = await importer.ImportAsync(result.path);
                progressHandler.ReportProgress(0.5f, "Creating object");
                if (dataset != null)
                {
                    volObj = await VolumeObjectFactory.CreateObjectAsync(dataset);
                    controllerHandler.SetObj(volObj);
                    slicerendererworld.volRendObject = volObj;
                    slicerendererworld.ReloadPlanes();
                    Camera playerCamera = Camera.main;
                    Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * 1.5f;
                    slicerendererworld.gameObject.transform.position = targetPosition;
                    slicerendererworld.gameObject.transform.LookAt(targetPosition + playerCamera.transform.rotation * Vector3.forward);
                    slicerendererworld.gameObject.SetActive(true);
                    progressHandler.ReportProgress(1.0f, "Adding interaction");
                    AddInteraction();
                }
                else
                {
                    Debug.LogError("Failed to import datset");
                }
            }
        }
    }
    private async void OnOpenNRRDDatasetResultAsync(FileBrowserMRTK.DialogResult result)
    {
        if (!result.cancelled)
        {
            Debug.Log("Async dataset load. Hold on.");
            DespawnAllDatasets();
            using (ProgressHandler progressHandler = new ProgressHandler(progressView, "NRRD import"))
            {
                progressHandler.ReportProgress(0.0f, "Importing NRRD dataset");
                IImageFileImporter importer = ImporterFactory.CreateImageFileImporter(ImageFileFormat.NRRD);
                VolumeDataset dataset = await importer.ImportAsync(result.path);
                progressHandler.ReportProgress(0.5f, "Creating object");
                if (dataset != null)
                {
                    volObj = await VolumeObjectFactory.CreateObjectAsync(dataset);
                    controllerHandler.SetObj(volObj);
                    slicerendererworld.volRendObject = volObj;
                    slicerendererworld.ReloadPlanes();
                    Camera playerCamera = Camera.main;
                    Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * 1.5f;
                    slicerendererworld.gameObject.transform.position = targetPosition;
                    slicerendererworld.gameObject.transform.LookAt(targetPosition + playerCamera.transform.rotation * Vector3.forward);
                    slicerendererworld.gameObject.SetActive(true);
                    progressHandler.ReportProgress(1.0f, "Adding interaction");
                    AddInteraction();
                }
                else
                {
                    Debug.LogError("Failed to import datset");
                }
            }
        }
    }
    public void DespawnAllDatasets()
    {
        VolumeRenderedObject[] volobjs = GameObject.FindObjectsOfType<VolumeRenderedObject>();
        foreach (VolumeRenderedObject volobj in volobjs)
        {
            GameObject.Destroy(volobj.gameObject);
        }
        controlsVolumeInported.SetActive(false);
    }
    private void AddInteraction()
    {
        appBarVo.volObj = volObj;
        trasnsferfunctionworld.volRendObject = volObj;
        appBarVo.SetActive();
        trasnsferfunctionworld.SetActive();
        appBarVo.SetDVR();
        controlsVolumeInported.SetActive(true);
        if (currentMode == CurrentMode.twoD)
        {
            Set2DMode();
        }
        else if (currentMode == CurrentMode.threeD)
        {
            Set3DMode();
        }
    }
    public void Set2DMode()
    {
        currentMode = CurrentMode.twoD;
        volObj.slicingPlaneCompXY.GetComponent<MeshRenderer>().enabled = true;
        volObj.slicingPlaneCompXZ.GetComponent<MeshRenderer>().enabled = true;
        volObj.slicingPlaneCompZY.GetComponent<MeshRenderer>().enabled = true;
        volObj.GetComponentsInChildren<MeshRenderer>()[0].enabled = false;
        appBarVo.gameObject.SetActive(false);
    }
    public void Set3DMode()
    {
        currentMode = CurrentMode.threeD;
        volObj.slicingPlaneCompXY.GetComponent<MeshRenderer>().enabled = false;
        volObj.slicingPlaneCompXZ.GetComponent<MeshRenderer>().enabled = false;
        volObj.slicingPlaneCompZY.GetComponent<MeshRenderer>().enabled = false;
        volObj.GetComponentsInChildren<MeshRenderer>()[0].enabled = true;
        appBarVo.gameObject.SetActive(true);
    }
    public void ToggleMode()
    {
        if (currentMode == CurrentMode.threeD)
        {
            Set2DMode();
        }
        else if (currentMode == CurrentMode.twoD)
        {
            Set3DMode();
        }
    }
}
