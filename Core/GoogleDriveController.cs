using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Newtonsoft.Json;
using UnityEngine;

namespace UniBridge.Core
{
    public class GoogleDriveController : IDriveFileController
    {
        private readonly string _configFile = "unibridge-config.json";
        private readonly string _targetFolderId = null;
        private readonly string _unityFolderPath = null;

        public GoogleDriveController()
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
            _targetFolderId = config.TargetFolderId;
            _unityFolderPath = config.UnityFolderPath;
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

        public async Task DownloadFileList(List<FileInfo> driveFileList, int maxParallel = 50)
        {
            Debug.Log($"[UniBridge] Downloading files...");

            // Create Directory structure first
            var dirList = driveFileList
                .Where(x => x.IsFolder)
                .OrderBy(x => x.Path);

            foreach (var dir in dirList)
            {
                var absPath = Path.Combine(Application.dataPath, _unityFolderPath, dir.Path);
                DirectoryInfo di = new(absPath);
                di.Create();
                Debug.Log($"[UniBridge] Directory was created! - {absPath}");
            }

            using var semaphore = new SemaphoreSlim(maxParallel);

            var taskList = driveFileList
                .Where(x => !x.IsFolder)
                .Select(async file =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await DownloadFile(file);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

            await Task.WhenAll(taskList);
            Debug.Log($"[UniBridge] Download finished! - {taskList.Count()} files was downloaded.");
        }

        private async Task DownloadFile(FileInfo file)
        {
            var absPath = Path.Combine(Application.dataPath, _unityFolderPath, file.Path);

            Debug.Log($"[UniBridge] Download start: {absPath}");

            var service = await GoogleDriveServiceProvider.Inst.GetDriveService();
            if (service == null)
            {
                Debug.LogError("[UniBridge] DriveService is null. Cannot download file.");
                return;
            }

            // 1. 다운로드 요청
            var request = service.Files.Get(file.Id);
            var downloader = request.MediaDownloader;

            // 2. 메모리 스트림에 데이터를 받는다
            using var memoryStream = new MemoryStream();
            var result = await request.DownloadAsync(memoryStream);

            if (result.Status == DownloadStatus.Completed)
            {
                // 3. 로컬 파일로 저장
                File.WriteAllBytes(absPath, memoryStream.ToArray());
                Debug.Log($"[UniBridge] Download complete: {absPath}");
            }
            else
            {
                // 실패 또는 부분완료
                Debug.LogWarning($"Download ended with status: {result.Status}, Exception: {result.Exception}");
            }
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
        Task DownloadFileList(List<FileInfo> fileList, int maxParallel = 50);
    }
}