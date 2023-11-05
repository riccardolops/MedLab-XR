using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Microsoft.MixedReality.Toolkit.UI.ObjectManipulator;

namespace UnityVolumeRendering.MRTK
{
    /// <summary>
    /// This is a basic runtime GUI, that can be used during play mode.
    /// You can import datasets, and edit them.
    /// Add this component to an empty GameObject in your scene (it's already in the test scene) and click play to see the GUI.
    /// </summary>
    public class RuntimeGUI_MRTK : MonoBehaviour
    {
        public FileBrowserMRTK FB;
        public AppBarVol AppBarVo;
        public TransferFunctionEditorWorld trasnsferfunctionworld;
        public SliceRendererWorld slicerendererworld;
        public BoxDisplayConfiguration BoxDisplayConfig;
        public ScaleHandlesConfiguration scaleHandleConfiguration;
        public RotationHandlesConfiguration rotationHandleConfiguration;
        public LinksConfiguration linksConfiguration;
        public ProximityEffectConfiguration proximityEffectConfiguration;
        public AudioClip manipulationStartClip;
        public AudioClip manipulationEndClip;
        public AudioClip rotationStartClip;
        public AudioClip rotationEndClip;
        public AudioClip scaleStartClip;
        public AudioClip scaleEndClip;
        private VolumeRenderedObject volObj = null;
        public GameObject loadingIndicatorGameObject;
        public GameObject loadingProgressGameObject;

        private void Start()
        {
        }
        public void importRAWdataset()
        {
            FB.ShowOpenFileDialog(OnOpenRAWDatasetResultAsync);
        }
        public void importPARCHGdataset()
        {
            FB.ShowOpenFileDialog(OnOpenPARDatasetResultAsync);
        }
        public void importDICOMdataset()
        {
            FB.ShowOpenDirectoryDialog(OnOpenDICOMDatasetResultAsync);
        }
        public void importNIFTIdataset()
        {
            FB.ShowOpenNIFTIFileDialog(OnOpenNIFTIDatasetResultAsync);
        }
        public void importNRRDdataset()
        {
            FB.ShowOpenFileDialog(OnOpenNRRDDatasetResultAsync);
        }
        private async void OnOpenPARDatasetResultAsync(FileBrowserMRTK.DialogResult result)
        {
            if (!result.cancelled)
            {
                Debug.Log("Async dataset load. Hold on.");

                DespawnAllDatasets();
                string filePath = result.path;
                IImageFileImporter parimporter = ImporterFactory.CreateImageFileImporter(ImageFileFormat.VASP);
                VolumeDataset dataset = await parimporter.ImportAsync(filePath);
                if (dataset != null)
                {
                    await VolumeObjectFactory.CreateObjectAsync(dataset);
                }
            }
        }

        private async void OnOpenRAWDatasetResultAsync(FileBrowserMRTK.DialogResult result)
        {
            if (!result.cancelled)
            {
                Debug.Log("Async dataset load. Hold on.");

                // We'll only allow one dataset at a time in the runtime GUI (for simplicity)
                DespawnAllDatasets();

                // Did the user try to import an .ini-file? Open the corresponding .raw file instead
                string filePath = result.path;
                if (System.IO.Path.GetExtension(filePath) == ".ini")
                    filePath = filePath.Substring(0, filePath.Length - 4);

                // Parse .ini file
                DatasetIniData initData = DatasetIniReader.ParseIniFile(filePath + ".ini");
                if (initData != null)
                {
                    // Import the dataset
                    RawDatasetImporter importer = new RawDatasetImporter(filePath, initData.dimX, initData.dimY, initData.dimZ, initData.format, initData.endianness, initData.bytesToSkip);
                    VolumeDataset dataset = await importer.ImportAsync();
                    // Spawn the object
                    if (dataset != null)
                    {
                        await VolumeObjectFactory.CreateObjectAsync(dataset);
                    }
                }
            }
        }

