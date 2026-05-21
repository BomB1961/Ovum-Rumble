## 1. 데이터 계층 및 인터페이스

- [x] 1.1 FeatureFlags에 `enableProceduralMap` bool 필드 추가 (v0.2 환경 모듈 섹션)
- [x] 1.2 GameSettings에 절차 맵 생성 파라미터 추가 (resolution, noiseScale, heightMultiplier, spawnRadius, maxSlopeGradient, maxRetryCount)
- [x] 1.3 `IBoardSurface` 인터페이스 생성 (Assets/_Project/Scripts/Environment/IBoardSurface.cs) — GetHeight, GetNormal, IsInsidePlayableArea, GetSpawnPoints, GetCameraBounds

## 2. 핵심 생성 로직

- [x] 2.1 `HeightfieldGenerator` 클래스 생성 (Environment) — Mathf.PerlinNoise 기반 float[,] 생성, seed 입력/랜덤 seed 지원
- [x] 2.2 `MeshFromHeightfield` 유틸리티 생성 — float[,] → Unity Mesh 변환 (vertex, triangle, UV, 법선 계산)
- [x] 2.3 `ProceduralBoardGenerator` MonoBehaviour 생성 — HeightfieldGenerator + MeshFromHeightfield 통합, MeshCollider 할당, PhysicMaterial 적용

## 3. 맵 검증

- [x] 3.1 스폰 존 평탄화 로직 구현 — P1/P2 시작 반경 내 height 평균화
- [x] 3.2 경사 제한 로직 구현 — 인접 셀 간 높이 차이 maxSlopeGradient로 클램핑
- [x] 3.3 `MapValidator` 클래스 구현 — 평탄화/경사검증/스폰공정성 검증, 실패 시 재시도 (maxRetryCount 제한)

## 4. ProceduralBoardSurface 구현

- [x] 4.1 `ProceduralBoardSurface` 클래스 생성 — IBoardSurface 구현체, heightfield 기반 GetHeight/GetNormal/IsInsidePlayableArea/GetSpawnPoints/GetCameraBounds

## 5. 기존 시스템 통합

- [x] 5.1 EggSpawner 수정 — IBoardSurface가 있으면 GetSpawnPoints() 사용, 없으면 기존 고정 좌표 사용
- [x] 5.2 FlickInputController 수정 — IBoardSurface가 있으면 GetHeight()로 표면 좌표 보정, 없으면 기존 y=0 Plane
- [x] 5.3 CameraController 수정 — IBoardSurface가 있으면 GetCameraBounds()로 pivot/distance 초기값 조정
- [x] 5.4 BoardFallZone 수정 — IBoardSurface가 있으면 IsInsidePlayableArea() 기반 낙하 판정 추가 지원

## 6. 연동 및 테스트

- [x] 6.1 ProceduralBoardGenerator가 게임 시작 시 자동으로 지형 생성 + ProceduralBoardSurface를 다른 시스템에 제공하도록 연결
- [x] 6.2 enableProceduralMap=false일 때 기존 고정 보드 게임이 정상 동작 확인
- [x] 6.3 enableProceduralMap=true일 때 절차 지형 게임이 정상 동작 확인 (스폰, 플릭, 낙하, 카메라)
