using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using Newtonsoft.Json;
using UnityEngine;

namespace UniBridge.Core
{
    public class GoogleDriveController : IDriveFileController
    {
        private readonly string _configFile = "unibridge-config.json";
        private readonly string _targetFolderId = null;

        public GoogleDriveController()
        {
            // JSON credential 파일 로드
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
            _targetFolderId = config.TargetFolderId;
        }

        public async Task<List<FileInfo>> GetFileListAsync()
        {
            if (_targetFolderId == null)
            {
                Debug.LogError("[UniBridge] Target folder id is invalid!");
                return null;
            }

            Debug.Log("[UniBridge] Searching google drive folder..");
            var service = await GoogleDriveServiceProvider.Inst.GetDriveService();

            return await RetrieveDriveFolder(_targetFolderId, "", service);
        }

        private async Task<List<FileInfo>> RetrieveDriveFolder(string folderId, string parent, DriveService service)
        {
            var ret = new List<FileInfo>();

            if (service == null)
            {
                Debug.LogError("[UniBridge] DriveService is null. Cannot list files.");
                return ret;
            }

            // 해당 폴더 하위의 파일/폴더 목록 가져오기
            var request = service.Files.List();
            request.Q = $"'{folderId}' in parents and trashed=false";
            request.Fields = "files(id, name, mimeType)";

            var result = await request.ExecuteAsync();

            if (result.Files == null || result.Files.Count == 0)
            {
                return ret;
            }

            foreach (var file in result.Files)
            {
                bool isFolder = file.MimeType == "application/vnd.google-apps.folder";
                ret.Add(new FileInfo()
                {
                    Id = file.Id,
                    Name = file.Name,
                    Path = Path.Combine(parent, file.Name),
                    MimeType = file.MimeType,
                });

                // 하위 폴더라면 재귀적으로 탐색
                if (isFolder)
                {
                    var child = await RetrieveDriveFolder(file.Id, Path.Combine(parent, file.Name), service);
                    ret.AddRange(child);
                }
            }

            return ret;
        }
    }

    public interface IDriveFileController
    {
        Task<List<FileInfo>> GetFileListAsync();
    }
}