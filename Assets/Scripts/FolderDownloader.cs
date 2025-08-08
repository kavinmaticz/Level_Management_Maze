using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Text.RegularExpressions;

public class FolderDownloader : MonoBehaviour
{
    [Tooltip("Paste your full Google Drive link here")]
    public string googleDriveLink = "https://drive.google.com/file/d/1Vcg9DNlAcU2-7LFJUp-p5Fb_ZHU5gomA/view?usp=sharing";

    private string extractPath;

    void Start()
    {
#if UNITY_EDITOR
        extractPath = Path.Combine(Application.dataPath, "Resources/MySprites");
#else
        extractPath = Path.Combine(Application.persistentDataPath, "MySprites");
#endif

        string fileId = ExtractFileId(googleDriveLink);
        if (string.IsNullOrEmpty(fileId))
        {
            Debug.LogError("Could not extract FILE_ID from Google Drive link: " + googleDriveLink);
            return;
        }

        StartCoroutine(DownloadAndExtract(fileId));
    }

    string ExtractFileId(string url)
    {
        // Match /d/<id>/ or id=<id>
        Match match = Regex.Match(url, @"/d/([0-9A-Za-z_-]{20,})");
        if (!match.Success)
        {
            match = Regex.Match(url, @"id=([0-9A-Za-z_-]{20,})");
        }
        return match.Success ? match.Groups[1].Value : null;
    }

    IEnumerator DownloadAndExtract(string fileId)
    {
        string baseUrl = "https://drive.google.com/uc?export=download&id=" + fileId;
        UnityWebRequest www = UnityWebRequest.Get(baseUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Initial request failed: " + www.error);
            yield break;
        }

        byte[] data = www.downloadHandler.data;
        string textCheck = System.Text.Encoding.UTF8.GetString(data);

        // If HTML, handle confirm token for large files
        if (textCheck.StartsWith("<!DOCTYPE html"))
        {
            string token = Regex.Match(textCheck, @"confirm=([0-9A-Za-z_]+)").Groups[1].Value;
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("Google Drive confirm token not found. Check file permissions.");
                yield break;
            }

            string downloadUrl = $"https://drive.google.com/uc?export=download&confirm={token}&id={fileId}";
            www = UnityWebRequest.Get(downloadUrl);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Final download failed: " + www.error);
                yield break;
            }

            data = www.downloadHandler.data;
        }

        // Save and extract
        string zipPath = Path.Combine(Application.temporaryCachePath, "temp.zip");
        File.WriteAllBytes(zipPath, data);
        ClearFolder(extractPath);
        ZipFile.ExtractToDirectory(zipPath, extractPath);

        Debug.Log("Folder extracted to: " + extractPath);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    void ClearFolder(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        foreach (string file in Directory.GetFiles(path))
            File.Delete(file);

        foreach (string dir in Directory.GetDirectories(path))
            Directory.Delete(dir, true);
    }
}
