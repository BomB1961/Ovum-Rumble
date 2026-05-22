# Ovum Rumble

## Map Select Flow 작업

이번 작업은 `00_MainMenu`에서 맵 선택 화면으로 이동하고, 선택한 맵으로 `01_Game`을 시작하는 흐름을 추가한 작업입니다.

기존에는 게임 시작 흐름에서 맵 선택 단계가 없었지만, 이번 작업으로 Terrian / Ice / Desert 중 하나를 선택한 뒤 해당 맵으로 게임을 시작할 수 있도록 구성했습니다.

## 브랜치

`feature/C-play-with-ai`

## 변경 파일

- `Assets/Scenes/00_MainMenu.unity`
- `Assets/_Project/Scripts/Presentation/MainMenuController.cs`

## 주요 변경 내용

- `MainMenuController`에 AI 버튼 전용 클릭 메서드 추가
- AI 버튼 클릭 시 `GameMode.VsComputer` 저장
- 이후 `02_MapSelect` 씬으로 이동
- 기존 `Button_CreateRoom` 흐름 유지
- 기존 `OnClickVsComputer()` 흐름 유지


---

## 작업 내용

* `02_MapSelect` 씬 추가
* Terrian / Ice / Desert 맵 선택 버튼 추가
* 맵 선택 후 `01_Game` 로드 흐름 구현
* 선택한 맵 정보를 런타임에서 저장하도록 구성
* `MapId` 추가
* `GameLaunchContext` 추가
* `MapSelectController` 추가
* `ProceduralBoardGenerator` 런타임 생성 방식 유지
* Ice / Desert 원본 프리팹은 직접 수정하지 않고 Wrapper 프리팹 추가
* Ice / Desert Wrapper 안에 원본 프리팹 인스턴스를 넣고 Transform 보정

---
