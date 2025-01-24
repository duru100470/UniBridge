using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using UnityEngine;

namespace UniBridge.Core
{
    public class GoogleDriveServiceProvider
    {
        private static GoogleDriveServiceProvider s_inst = null;
        public static GoogleDriveServiceProvider Inst
        {
            get
            {
                s_inst ??= new GoogleDriveServiceProvider();
                return s_inst;
            }
        }

        private string _credentialsPath = "credentials.json";

        private DriveService _driveService = null;

        public async Task<DriveService> GetDriveService()
        {
            if (_driveService != null)
                return _driveService;

            return await AuthenticateGoogleDrive(_credentialsPath);
        }

        private async Task<DriveService> AuthenticateGoogleDrive(string credentialsPath)
        {
            // JSON credential 파일 로드
            string fullPath = Path.Combine(Application.dataPath, credentialsPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[UniBridge] Config file not found: {fullPath}");
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
    }
}