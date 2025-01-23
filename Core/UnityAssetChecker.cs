using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace UniBridge.Core
{
    public class UnityAssetChecker
    {
        private readonly string _configFile = "unibridge-config.json";
        private readonly string _unityFolderPath = null;

        public UnityAssetChecker()
        {
            string fullPath = Path.Combine(Application.dataPath, _configFile);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[GoogleDriveTreeViewer] Config file not found: {fullPath}");
                return;
            }

            StreamReader reader = new StreamReader(fullPath);
            var json = reader.ReadToEnd();
            reader.Close();

            var config = JsonConvert.DeserializeObject<Config>(json);
            _unityFolderPath = config.UnityFolderPath;
        }

        public List<FileInfo> GetUnityAssetFiles()
        {
            Debug.Log($"[UniBridge] Finding assets at {_unityFolderPath}");
            var ret = GetUnityAssetFilesRecurse();
            return ret;
        }

        public List<FileInfo> GetUnityAssetFilesRecurse(string parent = "")
        {
            List<FileInfo> ret = new List<FileInfo>();

            var absPath = Path.Combine(Application.dataPath, _unityFolderPath);
            var fullPath = Path.Combine(absPath, parent);

            string[] arrFiles = Directory.GetFiles(fullPath);
            foreach (string file in arrFiles)
            {
                if (Path.GetExtension(file) == ".meta")
                    continue;

                var fileName = Path.GetFileName(file);

                ret.Add(new FileInfo()
                {
                    Name = fileName,
                    MimeType = "application/vnd.google-apps.file",
                    Path = Path.Combine(parent, fileName),
                });
            }

            string[] arrDirectories = Directory.GetDirectories(fullPath);
            foreach (string directory in arrDirectories)
            {
                var folderName = Path.GetFileName(directory);

                ret.Add(new FileInfo()
                {
                    Name = folderName,
                    MimeType = "application/vnd.google-apps.folder",
                    Path = Path.Combine(parent, folderName),
                });

                ret.AddRange(GetUnityAssetFilesRecurse(Path.Combine(parent, folderName)));
            }

            return ret;
        }
    }
}