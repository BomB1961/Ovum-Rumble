# Ovum Rumble

## Map Select Flow 작업

이번 작업은 `00_MainMenu`에서 맵 선택 화면으로 이동하고, 선택한 맵으로 `01_Game`을 시작하는 흐름을 추가한 작업입니다.

기존에는 게임 시작 흐름에서 맵 선택 단계가 없었지만, 이번 작업으로 Terrian / Ice / Desert 중 하나를 선택한 뒤 해당 맵으로 게임을 시작할 수 있도록 구성했습니다.

```
브랜치 정리

C 관련 브랜치는 작업 순서 기준으로 아래처럼 정리합니다.

순서	브랜치	역할
1	origin/feature/C-ui-flow	C UI 흐름 뼈대 작업 브랜치
2	origin/feature/C-add-ui-features	C UI 기능 추가 작업 브랜치
3	origin/feature/C-ui-integration-test	01_Game 실제 게임 씬과 C UI 통합 테스트 작업 브랜치
4	origin/feature/C-map-select-flow	맵 선택 흐름 추가 작업 브랜치, 현재 C 작업
```
---

## 작업 브랜치

```text
feature/C-map-select-flow
```

해당 작업은 PR 요청을 올린 상태입니다.

---

## 작업 목적

이번 작업의 목적은 최종 맵 디자인 완성이 아니라, **메인 메뉴 → 맵 선택 → 선택 맵으로 게임 시작** 흐름이 실제 게임 씬에서 동작하는지 확인하는 것입니다.

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

## 변경된 파일

이번 작업에서 추가 / 변경된 주요 파일은 다음과 같습니다.

```text
Assets/Scenes/02_MapSelect.unity
Assets/_Project/Scripts/Core/MapId.cs
Assets/_Project/Scripts/Core/GameLaunchContext.cs
Assets/_Project/Scripts/Presentation/MapSelectController.cs
Assets/_Project/Scripts/Environment/ProceduralBoardGenerator.cs
Assets/_Project/Prefabs/Board/Board_Ice_Wrapper.prefab
Assets/_Project/Prefabs/Board/Board_Desert_Wrapper.prefab
```

---

## 파일별 작업 내용

| 파일                            | 작업 내용                                                      |
| ----------------------------- | ---------------------------------------------------------- |
| `02_MapSelect.unity`          | Terrian / Ice / Desert 맵 선택 화면을 추가했습니다.                    |
| `MapId.cs`                    | 선택 가능한 맵 ID를 정의했습니다.                                       |
| `GameLaunchContext.cs`        | 씬 전환 중 선택 맵 정보를 저장하도록 구성했습니다.                              |
| `MapSelectController.cs`      | 맵 선택 버튼 클릭 처리와 `01_Game` 로드를 담당합니다.                        |
| `ProceduralBoardGenerator.cs` | Ice / Desert 선택 시 원본 프리팹 대신 Wrapper 프리팹을 생성하도록 참조를 변경했습니다. |
| `Board_Ice_Wrapper.prefab`    | 원본 Ice 프리팹을 직접 수정하지 않고 보정하기 위한 Wrapper 프리팹입니다.             |
| `Board_Desert_Wrapper.prefab` | 원본 Desert 프리팹을 직접 수정하지 않고 보정하기 위한 Wrapper 프리팹입니다.          |

---

## 작업 방식

```text
00_MainMenu
= 게임 시작 진입 지점

02_MapSelect
= Terrian / Ice / Desert 맵 선택 씬

01_Game
= 선택한 맵으로 실제 게임이 실행되는 씬
```

`01_Game`은 보드를 씬에 고정 배치하지 않고, `ProceduralBoardGenerator`가 런타임에 선택된 맵 프리팹을 생성하는 방식입니다.

이번 작업에서는 이 런타임 생성 방식을 유지했습니다.

---

## 맵 선택 흐름

```text
00_MainMenu
→ 플레이 시작 / 방 만들기
→ 02_MapSelect
→ Terrian / Ice / Desert 선택
→ GameLaunchContext에 선택 맵 저장
→ 01_Game 로드
→ ProceduralBoardGenerator가 선택 맵 생성
```

---

## Terrian 처리

Terrian은 기존 정상 프리팹을 그대로 사용했습니다.

```text
Terrian 선택
→ 기존 Terrian.prefab 생성
```

Terrian은 현재 Ice / Desert 보정의 기준 맵입니다.

---

## Ice 처리

Ice 원본 프리팹은 팀원 작업물이므로 직접 수정하지 않았습니다.

대신 Wrapper 프리팹을 새로 추가했습니다.

```text
Board_Ice_Wrapper
  └ Ice 원본 프리팹 인스턴스
```

