# 공룡 알까기 게임 디자인 (Game Design)

---

## 1. MVP 게임 룰

### 1-1. 기본 세팅

| 항목 | 값 |
|---|---|
| 플레이어 | 2명 |
| 조작 방식 | 같은 PC에서 번갈아 조작 (핫시트) |
| 알 개수 | 1인당 6개 (권장) 또는 8개 |
| 보드 | 정사각형 3D 보드 |
| 턴 순서 | P1 -> P2 -> P1 -> P2 반복 |
| 승리 조건 | 상대 생존 알이 0개가 되면 승리 |

### 1-2. 턴 진행

1. 현재 플레이어의 알 중 하나를 선택한다.
2. 마우스로 드래그한다.
3. 마우스를 놓으면 알이 발사된다.
4. 알들이 움직이는 동안 입력을 막는다.
5. 모든 알이 멈추거나 최대 해석 시간이 지나면 턴을 끝낸다.
6. 낙하/승패를 확인한다.
7. 다음 플레이어 턴으로 넘어간다.

---

## 2. 재미의 핵심

이 게임의 재미는 복잡한 기능보다 아래 장면에서 나온다.

- 내 알이 상대 알을 정확히 맞힌다.
- 상대 알이 보드 끝에서 아슬아슬하게 버틴다.
- 작은 힘 조절 차이로 결과가 크게 달라진다.
- 마지막 알이 떨어지며 승패가 결정된다.
- 확장 단계에서는 폭발물이 판을 흔든다.

개발 우선순위:
1. 알을 튕기는 손맛
2. 턴이 안정적으로 끝나는 구조
3. 승패가 명확한 룰
4. 사운드/카메라/파티클로 충돌감 강화
5. 환경 모듈 추가

---

## 3. 아트 방향

### 3-1. 기본 톤
- 저폴리 또는 캐주얼 3D
- 공룡 둥지/숲 분위기
- 따뜻한 갈색, 호박색, 풀색 계열
- 과도한 사실감보다 명확한 식별성 우선

### 3-2. 제작 우선순위
| 방식 | 대상 |
|---|---|
| Blender MCP 자체 제작 | 알, 보드, 둥지, 돌, 뼈, 폭발물, 단순 UI 장식 |
| 무료/오픈 라이선스 에셋 | 공룡 모델, 자연 프롭, UI 팩 |
| 3D 생성형 AI | 무료 에셋으로 해결 안 되는 보조 프롭 |

### 3-3. 공룡 식별성
MVP에서 공룡 NPC는 없어도 된다. 하지만 공룡 테마는 반드시 보여야 한다.
- 알은 공룡알 형태로 만든다.
- 보드는 둥지 또는 숲 느낌을 준다.
- 배경 프롭으로 뼈, 발자국, 알껍질, 나뭇잎 등을 배치한다.

---

## 4. 핵심 시스템 구조

### 4-1. 모듈 구성

| 모듈 | 역할 | MVP 여부 |
|---|---|---|
| Core | 알, 보드, 생존 상태 | 필수 |
| Input | 드래그 입력, 발사 힘 계산 | 필수 |
| Rules | 턴, 정지 판정, 승패 | 필수 |
| Presentation | UI, 카메라, 사운드, 파티클 | 필수 |
| Environment | 폭발물, 바람, 지진, 공룡 NPC | 확장 |
| Data | ScriptableObject 설정값 | 필수 |
| Network | LAN 멀티 | 장기 확장 |

### 4-2. 핵심 클래스

