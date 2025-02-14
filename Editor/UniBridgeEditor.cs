using System.Collections.Generic;
using UniBridge.Core;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UniBridge.Editor
{
    public class UniBridgeEditor : EditorWindow
    {
        [SerializeField] private VisualTreeAsset _visualTreeAsset;

        private TreeView _treeView;
        private Button _startBtn;
        private ProgressBar _progress;
        private Toggle _overwrite;
        private VisualElement _mainTabContent;
        private VisualElement _configTabContent;
        private ToolbarButton _tabMainBtn;
        private ToolbarButton _tabConfigBtn;

        private Button _saveBtn;

        private readonly string _configFile = "config.json";

        [MenuItem("Tools/UniBridge")]
        public static void ShowWindow()
        {
            // Create and show the window
            var window = GetWindow<UniBridgeEditor>();
            window.titleContent = new GUIContent("UniBridge");
            window.minSize = new Vector2(400, 300);
        }

        public void CreateGUI()
        {
            // Load and clone the UXML file
            var root = rootVisualElement;
            root.Add(_visualTreeAsset.Instantiate());

            _mainTabContent = root.Q<VisualElement>("main-tab-content");
            _configTabContent = root.Q<VisualElement>("config-tab-content");

            _tabMainBtn = root.Q<ToolbarButton>("tab-main");
            _tabConfigBtn = root.Q<ToolbarButton>("tab-config");

            // 버튼 이벤트 등록
            _tabMainBtn.clicked += () => ShowTab(true);
            _tabConfigBtn.clicked += () => ShowTab(false);

            // 초기 상태: Main 탭 보이기
            ShowTab(true);

            // Optional: Add additional C# controls or callbacks here
            SetupCallbacks();
        }

        private void SetupCallbacks()
        {
            _treeView = rootVisualElement.Q<TreeView>("google-tree");
            _startBtn = rootVisualElement.Q<Button>("start-btn");
            _progress = rootVisualElement.Q<ProgressBar>("progress-bar");
            _overwrite = rootVisualElement.Q<Toggle>("overwrite-toggle");

            _startBtn.clicked += () =>
            {
                SyncUnityAssetFiles();
            };

            _saveBtn = rootVisualElement.Q<Button>("config-save-btn");

            _saveBtn.clicked += () =>
            {
                SaveConfig();
            };
        }

        private void ShowTab(bool showMain)
        {
            if (showMain)
            {
                _mainTabContent.style.display = DisplayStyle.Flex;
                _configTabContent.style.display = DisplayStyle.None;
            }
            else
            {
                _mainTabContent.style.display = DisplayStyle.None;
                _configTabContent.style.display = DisplayStyle.Flex;
                LoadConfig();
            }
        }

        private void SaveConfig()
        {
            // UXML의 TextField 가져오기 (name="target-folder-id", "unity-folder-path")
            TextField targetFolderField = rootVisualElement.Q<TextField>("target-folder-id");
            TextField unityFolderField = rootVisualElement.Q<TextField>("unity-folder-path");

            if (targetFolderField == null)
                return;
            if (unityFolderField == null)
                return;

            string fullPath = GetConfigPath();

            Config conf = new();
            conf.TargetFolderId = targetFolderField.value;
            conf.UnityFolderPath = unityFolderField.value;

            var str = JsonUtility.ToJson(conf);
            File.WriteAllText(fullPath, str);

            Debug.Log($"Config file was saved: {fullPath}");
        }

        private void LoadConfig()
        {
            string fullPath = GetConfigPath();

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"Config file not found: {fullPath}");
                return;
            }

            // 4. 파일 읽기
            string jsonContent = File.ReadAllText(fullPath);

            // JSON -> C# 객체(UniBridgeConfig) 변환
            Config config = JsonUtility.FromJson<Config>(jsonContent);
            if (config == null)
            {
                Debug.LogError("config.json 파싱에 실패했습니다. JSON 구조를 확인하세요.");
                return;
            }

            // UXML의 TextField 가져오기 (name="target-folder-id", "unity-folder-path")
            TextField targetFolderField = rootVisualElement.Q<TextField>("target-folder-id");
            TextField unityFolderField = rootVisualElement.Q<TextField>("unity-folder-path");

            if (targetFolderField != null)
            {
                targetFolderField.value = config.TargetFolderId;
            }

            if (unityFolderField != null)
            {
                unityFolderField.value = config.UnityFolderPath;
            }

            Debug.Log("Config Loaded: " + jsonContent);
        }

        private string GetConfigPath()
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string editorFolderPath = Path.GetDirectoryName(scriptPath);
            string packageFolderPath = Directory.GetParent(editorFolderPath).FullName;
            string fullPath = Path.Combine(packageFolderPath, _configFile);

            return fullPath;
        }

        private async void SyncUnityAssetFiles()
        {
            _progress.value = 0;

            IDriveFileController controller = new GoogleDriveController(GetConfigPath());
            var driveFiles = await controller.GetFileListAsync();

            _progress.value = 50;

            if (_treeView == null)
            {
                Debug.LogError("TreeView 요소가 UXML 안에 없습니다. name을 확인하세요.");
                return;
            }

            var treeRoot = FileTreeBuilder.BuildTreeViewData(driveFiles);
            _treeView.SetRootItems(treeRoot);
            _treeView.makeItem = () => new Label();
            _treeView.bindItem = (VisualElement element, int index) =>
                (element as Label).text = _treeView.GetItemDataForIndex<string>(index);

            if (!_overwrite.value)
            {
                UnityAssetChecker checker = new UnityAssetChecker();
                var ret = checker.CheckMissingAssetFiles(driveFiles);
                await controller.DownloadFileList(ret);
            }
            else
            {
                await controller.DownloadFileList(driveFiles);
            }

            _progress.value = 100;

            AssetDatabase.Refresh();
        }
    }
}