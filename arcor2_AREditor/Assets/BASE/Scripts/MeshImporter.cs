using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using Base;
using TriLibCore;
using UnityEngine;
using UnityEngine.Networking;

public class MeshImporter : Singleton<MeshImporter> {

    public delegate void ImportedMeshEventHandler(object sender, ImportedMeshEventArgs args);
    public event ImportedMeshEventHandler OnMeshImported;

    /// <summary>
    /// Dictionary of all urdf robot source file names (e.g. dobot-magician.zip) and bool value indicating, whether download of these source files is in progress.
    /// (fileName, downloadInProgress)
    /// </summary>
    private Dictionary<string, bool> meshSources = new Dictionary<string, bool>();

    /// <summary>
    /// Content: MeshID, action object ID
    /// After downloading a mesh file all meshes stored here are imported
    /// </summary>
    private Dictionary<string, string> meshesToImport = new Dictionary<string, string>();

    /// <summary>
    /// Loads mesh - takes care of downloading, updating and importing mesh
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="aoId">ID of action object which is asociated with the mesh</param>
    public void LoadModel(IO.Swagger.Model.Mesh mesh, string aoId) {
        if (CheckIfNewerRobotModelExists(mesh.Id, mesh.Uri)) {
            meshesToImport.Add(aoId, mesh.Id);
            StartCoroutine(DownloadMesh(mesh.Id, mesh.Uri, aoId)); //downloads + imports mesh
        } else {
            if (CanReadMeshFile(mesh.Id)) {
                ImportMesh(GetPathToMesh(mesh.Id), aoId);
            } else { // mesh is just being downloaded
                meshesToImport.Add(aoId, mesh.Id);
            }
        }
    }

    /// <summary>
    /// Imports model of type DAE, FBX, OBJ, GLTF2, STL, PLY, 3MF into placeholder object, which is returned immediately.
    /// After the model itself is imported, the OnMeshImported action is triggered.
    /// </summary>
    /// <param name="path">Full path of the model to be imported.</param>
    /// <param name="aoId">ID of action object which is asociated with mesh</param>
    /// <returns></returns>
    private void ImportMesh(string path, string aoId) {
        Debug.LogError("importing mesh path: " + path);


        if (Path.GetExtension(path).ToLower() == ".dae") {
            StreamReader reader = File.OpenText(path);
            string daeFile = reader.ReadToEnd();
            GameObject loadedObject = new GameObject("ImportedMesh");

            // Requires Simple Collada asset from Unity Asset Store: https://assetstore.unity.com/packages/tools/input-management/simple-collada-19579
            // Supports: DAE
            //var go = ColladaImporter.Import(daeFile);
            //OnMeshImported?.Invoke(this, new ImportedMeshEventArgs(go, aoId));
            StartCoroutine(ColladaImporter.Instance.ImportAsync(daeFile, Quaternion.identity, Vector3.one, Vector3.zero,
                onMImported: delegate (GameObject loadedGameObject) {
                    OnMeshImported?.Invoke(this, new ImportedMeshEventArgs(loadedGameObject, aoId));
                },
                includeEmptyNodes: true, wrapperGameObject: loadedObject));

        } else {
            // Requires Trilib 2 asset from Unity Asset Store: https://assetstore.unity.com/packages/tools/modeling/trilib-2-model-loading-package-157548
            // Supports: FBX, OBJ, GLTF2, STL, PLY, 3MF
            AssetLoaderOptions assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
            AssetLoader.LoadModelFromFile(path, null, delegate (AssetLoaderContext assetLoaderContext) {
                OnMeshImported?.Invoke(this, new ImportedMeshEventArgs(assetLoaderContext.RootGameObject, aoId));
            }, null, assetLoaderOptions: assetLoaderOptions, onError: OnModelLoadError);
        }
    }

    /// <summary>
    /// Downloads mesh and saves it into persistent storage
    /// </summary> 
    /// <param name="meshId"></param>
    /// <param name="uri">Where should be the mesh download from.</param>
    /// <param name="aoId">ID of action object which is asociated with mesh</param>
    /// <returns></returns>
    private IEnumerator DownloadMesh(string meshId, string uri, string aoId) {

        Debug.LogError("MESH: download started");
        using (UnityWebRequest www = UnityWebRequest.Get(uri)) {
            // Request and wait for the desired page.
            yield return www.Send();
            if (www.isNetworkError || www.isHttpError) {
                Debug.LogError(www.error + " (" + uri + ")");
                Notifications.Instance.ShowNotification("Failed to download mesh", www.error);
            } else {
                string meshDirectory = string.Format("{0}/meshes/{1}/", Application.persistentDataPath, meshId);
                Directory.CreateDirectory(meshDirectory);
                string savePath = string.Format("{0}/{1}", meshDirectory, meshId);
                System.IO.File.WriteAllBytes(savePath, www.downloadHandler.data);

                //if the mesh is zipped, extract it
                if (Path.GetExtension(savePath).ToLower() == ".zip") {
                    string meshUnzipDirectory = string.Format("{0}/{1}", meshDirectory, "mesh");
                    try {
                        Directory.Delete(meshUnzipDirectory, true);
                    } catch (DirectoryNotFoundException) {
                        // ok, nothing to delete..
                    }
                    try {
                        ZipFile.ExtractToDirectory(savePath, meshUnzipDirectory);
                        OnMeshDownloaded(meshId);
                    } catch (Exception ex) when (ex is ArgumentException ||
                                                    ex is ArgumentNullException ||
                                                    ex is DirectoryNotFoundException ||
                                                    ex is PathTooLongException ||
                                                    ex is IOException ||
                                                    ex is FileNotFoundException ||
                                                    ex is InvalidDataException ||
                                                    ex is UnauthorizedAccessException) {
                        Debug.LogError(ex);
                        Notifications.Instance.ShowNotification("Failed to extract mesh", "");
                    }
                } else { //not *.zip
                    OnMeshDownloaded(meshId);
                }
            }
        }


    }

