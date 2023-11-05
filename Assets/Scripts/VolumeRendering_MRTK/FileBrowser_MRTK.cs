using UnityEngine;
using System.Collections;
using System.IO;
using SimpleFileBrowser;
using Microsoft.MixedReality.Toolkit.UI;


namespace UnityVolumeRendering.MRTK
{
    /// <summary>
    /// Runtime filebrowser.
    /// Opens a save/load file/directory browser that can be used during play-mode.
    /// </summary>
    public partial class FileBrowserMRTK : MonoBehaviour
    {
        public struct DialogResult
        {
            /// <summary>
            /// The user cancelled. Path will be invalid.
            /// </summary>
            public bool cancelled;

            /// <summary>
            /// The path of the file or directory.
            /// </summary>
            public string path;
        }
        public DialogCallback callback = null;

        public delegate void DialogCallback(DialogResult result);

        /// <summary>
        /// Show a dialog for opening a file
        /// </summary>
        /// <param name="resultCallback">Callback function called when the user has selected a file path</param>
        /// <param name="directory">Path of the file to open</param>
        public void ShowOpenFileDialog(DialogCallback resultCallback)
        {
            gameObject.SetActive(true);
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);
            callback = resultCallback;
            StartCoroutine(ShowLoadDialogCoroutine(FileBrowser.PickMode.Files));
        }
        public void ShowOpenNIFTIFileDialog(DialogCallback resultCallback)
        {
            gameObject.SetActive(true);
            gameObject.GetComponent<FollowMeToggle>().ToggleFollowMeBehavior();
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);
            FileBrowser.SetFilters( true, new FileBrowser.Filter( "NIfTI", ".nii.gz" ) );
            FileBrowser.SetDefaultFilter( ".nii.gz" );
            callback = resultCallback;
            StartCoroutine(ShowLoadDialogCoroutine(FileBrowser.PickMode.Files));
        }
        public void ShowOpenNRRDFileDialog(DialogCallback resultCallback)
        {
            gameObject.SetActive(true);
            gameObject.GetComponent<FollowMeToggle>().ToggleFollowMeBehavior();
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);
            FileBrowser.SetFilters( true, new FileBrowser.Filter( "NRRD", ".nrrd" ) );
            FileBrowser.SetDefaultFilter( ".nrrd" );
            callback = resultCallback;
            StartCoroutine(ShowLoadDialogCoroutine(FileBrowser.PickMode.Files));
        }
        public void ShowOpenTFDialog(DialogCallback resultCallback)
        {
            gameObject.SetActive(true);
            gameObject.GetComponent<FollowMeToggle>().ToggleFollowMeBehavior();
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);
            FileBrowser.SetFilters( true, new FileBrowser.Filter( "Transfer Function", ".tf", ".json" ) );
            FileBrowser.SetDefaultFilter( ".tf" );
            callback = resultCallback;
            StartCoroutine(ShowLoadDialogCoroutine(FileBrowser.PickMode.Files));
        }

        /// <summary>
        /// Show a dialog for saving a file
        /// </summary>
        /// <param name="resultCallback">Callback function called when the user has selected a file path</param>
        /// <param name="directory">The selected file path</param>
        public void ShowSaveFileDialog(DialogCallback resultCallback)
        {
            gameObject.SetActive(true);
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);
            callback = resultCallback;
            StartCoroutine(ShowSaveDialogCoroutine(FileBrowser.PickMode.Files));
        }
        
        public void ShowSaveTFDialog(DialogCallback resultCallback)
        {
            gameObject.SetActive(true);
            gameObject.GetComponent<FollowMeToggle>().ToggleFollowMeBehavior();
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);
            FileBrowser.SetFilters( true, new FileBrowser.Filter( "Transfer Function", ".tf", ".json" ) );
            FileBrowser.SetDefaultFilter( ".tf" );
            callback = resultCallback;
            StartCoroutine(ShowSaveDialogCoroutine(FileBrowser.PickMode.Files));
        }
        /// <summary>
        /// Show a dialog for opening a directory
        /// </summary>
        /// <param name="resultCallback">Callback function called when the user has selected a directory</param>
        /// <param name="directory">Path of the directory to open</param>
        public void ShowOpenDirectoryDialog(DialogCallback resultCallback)
        {
            gameObject.SetActive(true);
            gameObject.GetComponent<FollowMeToggle>().ToggleFollowMeBehavior();
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);
            callback = resultCallback;
            StartCoroutine(ShowLoadDialogCoroutine(FileBrowser.PickMode.Folders));
        }
        private void CloseBrowser(bool cancelled, string selectedPath)
        {
            DialogResult result;
            result.cancelled = cancelled;
            result.path = selectedPath;

            callback?.Invoke(result);
            callback = null;
            gameObject.SetActive(false);
        }
        public void OnCancel()
        {
            CloseBrowser(true, "");
        }
        IEnumerator ShowLoadDialogCoroutine(FileBrowser.PickMode mode)
        {
            yield return FileBrowser.WaitForLoadDialog(mode, false, null, null, "Load Files and Folders", "Load");
            Debug.Log(FileBrowser.Success);
            if (FileBrowser.Success)
            {
                CloseBrowser(!FileBrowser.Success, FileBrowser.Result[0]);
                Debug.Log(FileBrowser.Result[0]);
            }
            else
            {
                OnCancel();
            }
        }
        IEnumerator ShowSaveDialogCoroutine(FileBrowser.PickMode mode)
        {
            yield return FileBrowser.WaitForSaveDialog(mode, false, null, null, "Save Files and Folders", "Save");
            Debug.Log(FileBrowser.Success);
            if (FileBrowser.Success)
            {
                for (int i = 0; i < FileBrowser.Result.Length; i++)
                    Debug.Log(FileBrowser.Result[i]);
                byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(FileBrowser.Result[0]);
                string destinationPath = Path.Combine(Application.persistentDataPath, FileBrowserHelpers.GetFilename(FileBrowser.Result[0]));
                FileBrowserHelpers.CopyFile(FileBrowser.Result[0], destinationPath);
            }
            else
            {
                OnCancel();
            }
        }
    }
}
