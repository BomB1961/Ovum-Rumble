# 공룡 알까기 구현계획 및 세부구현계획

문서 버전: v2.0  
작성 목적: 4인 팀이 vibe coding으로 개발하더라도 같은 구조, 같은 이름, 같은 기준으로 구현하도록 만든다.

---

## 1. 구현 전략 요약

이 프로젝트는 아래 순서로 만든다.

```text
1. Basic Core
   알 발사, 충돌, 낙하, 턴, 승패

2. Playable MVP
   UI, 결과 화면, 재시작, 기본 사운드

3. Presentation
   공룡알 비주얼, 보드 아트, 카메라, 파티클

4. Environment Modules
   폭발물, 바람, 지진, 공룡 NPC

5. Expansion Modules
   HP, 능력 알, 부화, 4인 모드, LAN 멀티
```

가장 중요한 기준은 다음이다.

> **새 기능이 실패해도 Basic Core는 계속 플레이 가능해야 한다.**

---

## 2. 권장 Unity 폴더 구조

```text
Assets/
  _Project/
    Scenes/
      Main.unity
      Prototype.unity
    Scripts/
      Core/
      Input/
      Rules/
      Presentation/
      Environment/
      Data/
      Utility/
    Prefabs/
      Eggs/
      Board/
      UI/
      Effects/
      Environment/
    ScriptableObjects/
      Rules/
      Physics/
      Audio/
      FeatureFlags/
    Art/
      Models/
      Materials/
      Textures/
      UI/
    Audio/
      BGM/
      SFX/
    ThirdParty/
    Resources/
```

> **ThirdParty**: 외부에서 받은 라이브러리나 에셋을 넣는 폴더다. 출처와 라이선스를 반드시 기록한다.

---

## 3. 핵심 시스템 구조

### 3-1. 모듈 구성

| 모듈 | 역할 | MVP 여부 |
|---|---|---|
| Core | 알, 보드, 생존 상태 | 필수 |
| Input | 드래그 입력, 발사 힘 계산 | 필수 |
| Rules | 턴, 정지 판정, 승패 | 필수 |
| Presentation | UI, 카메라, 사운드, 파티클 | 필수 |
| Environment | 폭발물, 바람, 지진, 공룡 NPC | 확장 |
| Data | ScriptableObject 설정값 | 필수 |
| Network | LAN 멀티 | 장기 확장 |

### 3-2. 핵심 클래스

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

> **Controller**: 특정 흐름이나 규칙을 관리하는 스크립트다.  
> **Presenter**: 데이터를 UI에 보여주는 스크립트다.

---

## 4. 게임 상태 머신

게임 흐름은 `GameState`로 관리한다.

```csharp
public enum GameState
{
    Setup,
    Aiming,
    Resolving,
    CheckingResult,
    Result,
    Paused
}
```

| 상태 | 의미 |
|---|---|
| `Setup` | 알 배치, 초기화 중 |
| `Aiming` | 현재 플레이어가 알을 고르는 중 |
| `Resolving` | 알이 움직이는 중. 입력 금지 |
| `CheckingResult` | 낙하/승패/턴 종료 확인 |
| `Result` | 승패 화면 |
| `Paused` | 일시정지 |

> **상태 머신(State Machine)**: 게임이 지금 어떤 상태인지 명확히 나누는 방식이다. 입력, UI, 사운드가 서로 꼬이는 것을 줄여준다.

---

## 5. 이벤트 설계

팀원이 각자 만든 기능을 연결하기 위해 이벤트 이름을 고정한다.

| 이벤트 | 발생 위치 | 구독하는 시스템 |
|---|---|---|
| `OnGameStarted` | `GameSessionController` | HUD, Audio, Camera |
| `OnTurnStarted(int playerId)` | `TurnController` | HUD, Audio, Input |
| `OnEggLaunched(EggController egg)` | `FlickInputController` | Turn, Camera, Audio |
| `OnEggCollision(float impact)` | `EggController` | Audio, Effect, Camera |
| `OnEggFell(EggController egg)` | `BoardFallZone` | Rules, HUD, Audio, Effect |
| `OnAllEggsStopped` | `MotionResolver` | Turn, Rules |
| `OnGameEnded(GameResult result)` | `WinConditionChecker` | UI, Audio, Camera |

> **이벤트(Event)**: 어떤 일이 일어났음을 다른 시스템에 알려주는 신호다. 예를 들어 알이 떨어지면 UI와 사운드가 동시에 반응할 수 있다.

---

## 6. MVP 필수 알고리즘

### 6-1. 드래그 발사 알고리즘

목적: 마우스를 뒤로 당겼다가 놓으면 알이 반대 방향으로 튕긴다.

```text
1. 마우스 클릭 시 시작 위치 저장
2. 마우스를 놓을 때 끝 위치 저장
3. 시작 위치 - 끝 위치 = 발사 방향
4. 벡터 길이 = 발사 힘
5. 최소/최대 힘으로 Clamp
6. Rigidbody.AddForce(..., ForceMode.Impulse)
```

