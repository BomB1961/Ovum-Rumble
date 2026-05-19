# Person A MVP 구현계획 및 세부 구현계획

문서 버전: v0.1  
작성 목적: Person A 담당 범위인 알 물리, 입력, 발사, 스포너를 MVP 완성 기준으로 작게 구현하기 위한 작업 기준을 고정한다.

---

## 1. 전제

- 프로젝트는 PC 단일 플랫폼 게임이다.
- 입력은 마우스 클릭, 드래그, 릴리즈를 MVP 기준으로 한다.
- 모바일 터치 입력, 모바일 렌더 설정, 네트워크 입력은 MVP 범위가 아니다.
- Basic Core를 망가뜨리지 않는 범위에서만 작업한다.
- 작업 Scene은 `Assets/Scenes/A_EggPhysics_Prototype.unity`를 사용한다.
- 공용 Scene 또는 다른 팀원 담당 Scene은 직접 수정하지 않는다.

---

## 2. Person A MVP 목표

2인 핫시트 기본 알까기에서 플레이어가 자신의 알을 선택하고 마우스로 드래그해 발사할 수 있으며, 알끼리 물리 충돌하고, 주요 수치를 Inspector에서 튜닝할 수 있는 상태를 만든다.

MVP 완료 시점에는 다음이 가능해야 한다.

- P1/P2 알이 시작 위치에 생성된다.
- 마우스로 알 하나를 선택할 수 있다.
- 드래그 후 놓으면 알이 반대 방향으로 발사된다.
- 발사 힘은 최소/최대값 안으로 제한된다.
- 알끼리 `Rigidbody` 물리로 충돌한다.
- Person B의 턴/정지/승패 시스템과 연결할 수 있는 이벤트와 메서드가 준비된다.

---

## 3. 포함 범위

### 3-1. 담당 클래스

| 클래스 | MVP 책임 |
|---|---|
| `EggController` | 알 소유자, 생존 상태, Rigidbody 접근, 발사, 충돌 이벤트 |
| `FlickInputController` | 마우스 선택, 드래그 시작/종료, 발사 힘 계산 |
| `EggSpawner` | P1/P2 시작 위치에 알 Prefab 생성 |

### 3-2. Unity 구성

| 항목 | MVP 책임 |
|---|---|
| 알 Prefab | `Rigidbody`, `SphereCollider`, `EggController` 포함 |
| Physic Material | 마찰/탄성 기본값 제공 |
| 테스트 Scene | 카메라, 라이트, 임시 보드, 스포너, 입력 컨트롤러 배치 |

---

## 4. 제외 범위

아래 기능은 MVP 이후 확장으로 미룬다.

- HP 시스템
- 능력 알
- 알별 특수 외형/스탯
- 모바일 터치 입력
- 4인 모드
- LAN 멀티
- 턴 종료 판정
- 낙하 판정
- 승패 판정
- UI/HUD
- 사운드
- 파티클
- 카메라 셰이크

단, 제외 범위 시스템과 연결할 수 있도록 최소 이벤트와 공개 메서드는 준비한다.

---

## 5. 권장 파일 및 폴더 구조

기존 프로젝트 구조가 아직 단순하므로, MVP 구현 시 필요한 폴더만 생성한다.

```text
Assets/
  _Project/
    Scripts/
      Core/
        EggController.cs
        EggSpawner.cs
      Input/
        FlickInputController.cs
    Prefabs/
      Eggs/
        Egg.prefab
    Physics/
      Egg_PhysicMaterial.physicMaterial
```

주의:

- 기존에 다른 구조가 이미 있으면 그 구조를 우선한다.
- 새 추상화 폴더를 과하게 만들지 않는다.
- `Assets/Settings/Mobile_RPAsset.asset` 같은 모바일 설정 파일은 수정하지 않는다.

---

## 6. 클래스별 세부 구현계획

### 6-1. `EggController`

책임:

- 알 하나의 상태를 관리한다.
- 물리 발사는 `Rigidbody.AddForce`로 처리한다.
- 충돌 발생 시 외부 시스템에 알린다.

권장 필드:

```csharp
[SerializeField] private int ownerPlayerId;
[SerializeField] private bool isAlive = true;
[SerializeField] private Rigidbody cachedRigidbody;
```

권장 속성/메서드:

```csharp
public int OwnerPlayerId { get; }
public bool IsAlive { get; }
public Rigidbody Rigidbody { get; }
public bool CanLaunch { get; }

public void Initialize(int ownerId);
public void Launch(Vector3 impulse);
public void MarkFallen();
public void StopImmediately();
```

