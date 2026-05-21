## Context

현재 Ovum Rumble의 보드는 평평한 고정 mesh로 구성되어 있다. EggSpawner, BoardFallZone, CameraController, FlickInputController 모두 y=0 평면과 고정 좌표를 기반으로 동작한다.

사용자는 플레이를 누를 때마다 펄린 노이즈로 새로운 지형을 생성하여 매 게임마다 다른 맵에서 플레이하기를 원한다.

제약사항:
- Unity 6.3 LTS, C#, URP
- 핫시트 2인 (Windows)
- 타겟 60fps
- 직접 물리 엔진 금지 — Rigidbody/Collider/Physic Material 사용
- Core는 프레임워크 의존성 최소화
- FeatureFlags로 확장 on/off

## Goals / Non-Goals

**Goals:**
- Mathf.PerlinNoise 기반 heightfield로 매 플레이마다 다른 지형 생성
- BoardSurface 인터페이스 추상화로 기존 시스템 최소 변경
- 스폰 존 평탄화 및 경사 제한으로 알까기 gameplay 안정성 보장
- FeatureFlags.enableProceduralMap 플래그로 기능 토글
- 기존 고정 보드 모드도 정상 동작 보장

**Non-Goals:**
- Simplex/OpenSimplex 알고리즘 도입 (향후 확장)
- 다중 맵 프리셋/저장 시스템
- 네트워크 동기화
- 씬 에디터 GUI 도구
- 배경/장식용 에셋 절차 생성

## Decisions

### 1. Mathf.PerlinNoise 선택

**결정**: Unity 내장 `Mathf.PerlinNoise` 사용

**이유**: 별도 라이브러리 없이 즉시 사용 가능. 향후 Simplex/OpenSimplex로 교체 가능하도록 BoardSurface 인터페이스로 추상화.

**대안 고려**:
- FastNoise/OpenSimplex 외부 라이브러리 → MVP 단계에서 오버엔지니어링
- ComputeShader 기반 GPU 노이즈 → 성능 이점 있지만 복잡도 과다

### 2. BoardSurface 인터페이스 + ProceduralBoardSurface 구현체

**결정**: `IBoardSurface` 인터페이스를 Core 계층에, `ProceduralBoardSurface`를 Environment 계층에 배치

**이유**: 기존 시스템이 구체 구현이 아닌 인터페이스에 의존. 고정 보드/절차 지형 모두 동일 인터페이스로 동작.

```
IBoardSurface (Core/Environment 경계)
├── ProceduralBoardSurface (Environment)
└── 향후: FixedBoardSurface, LoadedBoardSurface 등
```

### 3. 런타임 Mesh + MeshCollider

**결정**: heightfield float[,] 배열로부터 Mesh와 MeshCollider를 런타임에 생성

**이유**: 씬 파일에 프리팹을 미리 만들 필요 없음. seed만으로 재현 가능.

**구조**:
- heightfield 해상도: 64x64 또는 128x128 (Inspector 튜닝)
- Mesh 생성: Vertex/ Triangle/ UV 수동 구성
- MeshCollider: 생성된 Mesh 할당
- PhysicMaterial: GameSettings의 bounciness/friction 적용

### 4. 스폰 존 평탄화 알고리즘

**결정**: 스폰 반경 내 height를 중심값으로 평균화

**이유**: 알이 시작하자마자 굴러가는 문제 방지. P1/P2 공정성 보장.

**파라미터**:
- spawnRadius: 평탄화 반경 (기본 2.0)
- spawnHeight: 중앙 셀 높이 또는 평균 높이

### 5. Fall Zone 처리

**결정**: BoardSurface.IsInsidePlayableArea() + 기존 BoardFallZone 병행

**이유**: 기존 Box Trigger 방식은 유지하되, 절차 지형에서는 노이즈 기반 radial falloff로 외곽을 fall zone으로 처리. heightfield 경계 밖은 자동으로 낙하.

### 6. FeatureFlags.enableProceduralMap

**결정**: FeatureFlags에 `enableProceduralMap` bool 추가

**동작**:
- false (기본): 기존 고정 보드 그대로 사용
- true: ProceduralBoardGenerator가 런타임에 지형 생성

## Risks / Trade-offs

- **[성능] 런타임 Mesh 생성 비용** → 64x64 해상도로 제한, Awake/Start에서 한 번만 생성. 로딩 화면 필요 시 대응.
- **[Gameplay] 경사면에서 알이 멈추지 않음** → MotionResolver의 maxResolveTime이 최후 안전망. 맵 생성 시 slope 제한으로 사전 차단.
- **[호환성] 기존 씬 동작 보장** → enableProceduralMap=false면 아무것도 바뀌지 않음.
- **[시각] 텍스처 없는 raw mesh** → 초기에는 vertex color 또는 단일 머티얼. 시각 품질은 향후 개선.
