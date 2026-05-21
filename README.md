# Ovum Rumble

## C UI 기능 구현

`00_MainMenu`와 `01_Game` 씬에 C 담당 UI 흐름을 추가했습니다.

이번 작업은 UI 기능 연결과 테스트 가능한 화면 구성을 목표로 했습니다. 별도의 최종 디자인, 고급 UI 애니메이션, 세부 비주얼 polish는 추가하지 않았습니다.

origin/feature/C-ui-flow
= UI 구조/뼈대만 잡은 브랜치

add-ui-features
= C-ui-flow 뼈대 + 실제 기능 추가한 브랜치

add-ui-features가 C-ui-flow 내용을 포함하고 있습니다.

## 클래스명 - 기능 내용

| 클래스명 | 기능 내용 |
|---|---|
| `MainMenuController` | `00_MainMenu` 씬의 메인 메뉴 UI를 제어합니다. 방 만들기, 방 참가, 테스트 참가, 설정 열기/닫기, 게임 종료 버튼을 처리합니다. |
| `GameUIController` | `01_Game` 씬의 HUD, 결과 화면, 설정 화면 UI를 제어합니다. 현재 턴, P1/P2 알 개수, 남은 턴 시간, 전체 게임 시간을 표시합니다. |
| `GameSessionUiBridge` | UI 테스트용 게임 시간 흐름을 관리합니다. 턴 시간과 전체 게임 시간을 계산해 HUD에 전달하고, 전체 시간이 끝나면 가이드 문구를 변경합니다. |
| `HudPresenter` | 게임 진행 정보를 HUD에 표시하기 위해 `GameUIController`로 전달합니다. |
| `ResultScreen` | 승리 또는 무승부 결과 정보를 결과 화면에 표시하기 위해 `GameUIController`로 전달합니다. |

## 파일 정리

```text
Assets/Scenes/00_MainMenu.unity
Assets/Scenes/01_Game.unity
Assets/_Project/Scripts/Presentation/GameSessionUiBridge.cs
Assets/_Project/Scripts/Presentation/GameUIController.cs
Assets/_Project/Scripts/Presentation/HudPresenter.cs
Assets/_Project/Scripts/Presentation/MainMenuController.cs
Assets/_Project/Scripts/Presentation/ResultScreen.cs
```

## 현재 구현된 UI

### 00_MainMenu

- 메인 메뉴 화면
- 방 만들기 버튼
- 방 참가 패널 열기
- 테스트 참가로 `01_Game` 씬 이동
- 설정 패널 열기/닫기
- BGM/SFX 슬라이더 UI
- 게임 종료 버튼

### 01_Game

- HUD 패널
- 현재 턴 표시
- P1/P2 남은 알 개수 표시
- 남은 턴 시간 표시
- 전체 게임 시간 표시
- 결과 패널
- 재시작 버튼
- 메인 메뉴 이동 버튼
- 게임 종료 버튼
- 설정 패널 게임 종료 버튼

## 연동 대기 항목

- 방 만들기와 방 참가는 현재 UI 버튼 연결 상태입니다.
- 실제 멀티플레이 동작은 Mirror 네트워크 동기화 연동이 필요합니다.
- 실제 턴 변경, 알 개수 변경, 승패 판단은 Core/Rules 쪽 게임 로직과 연결이 필요합니다.

## 테스트 방법

### 00_MainMenu 테스트

1. Unity에서 프로젝트를 엽니다.
2. `Assets/Scenes/00_MainMenu.unity` 씬을 엽니다.
3. Play Mode를 실행합니다.
4. 메인 메뉴 버튼을 확인합니다.
   - `방 만들기`: Console에 Mirror 연동 대기 로그가 출력되는지 확인합니다.
   - `방 참가`: 참가 패널이 열리는지 확인합니다.
   - `테스트 참가`: `01_Game` 씬으로 이동하는지 확인합니다.
   - `설정`: 설정 패널이 열리는지 확인합니다.
   - `게임 종료`: Play Mode가 종료되는지 확인합니다.

### 01_Game 테스트

1. `Assets/Scenes/01_Game.unity` 씬을 엽니다.
2. Play Mode를 실행합니다.
3. HUD 표시를 확인합니다.
   - 현재 턴이 표시되는지 확인합니다.
   - P1/P2 남은 알 개수가 표시되는지 확인합니다.
   - 남은 턴 시간이 줄어드는지 확인합니다.
   - 전체 게임 시간이 증가하는지 확인합니다.
4. 결과 화면 버튼을 확인합니다.
   - Play Mode 상태에서 `UI_ResultPanel`을 활성화합니다.
   - `Retry` 버튼을 누르면 씬이 다시 시작되는지 확인합니다.
   - `MainMenu` 버튼을 누르면 `00_MainMenu` 씬으로 이동하는지 확인합니다.
   - `Exit` 버튼을 누르면 Play Mode가 종료되는지 확인합니다.

### 전체 게임 시간 종료 테스트

전체 게임 시간이 끝났을 때 HUD 가이드 문구가 정상적으로 바뀌는지 빠르게 확인하는 테스트입니다.

1. `Assets/Scenes/01_Game.unity` 씬을 엽니다.
2. Hierarchy에서 `GameUIController` 오브젝트를 선택합니다.
3. Inspector에서 `Game Session Ui Bridge` 컴포넌트를 찾습니다.
4. `Game Duration Seconds` 값을 `10`으로 변경합니다.
5. Play Mode를 실행합니다.
6. 약 10초 후 가이드 문구가 `전체 게임 시간이 종료되었습니다.`로 바뀌는지 확인합니다.