| 클래스 | 담당 | 설명 |
|---|---|---|
| `GameSessionController` | 게임 전체 흐름 | 경기 시작, 상태 전환, 결과 처리 |
| `TurnController` | 턴 관리 | 현재 플레이어, 턴 시작/종료 |
| `EggController` | 알 개별 상태 | 소유자, 생존 여부, Rigidbody 접근 |
| `FlickInputController` | 입력 | 드래그 시작/종료, 힘 계산 |
| `MotionResolver` | 정지 판정 | 모든 알이 멈췄는지 확인 |
| `BoardFallZone` | 낙하 감지 | Trigger에 닿은 알을 제거 처리 |
| `WinConditionChecker` | 승패 판정 | 생존 알 수로 승패 결정 |
| `EggSpawner` | 알 생성 | 시작 위치에 알 배치 |
| `HudPresenter` | HUD 표시 | 현재 턴, 남은 알 수 표시 |
| `CameraController` | 카메라 | 탑다운, 추적, 셰이크 |
| `AudioManager` | 사운드 | BGM/SFX 재생 |
| `EffectController` | 이펙트 | 충돌 먼지, 낙하, 폭발 파티클 |
| `BombEventController` | 폭발물 | v0.2 환경 모듈 |

---

## 5. 게임 상태 머신

```csharp
public enum GameState
{
    Setup,          // 알 배치, 초기화 중
    Aiming,         // 현재 플레이어가 알을 고르는 중
    Resolving,      // 알이 움직이는 중. 입력 금지
    CheckingResult, // 낙하/승패/턴 종료 확인
    Result,         // 승패 화면
    Paused          // 일시정지
}
```

---

## 6. 이벤트 설계

| 이벤트 | 발생 위치 | 구독하는 시스템 |
|---|---|---|
| `OnGameStarted` | GameSessionController | HUD, Audio, Camera |
| `OnTurnStarted(int playerId)` | TurnController | HUD, Audio, Input |
| `OnEggLaunched(EggController egg)` | FlickInputController | Turn, Camera, Audio |
| `OnEggCollision(float impact)` | EggController | Audio, Effect, Camera |
| `OnEggFell(EggController egg)` | BoardFallZone | Rules, HUD, Audio, Effect |
| `OnAllEggsStopped` | MotionResolver | Turn, Rules |
| `OnGameEnded(GameResult result)` | WinConditionChecker | UI, Audio, Camera |

---

## 7. MVP 필수 알고리즘

### 7-1. 드래그 발사 알고리즘

```
1. 마우스 클릭 시 시작 위치 저장
2. 마우스를 놓을 때 끝 위치 저장
3. 시작 위치 - 끝 위치 = 발사 방향
4. 벡터 길이 = 발사 힘
5. 최소/최대 힘으로 Clamp
6. Rigidbody.AddForce(..., ForceMode.Impulse)
```

### 7-2. 정지 판정 알고리즘

```
1. 모든 생존 알의 속도를 확인
2. 모든 알 속도가 stopVelocity 이하인지 확인
3. 그 상태가 stopHoldTime 이상 유지되면 정지로 인정
4. OnAllEggsStopped 이벤트 발생
```

초기 권장값:

| 값 | 권장 |
|---|---|
| `stopVelocity` | 0.08 |
| `stopHoldTime` | 1.0초 |
| `maxResolveTime` | 8~10초 |

### 7-3. 강제 턴 종료 알고리즘

알이 미세하게 계속 움직여 턴이 끝나지 않는 문제를 막는다.

```
1. 발사 직후 resolveTimer 시작
2. maxResolveTime을 넘으면 모든 알 속도 0 처리
3. 턴 종료
```

### 7-4. 낙하 판정 알고리즘

보드 아래 또는 외곽에 큰 Trigger Collider를 배치한다.
알이 Trigger에 닿으면 `EggController.MarkFallen()` 호출. 해당 알은 비활성화하거나 낙하 연출 후 제거한다.

### 7-5. 승패 판정 알고리즘

```
1. 각 플레이어의 생존 알 수 계산
2. P1 생존 알이 0개면 P2 승리
3. P2 생존 알이 0개면 P1 승리
4. 아니면 다음 턴 진행
```

### 7-6. 폭발물 알고리즘 (v0.2)

```
1. 폭발물 생성
2. 카운트다운 표시
3. 폭발 시 Physics.OverlapSphere로 주변 알 탐색
4. 거리 비례 힘 계산
5. 각 알 Rigidbody에 AddForce 적용
```