권장 이벤트:

```csharp
public event Action<EggController> Launched;
public event Action<EggController, float> CollisionOccurred;
public event Action<EggController> Fallen;
```

구현 기준:

- `Awake`에서 `Rigidbody`를 캐싱한다.
- `Rigidbody`가 없으면 에러 로그를 남기고 발사를 막는다.
- `Launch`는 `isAlive == true`일 때만 동작한다.
- 충돌 강도는 `collision.relativeVelocity.magnitude`를 사용한다.

---

### 6-2. `FlickInputController`

책임:

- 마우스 입력으로 알을 선택한다.
- 드래그 시작점과 종료점을 계산한다.
- 선택한 알에 발사 impulse를 전달한다.

권장 필드:

```csharp
[SerializeField] private Camera inputCamera;
[SerializeField] private LayerMask eggLayerMask;
[SerializeField] private float minForce = 1.5f;
[SerializeField] private float maxForce = 12f;
[SerializeField] private float forceMultiplier = 8f;
[SerializeField] private float maxDragDistance = 2f;
[SerializeField] private int activePlayerId = 1;
```

내부 상태:

```csharp
private EggController selectedEgg;
private Vector3 dragStartWorld;
private bool isDragging;
```

알 선택 기준:

- 현재 게임 상태가 입력 가능할 때만 선택한다.
- `EggController.IsAlive == true`인 알만 선택한다.
- `EggController.OwnerPlayerId == activePlayerId`인 알만 선택한다.
- `eggLayerMask`에 포함된 Collider만 Raycast한다.

마우스 입력 알고리즘:

```text
1. MouseDown에서 Camera.ScreenPointToRay를 만든다.
2. Raycast로 EggController를 찾는다.
3. 선택 가능한 알이면 selectedEgg와 dragStartWorld를 저장한다.
4. MouseUp에서 현재 마우스 위치의 월드 지점을 구한다.
5. dragStartWorld - dragEndWorld로 발사 방향을 계산한다.
6. 드래그 거리를 forceMultiplier와 곱한다.
7. minForce/maxForce로 Clamp한다.
8. selectedEgg.Launch(direction * force)를 호출한다.
9. 외부 시스템에 발사 이벤트를 알릴 수 있게 한다.
```

월드 지점 계산 기준:

- MVP에서는 보드 높이 기준 Plane을 사용한다.
- 기본 보드 높이는 `y = 0`으로 둔다.
- 카메라 Ray와 Plane의 교차점을 드래그 월드 좌표로 사용한다.

권장 공개 메서드:

```csharp
public void SetInputEnabled(bool enabled);
public void SetActivePlayer(int playerId);
public void ClearSelection();
```

권장 이벤트:

```csharp
public event Action<EggController> EggSelected;
public event Action<EggController> EggLaunched;
```

---

### 6-3. `EggSpawner`

책임:

- P1/P2 알을 시작 위치에 생성한다.
- 생성된 알에 owner player id를 부여한다.
- 테스트와 룰 시스템에서 생성된 알 목록을 사용할 수 있게 한다.

권장 필드:

```csharp
[SerializeField] private EggController eggPrefab;
[SerializeField] private Transform player1Root;
[SerializeField] private Transform player2Root;
[SerializeField] private int eggsPerPlayer = 6;
[SerializeField] private float spacing = 1.1f;
[SerializeField] private Vector3 player1StartCenter = new Vector3(0f, 0.5f, -3f);
[SerializeField] private Vector3 player2StartCenter = new Vector3(0f, 0.5f, 3f);
```

권장 메서드:

```csharp
public IReadOnlyList<EggController> SpawnedEggs { get; }
public void SpawnAll();
public void ClearSpawnedEggs();
```

배치 기준:

- MVP는 1인당 6개로 시작한다.
- 각 플레이어 알은 2 x 3 형태로 배치한다.
- P1은 보드 아래쪽, P2는 보드 위쪽에 배치한다.

---

## 7. 물리 기본값

초기값은 테스트를 통해 조정한다.

| 항목 | 권장 초기값 |
|---|---|
| 알 질량 | 1 |
| Drag | 0.2 |
| Angular Drag | 0.3 |
| Collision Detection | Continuous Dynamic |
| Interpolate | Interpolate |
| Physic Material Dynamic Friction | 0.35 |
| Physic Material Static Friction | 0.45 |
| Physic Material Bounciness | 0.45 |
| Friction Combine | Average |
| Bounce Combine | Maximum |

