# Usagi Merge WebGL MVP

## 최초 설치

1. Unity 6.0.80f1에서 프로젝트를 연다.
2. 스크립트 컴파일이 끝나면 `Tools > Usagi Game > Install or Refresh MVP`를 실행한다.
3. 생성된 `Assets/UsagiGame/Scenes/TitleScene.unity`를 열고 Play로 확인한다.

설치 메뉴는 과일 임포트 설정, 11개 과일 데이터, 게임 규칙, 물리 프리팹, 두 씬, Build Settings와 WebGL 설정을 생성한다. 같은 메뉴를 다시 실행하면 해당 생성물을 최신 기본값으로 갱신한다.

## WebGL 빌드

1. `File > Build Profiles`에서 Web 플랫폼을 선택한다.
2. `Switch Platform` 후 `Build`를 선택한다.
3. 결과 폴더를 Gzip 파일을 제공할 수 있는 HTTP(S) 정적 서버에 업로드한다.
4. Chrome, Edge, Firefox, Safari 및 모바일 브라우저에서 입력, 탭 전환, 새로고침 후 최고 점수를 확인한다.

로컬 빌드 파일을 `file://`로 직접 열면 압축 리소스와 IndexedDB가 정상 동작하지 않을 수 있다. 브라우저 데이터 삭제 또는 시크릿 모드에서는 최고 점수와 설정이 유지되지 않을 수 있다.

## 검수

- `Window > General > Test Runner > EditMode`에서 `UsagiGame.Tests`를 실행한다.
- 9:16, 9:19.5, 9:20 및 넓은 데스크톱 Game View에서 상자 벽과 바닥, HUD가 모두 보이는지 확인한다.
- 같은 단계 3개 동시 접촉, 연쇄 합체, 11단계 두 개 제거, 1초 넘침, UI 위 입력, 멀티터치와 탭 포커스 상실을 수동 확인한다.
