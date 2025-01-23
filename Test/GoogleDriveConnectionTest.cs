using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Threading.Tasks;

public class GoogleDriveConnectionTest : MonoBehaviour
{
    // credential json 파일을 Resources 폴더 등에 넣어두고
    // 아래 필드에 경로를 지정(또는 인스펙터에서 할당)합니다.
    [Header("Google OAuth Credentials JSON (Resources 폴더 경로 등)")]
    [SerializeField] private string _credentialsFilePath = "credentials.json";

    // 트리를 출력하고 싶은 Google Drive 폴더 ID
    [Header("Google Drive Folder ID")]
    [SerializeField] private string _targetFolderId = "YOUR_FOLDER_ID";

    // Google Drive API 서비스 객체
    private DriveService _driveService;

    [ContextMenu("Test Service")]
    private async void Test()
    {
        // 1) 구글 인증 후 DriveService 생성
        _driveService = await AuthenticateGoogleDrive(_credentialsFilePath);

        // 2) 폴더 트리 구조 출력
        Debug.Log($"==== Drive Folder Tree for Folder ID: {_targetFolderId} ====");
        await PrintFolderTree(_targetFolderId, "");
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

    /// <summary>
    /// 특정 폴더 ID 하위의 트리 구조를 재귀적으로 탐색하여 출력하는 메서드
    /// </summary>
    /// <param name="folderId">트리 탐색을 시작할 폴더 ID</param>
    /// <param name="indent">현재 들여쓰기 문자열</param>
    private async Task PrintFolderTree(string folderId, string indent)
    {
        if (_driveService == null)
        {
            Debug.LogError("[GoogleDriveTreeViewer] DriveService is null. Cannot list files.");
            return;
        }

        // 해당 폴더 하위의 파일/폴더 목록 가져오기
        var request = _driveService.Files.List();
        request.Q = $"'{folderId}' in parents and trashed=false";
        request.Fields = "files(id, name, mimeType)";

        var result = await request.ExecuteAsync();

        if (result.Files == null || result.Files.Count == 0)
        {
            Debug.Log($"{indent}- (No files found)");
            return;
        }

        foreach (var file in result.Files)
        {
            bool isFolder = file.MimeType == "application/vnd.google-apps.folder";
            Debug.Log($"{indent}- {file.Name} {(isFolder ? "[Folder]" : "")}");

            // 하위 폴더라면 재귀적으로 탐색
            if (isFolder)
            {
                await PrintFolderTree(file.Id, indent + "    ");
            }
        }
    }
}
