using UnityEngine;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Threading.Tasks;
using Google.Apis.Download;

namespace UniBridge.Test
{
    public class GoogleDriveDownloadTest : MonoBehaviour
    {
        // credential json 파일을 Resources 폴더 등에 넣어두고
        // 아래 필드에 경로를 지정(또는 인스펙터에서 할당)합니다.
        [Header("Google OAuth Credentials JSON (Resources 폴더 경로 등)")]
        [SerializeField] private string _credentialsFilePath = "credentials.json";

        // 타겟 Google Drive 폴더 ID
        [Header("Google Drive File ID")]
        [SerializeField] private string _driveFileId = "YOUR_FOLDER_ID";

        [Header("Unity Drive Folder Path")]
        [SerializeField] private string _unityFolderPath = "YOUR_FOLDER_ID";

        // Google Drive API 서비스 객체
        private DriveService _driveService;

        [ContextMenu("Test Service")]
        private async void Test()
        {
            // 1) 구글 인증 후 DriveService 생성
            _driveService = await AuthenticateGoogleDrive(_credentialsFilePath);

            // 2) 폴더 트리 구조 출력
            Debug.Log($"==== File ID: {_driveFileId} ====");
            await DownloadFile(_driveFileId, "test.png", _unityFolderPath);
        }

        /// <summary>
        /// 구글 인증을 수행하고 DriveService 객체를 반환합니다.
        /// </summary>
        /// <param name="credentialsPath">OAuth 인증 JSON 파일 경로</param>
        /// <returns>인증된 DriveService</returns>
        private async Task<DriveService> AuthenticateGoogleDrive(string credentialsPath)
        {
            // JSON credential 파일 로드
            string fullPath = Path.Combine(Application.dataPath, credentialsPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[GoogleDriveTreeViewer] Credential file not found: {fullPath}");
                return null;
            }

            UserCredential credential;
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                // 사용자 인증 정보 생성
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { DriveService.Scope.DriveReadonly }, // 읽기 전용
                    "user",
                    CancellationToken.None,
                    new FileDataStore("TokenStore", true)
                );
            }

            // Drive API 서비스 생성
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Unity Google Drive Tree Viewer",
            });

            return service;
        }

        private async Task DownloadFile(string fileId, string fileName, string localPath)
        {
            if (_driveService == null)
            {
                Debug.LogError("[GoogleDriveFileDownloader] DriveService is null. Cannot download file.");
                return;
            }

            // 1. 다운로드 요청
            var request = _driveService.Files.Get(fileId);
            var downloader = request.MediaDownloader;

            var absPath = Application.dataPath + "/" + localPath + "/" + fileName;

            // (선택) 다운로드 진행 상황 모니터링
            downloader.ProgressChanged += Download_ProgressChanged;

            // 2. 메모리 스트림에 데이터를 받는다
            using var memoryStream = new MemoryStream();
            var result = await request.DownloadAsync(memoryStream);

            if (result.Status == DownloadStatus.Completed)
            {
                // 3. 로컬 파일로 저장
                File.WriteAllBytes(absPath, memoryStream.ToArray());
                Debug.Log($"[GoogleDriveFileDownloader] Download complete: {localPath}");
            }
            else
            {
                // 실패 또는 부분완료
                Debug.LogWarning($"Download ended with status: {result.Status}, Exception: {result.Exception}");
            }
        }

        // 다운로드 진행 상황 체크 (선택)
        private void Download_ProgressChanged(IDownloadProgress progress)
        {
            switch (progress.Status)
            {
                case DownloadStatus.Downloading:
                    Debug.Log($"Downloading... {progress.BytesDownloaded} bytes");
                    break;
                case DownloadStatus.Completed:
                    Debug.Log("Download completed.");
                    break;
                case DownloadStatus.Failed:
                    Debug.LogError($"Download failed. {progress.Exception}");
                    break;
            }
        }
    }
}