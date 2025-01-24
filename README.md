# UniBridge
## 사용법
1. Google Cloud에서 OAuth credential 파일 생성 및 다운
2. Assets 폴더에 credentials.json의 이름으로 넣기
3. Assets 폴더에 unibridge-config.json 만들기
```json
{
  "TargetFolderId": "타겟 구글 폴더 Id",
  "UnityFolderPath": "동기화 할 Unity 폴더 경로(Assets 폴더를 루트로 함. ex) Sprites/New)"
}
```
4. Tools/UniBridge에서 Start 버튼 누르면 끝

## 설치법
1. Package Manager/Add package from git URL.. -> https://github.com/duru100470/UniBridge.git
2. CLI 열고 ```openupm add com.duru100470.unibridge``` 입력