작업 내용:

* 원본 `Ice.prefab` 직접 수정 없음
* `Board_Ice_Wrapper.prefab` 추가
* Wrapper 안에 원본 Ice 프리팹 인스턴스 배치
* 자식 Ice Transform을 조정해 벽처럼 서 있던 문제를 보정
* 위치가 살짝 어긋난 부분은 Wrapper 자식 Transform 기준으로 추가 조정

---

## Desert 처리

Desert 원본 프리팹도 팀원 작업물이므로 직접 수정하지 않았습니다.

대신 Wrapper 프리팹을 새로 추가했습니다.

```text
Board_Desert_Wrapper
  └ Desert 원본 프리팹 인스턴스
```

작업 내용:

* 원본 `Desert.prefab` 직접 수정 없음
* `Board_Desert_Wrapper.prefab` 추가
* Wrapper 안에 원본 Desert 프리팹 인스턴스 배치
* 자식 Desert Transform을 조정해 벽처럼 서거나 크게 밀리는 문제를 보정
* 축별 Scale을 다르게 주면 원본 모델이 찌그러질 수 있어 균등 Scale 유지가 필요함

---

## 원본 프리팹 보호

이번 작업에서는 팀원 원본 프리팹을 직접 수정하지 않는 방향으로 진행했습니다.

수정하지 않은 원본:

```text
Assets/_Project/Prefabs/Board/Ice.prefab
Assets/_Project/Prefabs/Board/Desert.prefab
Assets/_Project/Prefabs/Board/Terrian.prefab
```

확인 내용:

```text
git diff 기준 원본 Ice / Desert 프리팹 변경 없음
```

---

## 현재 남은 문제

Ice / Desert에서 알이 맵에 먹히거나 이상하게 튕기는 문제가 남아 있습니다.

원인 추정:

```text
비주얼 Transform은 보정했지만,
실제 Collider / 플레이 표면 기준이 Terrian과 완전히 맞지 않음
```

Ice / Desert 모델 자체 Collider가 알과 직접 충돌하면서 물리 반응이 불안정한 것으로 보입니다.

---

## 다음 작업 제안

Ice / Desert는 비주얼 전용으로 두고, 실제 물리 충돌은 별도 GameplayCollider가 담당하도록 분리하는 방향이 좋습니다.

예상 구조:

```text
Board_Ice_Wrapper
  ├ Ice                # Visual 전용
  └ GameplayCollider  # 실제 충돌용 평평한 Collider

Board_Desert_Wrapper
  ├ Desert             # Visual 전용
  └ GameplayCollider  # 실제 충돌용 평평한 Collider
```

작업 방향:

* 원본 Ice / Desert 프리팹 수정 금지
* Wrapper 안에서만 Collider 조정
* 자식 Ice / Desert의 Collider가 충돌을 방해하면 Wrapper 내부 인스턴스에서 비활성화 검토
* GameplayCollider는 Terrian 기준의 평평한 플레이 표면으로 구성

---

## 이번 작업에서 하지 않은 것

* Ice / Desert 원본 프리팹 직접 수정
* `01_Game` Hierarchy에 보드 고정 배치
* 런타임 생성 방식을 고정 배치 방식으로 변경
* Camera 수정
* EggSpawner 수정
* TurnController 수정
* MotionResolver 수정
* WinConditionChecker 수정
* 맵 선택 로직 전체 리팩토링
* 새 Manager 또는 Singleton 추가
* 실제 AI 플레이 로직 추가
* 네트워크 / 방 만들기 실제 연결

---

## 확인 방법

1. `feature/C-map-select-flow` 브랜치를 받습니다.
2. `Assets/Scenes/00_MainMenu.unity` 또는 시작 씬을 엽니다.
3. Play Mode를 실행합니다.
4. 메인 메뉴에서 맵 선택 화면으로 이동합니다.
5. Terrian / Ice / Desert 버튼을 각각 눌러 `01_Game` 로드를 확인합니다.
6. 각 맵이 생성되는지 확인합니다.
7. Console Error가 없는지 확인합니다.

추가 확인 필요:

* Ice / Desert에서 알이 보드 위에 안정적으로 올라오는지
* Ice / Desert에서 알이 먹히거나 튕기지 않는지
* Collider 기준이 Terrian과 맞는지

---

## 브랜치 정리

### 현재 PR 브랜치

```text
feature/C-map-select-flow
```

현재 맵 선택 흐름 작업 브랜치입니다.

포함 내용:

* `02_MapSelect` 씬
* 맵 선택 코드
* Ice / Desert Wrapper 프리팹
* `ProceduralBoardGenerator` Wrapper 참조 변경

---