> **Clamp**: 값이 너무 작거나 커지지 않도록 최소/최대 범위 안에 가두는 처리다.

### 6-2. 정지 판정 알고리즘

목적: 알이 거의 멈췄을 때 턴을 종료한다.

```text
1. 모든 생존 알의 속도를 확인
2. 모든 알 속도가 stopVelocity 이하인지 확인
3. 그 상태가 stopHoldTime 이상 유지되면 정지로 인정
4. OnAllEggsStopped 이벤트 발생
```

권장 초기값:

| 값 | 권장 |
|---|---|
| `stopVelocity` | 0.08 |
| `stopHoldTime` | 1.0초 |
| `maxResolveTime` | 8~10초 |

### 6-3. 강제 턴 종료 알고리즘

목적: 알이 미세하게 계속 움직여 턴이 끝나지 않는 문제를 막는다.

```text
1. 발사 직후 resolveTimer 시작
2. maxResolveTime을 넘으면 모든 알 속도 0 처리
3. 턴 종료
```

### 6-4. 낙하 판정 알고리즘

목적: 보드 밖으로 떨어진 알을 제거한다.

구현 방식:

- 보드 아래 또는 외곽에 큰 Trigger Collider를 배치한다.
- 알이 Trigger에 닿으면 `EggController.MarkFallen()` 호출.
- 해당 알은 비활성화하거나 낙하 연출 후 제거한다.

> **Trigger Collider**: 물체를 물리적으로 막지 않고, 들어왔는지만 감지하는 충돌 영역이다.

### 6-5. 승패 판정 알고리즘

```text
1. 각 플레이어의 생존 알 수 계산
2. P1 생존 알이 0이면 P2 승리
3. P2 생존 알이 0이면 P1 승리
4. 아니면 다음 턴 진행
```

### 6-6. 폭발물 알고리즘(v0.2)

```text
1. 폭발물 생성
2. 카운트다운 표시
3. 폭발 시 Physics.OverlapSphere로 주변 알 탐색
4. 거리 비례 힘 계산
5. 각 알 Rigidbody에 AddForce 적용
```

> **OverlapSphere**: 특정 반경 안에 있는 Collider를 찾아주는 Unity 물리 함수다.

---

## 7. Unity 내장/공식 기능 사용 계획

| 영역 | 사용할 기능 | 사용 이유 |
|---|---|---|
| 물리 | `Rigidbody`, `SphereCollider` | 직접 물리 계산을 줄인다. |
| 마찰/탄성 | `Physic Material` | Inspector에서 튜닝 가능하다. |
| 낙하 | Trigger Collider | 좌표 계산보다 단순하고 안정적이다. |
| 입력 | Unity Input System | 마우스/터치 확장에 유리하다. |
| UI | UGUI, Canvas, Button, Slider | AI가 잘 만들고 팀원이 이해하기 쉽다. |
| 텍스트 | TextMeshPro | 글자가 선명하고 UI 품질이 좋다. |
| 카메라 | Cinemachine | 탑다운/추적/블렌딩을 쉽게 만든다. |
| 셰이크 | Cinemachine Impulse | 충돌/폭발 흔들림을 직접 구현하지 않아도 된다. |
| 사운드 | AudioSource, AudioMixer | BGM/SFX/볼륨을 관리한다. |
| 이펙트 | Particle System | 흙먼지, 폭발, 낙하 연출에 적합하다. |
| 데이터 | ScriptableObject | 수치를 코드 수정 없이 조절한다. |
| 저장 | PlayerPrefs | 볼륨 설정 저장에 사용한다. |
| NPC | NavMesh | v0.3 이후 공룡 NPC 이동에 사용한다. |

---

## 8. 오픈소스/무료 에셋 사용 계획

| 이름 | 용도 | 라이선스/주의 |
|---|---|---|
| Mirror | LAN 멀티 | MIT. v0.4 이후 권장. |
| LitMotion | 트윈 애니메이션 | MIT. DOTween 대체 후보. |
| DOTween Free | 트윈 애니메이션 | 상업 사용 가능하나 MIT가 아닌 별도 라이선스. 사용 시 라이선스 확인. |
| Quaternius | 공룡 3D 에셋 | CC0 에셋 우선 사용. |
| Kenney | UI/3D 에셋 | CC0. 출처 기록 권장. |
| Google Fonts | 폰트 | SIL Open Font License. |
| Freesound | 효과음 | CC0/CC-BY만 사용. CC-BY는 크레딧 필수. |
| Game-icons.net | 아이콘 | CC-BY. 크레딧 필수. |
| Pixabay | 음악/효과음 | 약관 확인 후 사용. 출처 기록 필수. |

사용 금지:

- CC-BY-NC
- CC-BY-ND
- GPL 코드/에셋
- 라이선스가 불명확한 무료 에셋
- 유명 IP, 팬아트, 특정 상업 게임을 닮은 에셋

