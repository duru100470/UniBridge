using UniBridge.Core;
using UnityEngine;

namespace UniBridge.Test
{
    public class FileInfoTest : MonoBehaviour
    {
        [ContextMenu("Test 1")]
        private void Test()
        {
            var file1 = new FileInfo()
            {
                Id = "1234",
                Name = "test",
                MimeType = "123123",
                Path = "test123/test",
            };

            var file2 = new FileInfo()
            {
                Id = "1232",
                Name = "test",
                MimeType = "121123",
                Path = "test123/test",
            };

            Debug.Log(FileInfo.Compare(file1, file2));
            Debug.Log(FileInfo.Compare(file1, file2, isExact: true));
        }
    }
}