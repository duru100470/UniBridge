using System.Collections.Generic;
using UniBridge.Core;
using UnityEngine.UIElements;

namespace UniBridge.Editor
{
    public static class FileTreeBuilder
    {
        private static int _idCounter = 0;

        /// <summary>
        /// 파일 목록을 트리 구조로 빌드해서 최상위 루트 노드들을 반환한다.
        /// </summary>
        public static List<TreeViewItemData<string>> BuildTreeViewData(List<FileInfo> fileInfos)
        {
            // 경로 -> TreeViewItemData<string> 매핑
            var nodeLookup = new Dictionary<string, TreeViewItemData<string>>();

            // 경로 -> 상위 경로 기록
            // (최상위 노드는 parentPath가 null)
            var parentLookup = new Dictionary<string, string>();

            // 루프 돌며 경로 파츠를 하나씩 트리에 삽입
            foreach (var fileInfo in fileInfos)
            {
                string path = fileInfo.Path.Replace('\\', '/');
                var parts = path.Split('/');

                // 누적 경로 관리
                string parentPath = null;
                string currentPath = null;

                for (int i = 0; i < parts.Length; i++)
                {
                    // 누적 경로 계산
                    // - 첫 파츠일 때는 그냥 parts[i]
                    // - 이후 파츠는 parentPath + "/" + parts[i]
                    if (i == 0)
                    {
                        currentPath = parts[i];
                    }
                    else
                    {
                        currentPath = parentPath + "/" + parts[i];
                    }

                    // 이미 노드가 없다면 새로 생성
                    if (!nodeLookup.TryGetValue(currentPath, out var currentNode))
                    {
                        currentNode = new TreeViewItemData<string>(
                            id: _idCounter++,
                            data: parts[i], // 보여줄 텍스트 (폴더/파일명)
                            children: new List<TreeViewItemData<string>>() // 빈 리스트 (추가 예정)
                        );

                        nodeLookup[currentPath] = currentNode;
                        parentLookup[currentPath] = parentPath; // parentPath가 null이면 루트
                    }

                    // 부모 노드가 있다면, 그 부모의 children에 현재 노드를 추가
                    if (parentPath != null)
                    {
                        var parentNode = nodeLookup[parentPath];
                        // TreeViewItemData<T>.children는 IReadOnlyList이지만
                        // 생성자에 넘겨준 List를 캐스팅해 리스트를 직접 추가할 수 있음
                        var parentChildren = (List<TreeViewItemData<string>>)parentNode.children;

                        // 혹시 중복 추가되지 않도록 확인
                        if (!parentChildren.Contains(currentNode))
                            parentChildren.Add(currentNode);
                    }

                    // 탐색을 한 단계 내려감
                    parentPath = currentPath;
                }
            }

            // 이제 parentPath가 null인 노드(즉, parentLookup[currentPath] == null)를
            // 최상위 노드로 모아서 반환
            var rootNodes = new List<TreeViewItemData<string>>();

            foreach (var kvp in nodeLookup)
            {
                string pathKey = kvp.Key;
                string parentKey = parentLookup[pathKey];

                if (parentKey == null) // = 루트 노드
                {
                    rootNodes.Add(kvp.Value);
                }
            }

            return rootNodes;
        }
    }
}