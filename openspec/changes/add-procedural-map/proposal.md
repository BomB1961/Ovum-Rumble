## Why

현재 게임은 고정된 평면 보드에서만 플레이된다. 플레이를 누를 때마다 다른 지형이 생성되면 재플레이성과 전략적 깊이가 크게 향상된다. 펄린 노이즈 기반 절차 생성으로 매 게임마다 새로운 맵을 제공하여 핵심 알까기 gameplay를 유지하면서 변화를 준다.

## What Changes

- Mathf.PerlinNoise 기반 heightfield 생성기 추가 — seed값으로 매 플레이마다 다른 지형 생성
- BoardSurface 추상화 계층 도입 — 기존 시스템(BoardFallZone, EggSpawner, CameraController, FlickInputController)이 고정 보드/절차 지형 공통 인터페이스로 동작
- 런타임 Mesh 생성 — heightfield 데이터로부터 지형 Mesh + MeshCollider 생성
- Spawn zone 평탄화 — P1/P2 시작 위치 주변은 강제로 평평하게 생성
- Fall zone mask — 보드 외곽/절벽을 낙하 영역으로 처리
- FeatureFlags.enableProceduralMap 플래그로 on/off 전환 가능 (기본 off)

## Capabilities

### New Capabilities
- `procedural-board-generation`: 펄린 노이즈 기반 heightfield 생성, 런타임 mesh 생성, seed 관리
- `board-surface-abstraction`: BoardSurface 인터페이스로 지형 높이/법선/낙하영역/스폰포인트/카메라bounds 조회
- `map-validation`: 생성된 맵의 gameplay 적합성 검증 (경사 제한, 스폰 평탄화, fall zone)

### Modified Capabilities
<!-- 기존 스펙 없음 — 신규 프로젝트 -->

## Impact

- **Core**: EggSpawner — 고정 좌표 대신 BoardSurface.GetSpawnPoints() 사용
- **Rules**: BoardFallZone — Box Trigger 대신 BoardSurface.IsFallArea() 기반 낙하 판정 추가 (기존 방식도 유지)
- **Presentation**: CameraController — BoardSurface.GetCameraBounds()로 카메라 기본값 조정
- **Input**: FlickInputController — y=0 Plane 대신 BoardSurface 기반 표면 좌표 보정
- **Data**: FeatureFlags에 enableProceduralMap 추가, GameSettings에 맵 생성 파라미터 추가
- **Environment**: 새 ProceduralBoardGenerator MonoBehaviour + BoardSurface 인터페이스/클래스
