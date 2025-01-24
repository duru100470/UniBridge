using System.Collections;
using System.Collections.Generic;
using UniBridge.Core;
using UnityEngine;

namespace UniBridge.Test
{
    public class UnityAssetCheckerTest : MonoBehaviour
    {
        [ContextMenu("Test loading drive folder")]
        private void Test()
        {
            UnityAssetChecker checker = new UnityAssetChecker();
            var ret = checker.GetUnityAssetFiles();

            foreach (var file in ret)
            {
                Debug.Log(file.ToString());
            }
        }
    }
}