    /// <summary>
    /// imports mesh for all action objects, which need it
    /// Uses meshesToImport dictionary
    /// </summary>
    /// <param name="meshID"></param>
    private void OnMeshDownloaded(string meshID) {
        meshSources[meshID] = false; //not downloading anymore
        List<string> toRemove = new List<string>();

        foreach (KeyValuePair<string, string> toImport in meshesToImport) {
            if (toImport.Value != meshID)
                continue;

            toRemove.Add(toImport.Key);
            string path = GetPathToMesh(meshID);
            if (Path.GetExtension(path).ToLower() == ".zip") {
                try {
                    ImportMesh(GetPathToMesh(meshID), toImport.Key);
                } catch (FileNotFoundException ex) {
                    OnModelLoadError(ex.Message);
                }
            } else {
                ImportMesh(path, toImport.Key);
            }
        }

        foreach (var aoid in toRemove) {
            meshesToImport.Remove(aoid);
        }
    }

    private string GetPathToMesh(string meshId) {
        if (Path.GetExtension(meshId).ToLower() == ".zip") {
            string path = string.Format("{0}/meshes/{1}/mesh/", Application.persistentDataPath, meshId);
            string[] extensions = { "fbx", "dae", "obj", "gltf2", "stl", "ply", "3mf" };
            string[] files = { };
            foreach (var extension in extensions) {
                files = System.IO.Directory.GetFiles(path, "*." + extension);
                if (files.Length > 0)
                    return files[0];
            }
            throw new FileNotFoundException("Not found mesh file with a known extension.");
        } else { //not .zip
            return string.Format("{0}/meshes/{1}/{1}", Application.persistentDataPath, meshId);
        }
    }

    private void OnModelLoadError(IContextualizedError obj) {
        Notifications.Instance.ShowNotification("Unable to show mesh ", obj.GetInnerException().Message);
    }

    private void OnModelLoadError(string message) {
        Notifications.Instance.ShowNotification("Unable to show mesh ", message);
    }

    /// <summary>
    /// Checks that newer version of mesh exists on the server.
    /// Returns true if so or if there is no downloaded zip file with the robot model at all,
    /// false if downloaded zip file is already at its newest version. 
    /// </summary>
    /// <param name="meshId"></param>
    /// <param name="uri">Where the mesh should be downloaded from</param>
    /// <returns></returns>
    public bool CheckIfNewerRobotModelExists(string meshId, string uri) {
        Debug.LogError("mesh: Checking if newer  mesh exists " + meshId);
        if (string.IsNullOrEmpty(uri)) {
            return false;
        }

        FileInfo meshFileInfo = new FileInfo(Application.persistentDataPath + "/meshes/" + meshId + "/" + meshId);
        if (!meshFileInfo.Exists) {
            Debug.LogError("mesh: mesh file " + meshId + " has to be downloaded.");
            // Check whether downloading can be started and start it, if so.
            return CanIDownload(meshId);
        }

        DateTime downloadedZipLastModified = meshFileInfo.LastWriteTime;

        HttpWebRequest httpWebRequest = (HttpWebRequest) WebRequest.Create(uri);
        HttpWebResponse httpWebResponse = (HttpWebResponse) httpWebRequest.GetResponse();

        // t1 is earlier than t2 --> newer version of urdf is present on the server
        //Debug.LogError("zip: " + downloadedZipLastModified + " http lastmodified: " + httpWebResponse.LastModified);
        if (DateTime.Compare(downloadedZipLastModified, httpWebResponse.LastModified) < 0) {
            Debug.LogError("mesh: Newer version is present on the server.");
            httpWebResponse.Close();
            // Check whether downloading can be started and start it, if so.
            return CanIDownload(meshId);
        } else {
            // There is no need to download anything, lets return false
            Debug.LogError("mesh: Downloaded version is already the latest one.");
            httpWebResponse.Close();
            return false;
        }
    }

    /// <summary>
    /// Checks whether someone else is not downloading specified mesh in given meshId. If not, return true and start downloading.
    /// </summary>
    /// <param name="meshId"></param>
    /// <returns></returns>
    private bool CanIDownload(string meshId) {
        // If RobotModelsSources has entry, lets check if download of the urdf is in progress
        if (meshSources.TryGetValue(meshId, out bool downloadInProgress)) {
            if (downloadInProgress) {
                // download is in progress, return false so the urdf file won't download again
                return false;
            } else {
                // return true and start downloading
                meshSources[meshId] = true;
                return true;
            }
        } else {
            // Create the entry in RoboModelsSources and set downloadProgress to true and start downloading
            meshSources.Add(meshId, true);
            return true;
        }
    }

    private bool CanReadMeshFile(string meshId) {
        if (meshSources.TryGetValue(meshId, out bool downloadInProgress)) {
            return !downloadInProgress;
        } else {
            return true;
        }
    }
}


/// <summary>
/// Used when model is imported.
/// </summary>
public class ImportedMeshEventArgs : EventArgs {
    /// <summary>
    /// Imported GameObject.
    /// </summary>
    public GameObject RootGameObject {
        get; set;
    }

    public string aoId {
        get;set;
    }

    public ImportedMeshEventArgs(GameObject gameObject, string name) {
        RootGameObject = gameObject;
        aoId = name;
    }
}