> **CC0**: 사실상 저작권 포기에 가까운 라이선스다. 상업 사용과 수정이 자유로운 편이다.  
> **CC-BY**: 사용은 가능하지만 저작자 표시가 필요하다.  
> **NC**: Non-Commercial, 상업적 사용 금지다. 사용하지 않는다.  
> **ND**: No Derivatives, 수정 금지다. 게임 에셋에 부적합하다.

---

## 9. 아트/프리팹 제작 파이프라인

### 9-1. 제작 우선순위

1. Unity 기본 도형으로 먼저 기능 구현
2. Blender MCP로 간단한 자체 모델 제작
3. 무료/오픈 라이선스 에셋 사용
4. 3D 생성형 AI로 보조 제작

### 9-2. Blender MCP로 만들 대상

- 기본 공룡알
- 보드 베이스
- 둥지
- 돌, 뼈, 나무토막
- 진영 표시 디스크
- 폭발물
- 단순 UI 장식

### 9-3. 무료 에셋 우선 대상

- 리깅된 공룡 모델
- 공룡 애니메이션
- 복잡한 자연 프롭
- UI 팩
- 사운드

### 9-4. 3D 생성형 AI 사용 규칙

- 유명 IP 이름 사용 금지.
- 특정 작가 스타일 사용 금지.
- 생성 결과는 Blender에서 후처리한다.
- 사용한 툴, 프롬프트, 날짜를 기록한다.
- 상업 사용 약관을 확인한다.

---

## 10. 확장 모듈 설계

모든 확장 기능은 `FeatureFlags`로 켜고 끌 수 있게 만든다.

예시:

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

## 11. Vibe Coding 공통 규칙

전원이 다른 AI 모델을 사용해도 결과물이 맞물리도록 아래 규칙을 지킨다.

### 11-1. 공통 프롬프트

```text
Unity 6.3 LTS, C# 기준입니다.
이 프로젝트는 2인 핫시트 3D 공룡 알까기입니다.
직접 물리 엔진을 만들지 말고 Rigidbody, Collider, Physic Material을 사용해 주세요.
기존 클래스명은 GameSessionController, TurnController, EggController, FlickInputController, MotionResolver입니다.
새 기능은 기본 알까기 코어를 망가뜨리지 않아야 하며, 가능하면 Inspector에서 켜고 끌 수 있게 만들어 주세요.
복잡한 새 아키텍처를 만들지 말고 현재 구조에 맞춰 작게 구현해 주세요.
```

### 11-2. AI 작업 요청 방식

좋은 요청:

- "`MotionResolver`가 모든 알의 속도를 보고 턴 종료 이벤트를 발생시키게 해줘."
- "`BoardFallZone` Trigger에 알이 닿으면 해당 알을 fallen 처리하게 해줘."
- "`AudioManager`가 충돌 강도에 따라 볼륨을 다르게 재생하게 해줘."

나쁜 요청:

- "알까기 게임 전체 코드를 짜줘."
- "네트워크까지 포함해서 완성해줘."
- "멋진 구조로 리팩토링해줘."

### 11-3. AI 코드 리뷰 규칙

AI가 코드를 만들면 다른 AI에게 아래 질문으로 다시 검토한다.

```text
이 Unity C# 코드에서 null reference, 턴이 끝나지 않는 문제, 이벤트 중복 호출,
Rigidbody 물리 처리 문제, Inspector 연결 누락 가능성이 있는지 리뷰해 주세요.
```

---

## 12. 테스트 기준

### 12-1. 매일 확인할 테스트

- 프로젝트가 실행되는가?
- Basic Core 한 판이 끝나는가?
- 턴이 멈추지 않고 넘어가는가?
- 알이 보드 밖으로 떨어지면 제거되는가?
- 한 플레이어의 알이 0개가 되면 결과 화면이 나오는가?
- 새 기능을 꺼도 게임이 돌아가는가?

### 12-2. 발표 빌드 기준

- 새 기능 추가 금지: Day 12 이후
- 발표 빌드 고정: Day 13
- Day 14에는 버그 수정만 허용

---

## 13. 참고 링크

- Unity Asset Store 상업 사용 안내: https://support.unity.com/hc/en-us/articles/205623589-Can-I-use-assets-from-the-Asset-Store-in-my-commercial-game-
- Blender 라이선스 안내: https://docs.blender.org/manual/en/latest/getting_started/about/license.html
- Mirror GitHub: https://github.com/MirrorNetworking/Mirror
- LitMotion GitHub: https://github.com/annulusgames/LitMotion
- DOTween License: https://dotween.demigiant.com/license.php
- Kenney Support: https://kenney.nl/support
- Quaternius Dinosaurs: https://quaternius.itch.io/animated-lowpoly-dinosaurs
- Google Fonts FAQ: https://developers.google.com/fonts/faq
- Game-icons FAQ: https://game-icons.net/faq.html

