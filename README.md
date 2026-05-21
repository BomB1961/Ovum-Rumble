# Ovum Rumble

## Gameplay + C UI 통합 작업

이번 작업은 `B_EggPhysics_Prototype`의 실제 게임플레이를 기반으로 `01_Game` 씬을 구성하고, C 담당 UI를 실제 게임 이벤트에 연결한 작업입니다.

기존 UI 테스트용 화면이 아니라, 실제 알까기 게임이 동작하는 씬에서 HUD와 Result UI가 함께 작동하도록 통합했습니다.

---

## 작업 브랜치

```text
feature/C-ui-integration-test
```

해당 작업은 `main`에도 반영된 상태입니다.

---

## 작업 목적

이번 작업의 목적은 최종 디자인 완성이 아니라, **실제 게임플레이 씬과 C UI가 함께 동작하는지 확인하는 것**입니다.

---

## 작업 내용

* `B_EggPhysics_Prototype` 기반 게임플레이를 `01_Game` 씬에 통합
* C UI를 실제 게임 흐름에 연결
* HUD 현재 턴 표시 연결
* Result 화면 연결
* Result 버튼 연결

  * 한 판 더
  * 메인 메뉴
  * 게임 종료
* Result 화면 TextMeshPro Missing 문제 수정
* `BlackHanSans-Regular` 폰트 연결
* BGM / SFX 슬라이더 값 유지 흐름 확인
* 릴리스 시 HUD 턴 표시가 빠르게 바뀌도록 조정

---

## 변경된 파일

이번 작업에서 변경된 파일은 총 8개입니다.

```text
Assets/_Project/Scripts/Core/GameSessionController.cs
Assets/_Project/Scripts/Presentation/GameSessionUiBridge.cs
Assets/_Project/Scripts/Rules/TurnController.cs
Assets/Scenes/01_Game.unity
Assets/Scenes/01_Game_UI_Source.unity
Assets/TextMesh Pro/Resources/Fonts & Materials/BlackHanSans-Regular SDF.asset
ProjectSettings/EditorBuildSettings.asset
ProjectSettings/TimelineSettings.asset
```

---

## 파일별 작업 내용

| 파일                               | 작업 내용                                           |
| -------------------------------- | ----------------------------------------------- |
| `GameSessionController.cs`       | 게임 흐름과 UI 연결 흐름을 조정했습니다.                        |
| `GameSessionUiBridge.cs`         | 실제 게임 이벤트와 UI 표시 사이를 연결했습니다.                    |
| `TurnController.cs`              | 턴 진행 흐름과 HUD 표시 타이밍을 조정했습니다.                    |
| `01_Game.unity`                  | 실제 게임플레이와 C UI가 함께 동작하는 통합 씬으로 수정했습니다.          |
| `01_Game_UI_Source.unity`        | 기존 C UI 구조를 참고하기 위한 UI 소스 씬으로 사용했습니다.           |
| `BlackHanSans-Regular SDF.asset` | TextMeshPro 폰트 Missing 문제 해결을 위해 폰트 연결을 정리했습니다. |
| `EditorBuildSettings.asset`      | 빌드에 포함되는 씬 설정을 확인 및 반영했습니다.                     |
| `TimelineSettings.asset`         | Unity ProjectSettings 변경 사항으로 함께 반영되었습니다.       |

---

## 작업 방식

```text
B_EggPhysics_Prototype
= 실제 게임플레이 기반 씬

01_Game
= 실제 게임플레이 + C UI 통합 씬
```

실제 게임플레이가 동작하는 `B_EggPhysics_Prototype`을 기준으로 삼고, C UI를 `01_Game`에 연결해 최종 게임 씬 형태로 정리했습니다.

---

## Result 화면

Result 화면은 승자 기준으로 단순하게 표시하도록 정리했습니다.

```text
P1 승리!

남은 알 3
1승

[한 판 더] [메인 메뉴] [게임 종료]
```

---

## 턴 표시

기존에는 알이 완전히 멈춘 뒤에 현재 턴 표시가 바뀌어 늦게 느껴졌습니다.

이번 작업에서는 알을 릴리스하는 순간 HUD 표시만 다음 플레이어로 빠르게 바뀌도록 조정했습니다.

```text
P1 릴리스
→ HUD 현재 차례 표시를 P2로 변경
→ 알이 움직이는 동안 실제 입력은 잠금
→ 알이 멈춘 뒤 P2 입력 허용
```

실제 입력까지 바로 넘기면 물리 충돌, 낙하 판정, 승패 판정이 꼬일 수 있어 적용하지 않았습니다.

---

## 이번 작업에서 하지 않은 것

* 서버 / 방 만들기 실제 연결
* 방 참가 실제 연결
* 새 네트워크 시스템 구현
* 새 Manager 또는 Singleton 추가
* 새 폰트 추가
* 새 사운드 에셋 추가
* 물리 시스템 구조 변경
* 전체 UI 리디자인

---

## 확인 방법

1. `main`을 pull 받습니다.
2. `Assets/Scenes/01_Game.unity` 씬을 엽니다.
3. Play Mode를 실행합니다.
4. 알 발사, HUD 턴 표시, Result 화면을 확인합니다.
5. Result 버튼 3개가 보이는지 확인합니다.

   * 한 판 더
   * 메인 메뉴
   * 게임 종료
