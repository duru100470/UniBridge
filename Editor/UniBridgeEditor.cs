using System.Collections.Generic;
using UniBridge.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniBridge.Editor
{
    public class UniBridgeEditor : EditorWindow
    {
        [SerializeField] private VisualTreeAsset _visualTreeAsset;

        private TreeView _treeView;
        private Button _startBtn;
        private ProgressBar _progress;

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

            // Optional: Add additional C# controls or callbacks here
            SetupCallbacks();
        }

        private void SetupCallbacks()
        {
            _treeView = rootVisualElement.Q<TreeView>("google-tree");
            _startBtn = rootVisualElement.Q<Button>("start-btn");
            _progress = rootVisualElement.Q<ProgressBar>("progress-bar");

            _startBtn.clicked += () =>
            {
                SyncUnityAssetFiles();
            };
        }

        private async void SyncUnityAssetFiles()
        {
            _progress.value = 0;

            IDriveFileController controller = new GoogleDriveController();
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

            UnityAssetChecker checker = new UnityAssetChecker();
            var ret = checker.CheckMissingAssetFiles(driveFiles);

            await controller.DownloadFileList(ret);

            _progress.value = 100;
        }
    }
}