        private async void OnOpenDICOMDatasetResultAsync(FileBrowserMRTK.DialogResult result)
        {
            if (!result.cancelled)
            {
                Debug.Log("Async dataset load. Hold on.");

                // We'll only allow one dataset at a time in the runtime GUI (for simplicity)
                DespawnAllDatasets();

                bool recursive = true;

                // Read all files
                IEnumerable<string> fileCandidates = Directory.EnumerateFiles(result.path, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(p => p.EndsWith(".dcm", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicom", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicm", StringComparison.InvariantCultureIgnoreCase));

                // Import the dataset
                IImageSequenceImporter importer = ImporterFactory.CreateImageSequenceImporter(ImageSequenceFormat.DICOM);
                IEnumerable<IImageSequenceSeries> seriesList = await importer.LoadSeriesAsync(fileCandidates);
                float numVolumesCreated = 0;
                foreach (IImageSequenceSeries series in seriesList)
                {
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

        private async void OnOpenNIFTIDatasetResultAsync(FileBrowserMRTK.DialogResult result)
        {
            if (!result.cancelled)
            {
                Debug.Log("Async dataset load. Hold on.");
                using (ProgressHandler progressHandler = new ProgressHandler(new MRTKProgressView(), "NIFTI import"))
                {
                    progressHandler.ReportProgress(0.0f, "Importing NIfTI dataset");
                    IImageFileImporter importer = ImporterFactory.CreateImageFileImporter(ImageFileFormat.NIFTI);
                    VolumeDataset dataset = await importer.ImportAsync(result.path);
                    progressHandler.ReportProgress(0.5f, "Creating object");
                    if (dataset != null)
                    {
                        StartLoading();
                        volObj = await VolumeObjectFactory.CreateObjectAsync(dataset);
                        slicerendererworld.volRendObject = volObj;
                        slicerendererworld.ReloadPlanes();
                        progressHandler.ReportProgress(1.0f, "Adding interaction");
                        AddInteraction();
                        StopLoading();
                    }
                    else
                    {
                        Debug.LogError("Failed to import datset");
                    }
                }
            }
        }

        private void StartLoading()
        {
            // Show the loading indicator GameObject
            loadingIndicatorGameObject.SetActive(true);
        }

        private void StopLoading()
        {
            // Hide the loading indicator GameObject
            loadingIndicatorGameObject.SetActive(false);
        }

        private async void OnOpenNRRDDatasetResultAsync(FileBrowserMRTK.DialogResult result)
        {
            if (!result.cancelled)
            {
                IImageFileImporter importer = ImporterFactory.CreateImageFileImporter(ImageFileFormat.NRRD);
                VolumeDataset dataset = await importer.ImportAsync(result.path);

                if (dataset != null)
                {
                    volObj = await VolumeObjectFactory.CreateObjectAsync(dataset);
                    volObj.volumeContainerObject.AddComponent<BoxCollider>();
                }
                else
                {
                    Debug.LogError("Failed to import datset");
                }
            }
        }

        private void AddInteraction()
        {
            volObj.volumeContainerObject.AddComponent<BoxCollider>();
            volObj.volumeContainerObject.AddComponent<NearInteractionGrabbable>();
            volObj.volumeContainerObject.AddComponent<AudioSource>();
            volObj.volumeContainerObject.AddComponent<RotationAxisConstraint>();
            volObj.volumeContainerObject.GetComponent<RotationAxisConstraint>().ConstraintOnRotation = AxisFlags.XAxis | AxisFlags.ZAxis;
            volObj.volumeContainerObject.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
            volObj.volumeContainerObject.AddComponent<CursorContextObjectManipulator>();

            /// Add manipulator control
            Microsoft.MixedReality.Toolkit.UI.ObjectManipulator manipulator = volObj.volumeContainerObject.GetComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
            manipulator.OneHandRotationModeNear = RotateInOneHandType.RotateAboutObjectCenter;
            manipulator.OneHandRotationModeFar = RotateInOneHandType.RotateAboutObjectCenter;
            manipulator.OnManipulationStarted.AddListener(PlayStartM);
            manipulator.OnManipulationEnded.AddListener(PlayEndM);

            /// Add bounds control & configs
            BoundsControl boundsControl = volObj.volumeContainerObject.AddComponent<BoundsControl>();
            /// boundsControl.BoundsControlActivation = Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes.BoundsControlActivationType.ActivateByProximityAndPointer;
            boundsControl.BoxDisplayConfig = BoxDisplayConfig;
            boundsControl.ScaleHandlesConfig = scaleHandleConfiguration;
            boundsControl.ScaleHandlesConfig.HandleSize = 0.032f;
            boundsControl.RotationHandlesConfig = rotationHandleConfiguration;
            boundsControl.RotationHandlesConfig.HandleSize = 0.032f;
            boundsControl.LinksConfig = linksConfiguration;
            boundsControl.HandleProximityEffectConfig = proximityEffectConfiguration;
            boundsControl.RotateStarted.AddListener(PlayStartR);
            boundsControl.RotateStopped.AddListener(PlayEndR);
            boundsControl.ScaleStarted.AddListener(PlayStartS);
            boundsControl.ScaleStopped.AddListener(PlayEndS);

            ConstraintManager constraintmanager = volObj.volumeContainerObject.AddComponent<ConstraintManager>();
            constraintmanager.AutoConstraintSelection = false;
            constraintmanager.SelectedConstraints.Clear();
            boundsControl.ConstraintsManager = constraintmanager;

            /// 
            AppBarVo.volObj = volObj;
            trasnsferfunctionworld.volRendObject = volObj;
            AppBarVo.SetActive();
            trasnsferfunctionworld.SetActive();
            AppBarVo.SetDVR();
        }
        private void PlayStartR()
        {
            try
            {
                AudioSource audioSource = volObj.volumeContainerObject.GetComponent<AudioSource>();
                audioSource.PlayOneShot(rotationStartClip);
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
        private void PlayStartS()
        {
            try
            {
                AudioSource audioSource = volObj.volumeContainerObject.GetComponent<AudioSource>();
                audioSource.PlayOneShot(scaleStartClip);
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
        private void PlayStartM(ManipulationEventData arg0)
        {
            try
            {
                AudioSource audioSource = volObj.volumeContainerObject.GetComponent<AudioSource>();
                audioSource.PlayOneShot(manipulationStartClip);
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
        private void PlayEndR()
        {
            try
            {
                AudioSource audioSource = volObj.volumeContainerObject.GetComponent<AudioSource>();
                audioSource.PlayOneShot(rotationEndClip);
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
        private void PlayEndS()
        {
            try
            {
                AudioSource audioSource = volObj.volumeContainerObject.GetComponent<AudioSource>();
                audioSource.PlayOneShot(scaleEndClip);
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
        private void PlayEndM(ManipulationEventData arg0)
        {
            try
            {
                AudioSource audioSource = volObj.volumeContainerObject.GetComponent<AudioSource>();
                audioSource.PlayOneShot(manipulationEndClip);
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
        public void DespawnAllDatasets()
        {
            VolumeRenderedObject[] volobjs = GameObject.FindObjectsOfType<VolumeRenderedObject>();
            foreach (VolumeRenderedObject volobj in volobjs)
            {
                GameObject.Destroy(volobj.gameObject);
            }
        }
    }
}