---

## 8. 확장 모듈 설계

모든 확장 기능은 `FeatureFlags`로 켜고 끌 수 있게 만든다.

```csharp
public class FeatureFlags : ScriptableObject
{
    public bool enableBomb;
    public bool enableWind;
    public bool enableEarthquake;
    public bool enableDinosaurNpc;
}
```

| 모듈 | 의존성 | 꺼도 되는가 |
|---|---|---|
| 폭발물 | Core, Physics, Effect, Audio | Yes |
| 바람 | Core, Physics, HUD | Yes |
| 지진 | Camera, Physics | Yes |
| 공룡 NPC | NavMesh, Animation, Core | Yes |
| HP | EggController, UI | Yes |
| 능력 알 | EggType Data, Strategy | Yes |
| LAN | Network Module | Yes |

---

## 9. 아트/프리팹 제작 파이프라인

### 제작 우선순위
1. Unity 기본 도형으로 먼저 기능 구현
2. Blender MCP로 간단한 자체 모델 제작
3. 무료/오픈 라이선스 에셋 사용
4. 3D 생성형 AI로 보조 제작

### Blender MCP로 만들 대상
기본 공룡알, 보드 베이스, 둥지, 돌/뼈/나무토막, 진영 표시 디스크, 폭발물, 단순 UI 장식

### 3D 생성형 AI 사용 규칙
- 유명 IP 이름 사용 금지. 특정 작가 스타일 사용 금지.
- 생성 결과는 Blender에서 후처리한다.
- 사용한 툴, 프롬프트, 날짜를 기록한다.
- 상업 사용 약관을 확인한다.

---

## 10. 용어 설명

| 용어 | 설명 |
|---|---|
| MVP | 최소 기능으로 완성된 첫 버전. 작지만 플레이 가능해야 한다. |
| 모듈 | 독립적으로 켜고 끌 수 있는 기능 덩어리 |
| 핫시트 (Hot-seat) | 같은 PC에서 여러 명이 번갈아 조작하는 방식 |
| 해석 시간 (Resolve Time) | 발사 후 물리 움직임을 지켜보는 시간. 알이 계속 미세하게 움직이면 턴이 끝나지 않으므로 최대 시간을 둔다. |
| Rigidbody | Unity 물리 엔진에서 물체를 움직이게 하는 컴포넌트 |
| Collider | 충돌 판정을 담당하는 컴포넌트 |
| Trigger | 물체가 닿았는지만 감지하고 물리적으로 막지는 않는 Collider 설정 |
| ScriptableObject | Unity에서 수치와 데이터를 파일처럼 저장하는 방식 |
| Prefab | 미리 만들어 둔 오브젝트 템플릿 |
| Vibe Coding | AI에게 의도를 설명하고 코드를 생성/수정하게 하며 개발하는 방식 |
| Clamp | 값이 너무 작거나 커지지 않도록 최소/최대 범위 안에 가두는 처리 |
| 상태 머신 (State Machine) | 게임이 지금 어떤 상태인지 명확히 나누는 방식 |
| 이벤트 (Event) | 어떤 일이 일어났음을 다른 시스템에 알려주는 신호 |
| OverlapSphere | 특정 반경 안에 있는 Collider를 찾아주는 Unity 물리 함수 |
| 기능 동결 (Feature Freeze) | 새 기능을 더 넣지 않고 버그 수정과 마감만 하는 시점 |
| 디버그 UI | 개발 중에 현재 상태, 알 속도, 생존 알 수 등을 확인하는 임시 UI |
| Controller | 특정 흐름이나 규칙을 관리하는 스크립트 |
| Presenter | 데이터를 UI에 보여주는 스크립트 |
| CC0 | 사실상 저작권 포기에 가까운 라이선스. 상업 사용과 수정이 자유롭다. |
| CC-BY | 사용 가능하지만 저작자 표시가 필요하다. |