주의:

- 발사감은 `Rigidbody` 값보다 `forceMultiplier`, `minForce`, `maxForce`에서 먼저 조정한다.
- 턴 종료 안정성은 Person B의 `MotionResolver`와 함께 조정한다.

---

## 8. 테스트 Scene 구성

Scene: `Assets/Scenes/A_EggPhysics_Prototype.unity`

MVP 테스트에 필요한 최소 구성:

- `Main Camera`
- `Directional Light`
- 임시 보드 Cube
- `EggSpawner`
- `FlickInputController`
- 알 Prefab 1종
- 알 Layer

권장 카메라:

- 보드 전체가 보이는 탑다운 사선 시점
- MVP에서는 Cinemachine 없이 기본 Camera 사용 가능

권장 임시 보드:

- 크기: `8 x 0.4 x 8`
- 위치: `(0, -0.2, 0)`
- Collider 포함
- 낙하 판정은 Person B 범위이므로 MVP 테스트 Scene에서는 보드 밖 이동만 육안 확인한다.

---

## 9. Person B/C/D 연결 지점

Person A는 아래 연결 지점까지만 준비한다.

| 대상 | 연결 방식 |
|---|---|
| Person B 턴 시스템 | `FlickInputController.SetActivePlayer`, `SetInputEnabled` |
| Person B 정지/승패 시스템 | `EggController.Rigidbody`, `IsAlive`, `MarkFallen` |
| Person C UI | `EggSelected`, `EggLaunched` 이벤트 |
| Person D 사운드/이펙트 | `CollisionOccurred`, `Launched`, `Fallen` 이벤트 |

Person A 범위에서는 턴 진행, UI 표시, 사운드 재생을 직접 구현하지 않는다.

---

## 10. 작업 단계 및 검증 기준

### Step A1. 계획 문서 고정

작업:

- 이 문서를 작성하고 사용자 승인을 받는다.

검증:

- MVP 범위와 제외 범위가 명확하다.
- Person A 작업이 다른 팀원 범위를 침범하지 않는다.

### Step A2. 폴더 구조 생성

작업:

- 필요한 `Assets/_Project` 하위 폴더만 생성한다.

검증:

- 공용 Scene과 모바일 설정 파일이 변경되지 않는다.

### Step A3. `EggController` 구현

작업:

- 알 상태, Rigidbody 캐싱, 발사, 충돌 이벤트를 구현한다.

검증:

- 컴파일 에러가 없다.
- Rigidbody 없는 경우를 방어한다.
- Inspector에서 owner id를 확인할 수 있다.

### Step A4. `EggSpawner` 구현

작업:

- P1/P2 알을 6개씩 생성한다.

검증:

- Play Mode에서 알 12개가 생성된다.
- P1/P2 owner id가 올바르다.
- 중복 생성 전 기존 생성물을 정리할 수 있다.

### Step A5. `FlickInputController` 구현

작업:

- 마우스 선택과 드래그 발사를 구현한다.

검증:

- 현재 플레이어 알만 선택된다.
- 드래그 방향 반대로 발사된다.
- 힘이 너무 약하거나 강하지 않게 제한된다.

### Step A6. Prefab/Physic Material 구성

작업:

- 알 Prefab과 물리 재질을 만든다.

검증:

- 알끼리 충돌한다.
- 알이 보드 위에서 자연스럽게 감속한다.
- 주요 값이 Inspector에서 조절 가능하다.

### Step A7. 테스트 Scene 조립

작업:

- `A_EggPhysics_Prototype.unity`에서 Person A 기능을 육안 테스트할 수 있게 배치한다.

검증:

- Play Mode에서 알 생성, 선택, 발사, 충돌이 가능하다.
- Console에 Error가 없다.

### Step A8. 사용자 시각 테스트 승인

작업:

- 사용자가 Unity Editor에서 직접 확인한다.

검증:

- 사용자가 기능 동작을 승인한다.

### Step A9. 커밋/푸시

작업:

- 승인된 기능 단위로 커밋하고 원격 브랜치에 푸시한다.

검증:

- 커밋에는 Person A MVP 범위 변경만 포함된다.

---

## 11. MVP 완료 DoD

Person A MVP는 아래 항목을 모두 만족해야 완료로 본다.

