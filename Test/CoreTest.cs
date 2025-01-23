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
    }
}
