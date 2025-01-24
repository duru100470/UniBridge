using System.Collections;
using System.Collections.Generic;
using UniBridge.Core;
using UnityEngine;

namespace UniBridge.Test
{
    public class CoreTest : MonoBehaviour
    {
        [ContextMenu("Test loading drive folder")]
        private async void Test()
        {
            IDriveFileController controller = new GoogleDriveController();
            var ret = await controller.GetFileListAsync();

            foreach (var file in ret)
            {
                Debug.Log(file.ToString());
            }
        }

        [ContextMenu("Test checking unity asset")]
        private async void Test2()
        {
            IDriveFileController controller = new GoogleDriveController();
            var driveFiles = await controller.GetFileListAsync();

            UnityAssetChecker checker = new UnityAssetChecker();
            var ret = checker.CheckMissingAssetFiles(driveFiles);

            foreach (var file in ret)
            {
                Debug.Log(file.ToString());
            }
        }

        [ContextMenu("Test downloading missing unity asset")]
        private async void Test3()
        {
            IDriveFileController controller = new GoogleDriveController();
            var driveFiles = await controller.GetFileListAsync();

            UnityAssetChecker checker = new UnityAssetChecker();
            var ret = checker.CheckMissingAssetFiles(driveFiles);

            foreach (var file in ret)
            {
                Debug.Log(file.ToString());
            }

            await controller.DownloadFileList(ret);
        }
    }
}