- Unity Editor에서 컴파일 에러가 없다.
- `A_EggPhysics_Prototype.unity`가 열린 상태에서 Play Mode 실행이 가능하다.
- P1/P2 알이 각각 6개씩 생성된다.
- 현재 플레이어 알만 선택 가능하다.
- 마우스 드래그 후 릴리즈로 알이 발사된다.
- 발사 힘이 `minForce`와 `maxForce` 사이로 제한된다.
- 알끼리 충돌하고 서로 밀린다.
- 주요 물리/입력 수치를 Inspector에서 조절할 수 있다.
- 모바일 관련 설정 파일이 변경되지 않는다.
- Person B/C/D 연결용 이벤트와 메서드가 준비되어 있다.
- 사용자 시각 테스트 승인을 받았다.

---

## 12. 작업 중 체크 규칙

MVP 완성 전까지 새 작업을 시작하기 전에 아래를 확인한다.

```text
1. 지금 작업이 Person A MVP 범위인가?
2. 지금 작업이 이 문서의 Step 중 어디에 해당하는가?
3. Basic Core를 망가뜨릴 위험이 있는가?
4. 다른 팀원 담당 범위를 직접 구현하고 있지 않은가?
5. 모바일/확장 기능을 건드리고 있지 않은가?
6. 변경 파일이 의도한 범위 안에 있는가?
```

위 질문 중 하나라도 애매하면 구현을 멈추고 사용자에게 확인한다.

---

## 13. 기능 단위 작업 운영 워크플로우

Person A MVP 작업은 기능 하나를 작은 단위로 끝내고 검증한 뒤 다음 기능으로 넘어가는 루프를 따른다.

### 13-1. 기본 루프

```text
1. 현재 기능이 Person A MVP 범위인지 확인한다.
2. Codex가 구현 계획을 짧게 제시하고 사용자 승인을 받는다.
3. Codex가 프로그래밍 작업과 필요한 Unity 작업을 진행한다.
4. Codex가 Unity Editor에서 가능한 범위의 검토와 검증 테스트를 수행한다.
5. Codex가 직접 수행할 수 없는 검증 또는 조작만 사용자에게 절차적으로 요청한다.
6. 사용자가 Unity Editor에서 기능을 테스트하고 문제가 없다고 승인한다.
7. Codex가 승인된 변경만 커밋하고 원격 기능 브랜치에 푸시한다.
8. 기능 브랜치를 `main`에 병합한다.
9. `WORKLOG.md`에 작업 내용, 검증 결과, 남은 이슈를 갱신한다.
10. 다음 기능도 같은 루프로 반복한다.
```

### 13-2. Codex 검증 책임

Codex는 커밋 전 가능한 범위에서 아래를 먼저 확인한다.

- Unity 프로젝트 연결은 프로젝트 경로 기준으로 확인한다.
- Unity 컴파일 에러가 없는지 확인한다.
- Console Error가 없는지 확인한다.
- 대상 Scene 또는 Prefab이 의도한 범위에서만 변경되었는지 확인한다.
- 변경 파일이 Person A MVP 범위 밖으로 벗어나지 않았는지 확인한다.
- `Assets/Settings/Mobile_RPAsset.asset` 같은 모바일 설정 파일을 수정하지 않았는지 확인한다.

Codex가 Unity Editor 조작, Play Mode 확인, Scene 저장, 시각 판단 등을 직접 수행할 수 없는 경우에는 임의로 완료 처리하지 않고 사용자에게 필요한 절차를 순서대로 요청한다.

### 13-3. 사용자 승인 기준

사용자는 Unity Editor에서 해당 기능을 직접 실행해 보고 아래를 확인한다.

- 기능이 문서의 검증 기준대로 동작한다.
- 눈에 보이는 이상 동작이 없다.
- Console에 새 Error가 없다.
- 다음 단계로 넘어가도 된다고 승인한다.

사용자 승인이 없으면 기능 완료 커밋, 원격 푸시, `main` 병합을 진행하지 않는다.

### 13-4. Git 및 작업일지 기준

- `main`은 직접 수정하지 않고 기능 브랜치에서 작업한다.
- 커밋에는 승인된 기능 단위 변경만 포함한다.
- 원격 기능 브랜치에 푸시한 뒤 `main` 병합을 진행한다.
- 병합 후 `main` 기준 실행 가능 상태를 확인한다.
- `WORKLOG.md`에는 구현 내용, 검증 내용, 사용자 승인 여부, 후속 작업을 남긴다.
- 병합이나 검증 과정에서 충돌 또는 실패가 발생하면 즉시 중단하고 사용자에게 상태와 선택지를 보고한다.
