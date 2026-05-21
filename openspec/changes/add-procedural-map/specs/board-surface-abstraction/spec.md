## ADDED Requirements

### Requirement: IBoardSurface interface
Core 계층에 보드 표면 정보를 조회하는 인터페이스 IBoardSurface가 존재해야 한다(MUST).

#### Scenario: GetHeight returns terrain height at position
- **WHEN** IBoardSurface.GetHeight(xz)를 특정 x,z 좌표로 호출하면
- **THEN** 해당 위치의 지형 높이(y)를 반환해야 한다

#### Scenario: GetNormal returns surface normal
- **WHEN** IBoardSurface.GetNormal(xz)를 호출하면
- **THEN** 해당 위치의 표면 법선 벡터를 반환해야 한다

### Requirement: Spawn point queries
IBoardSurface는 플레이어별 스폰 포인트를 제공해야 한다(MUST).

#### Scenario: Player 1 spawn point
- **WHEN** GetSpawnPoints(1)을 호출하면
- **THEN** Player 1의 시작 위치 목록이 반환되어야 한다

#### Scenario: Player 2 spawn point
- **WHEN** GetSpawnPoints(2)을 호출하면
- **THEN** Player 2의 시작 위치 목록이 반환되어야 한다

### Requirement: Playable area and fall zone queries
IBoardSurface는 플레이 가능 영역과 낙하 영역을 판별할 수 있어야 한다(MUST).

#### Scenario: Inside playable area check
- **WHEN** IsInsidePlayableArea(position)을 플레이 영역 내 좌표로 호출하면
- **THEN** true를 반환해야 한다

#### Scenario: Outside playable area is fall zone
- **WHEN** IsInsidePlayableArea(position)을 보드 외곽 좌표로 호출하면
- **THEN** false를 반환해야 한다

### Requirement: Camera bounds query
IBoardSurface는 카메라 설정에 필요한 경계 정보를 제공해야 한다(SHALL).

#### Scenario: Camera bounds return center and size
- **WHEN** GetCameraBounds()를 호출하면
- **THEN** 보드 중심점, 크기, 최대 높이를 포함한 Bounds를 반환해야 한다

### Requirement: Existing systems consume BoardSurface
EggSpawner, BoardFallZone, CameraController, FlickInputController는 IBoardSurface를 통해 보드 정보를 조회해야 한다(SHALL).

#### Scenario: EggSpawner uses BoardSurface spawn points
- **WHEN** 절차 지형 모드에서 EggSpawner가 알을 생성하면
- **THEN** BoardSurface.GetSpawnPoints()에서 받은 위치를 사용해야 한다

#### Scenario: FlickInputController uses surface height
- **WHEN** 절차 지형 모드에서 드래그 입력을 처리하면
- **THEN** y=0 평면 대신 BoardSurface.GetHeight()로 보정된 표면 좌표를 사용해야 한다
