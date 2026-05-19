## ADDED Requirements

### Requirement: Camera Orbit
시스템은 우클릭 드래그 입력으로 카메라가 pivot 지점을 중심으로 회전하도록 지원해야 한다.

#### Scenario: Right-drag rotates camera around pivot
- **WHEN** 플레이어가 우클릭을 누른 상태에서 마우스를 수평으로 드래그
- **THEN** 카메라는 yaw 축(World Y)으로 회전하고, pivot을 계속 바라본다

#### Scenario: Right-drag changes pitch
- **WHEN** 플레이어가 우클릭을 누른 상태에서 마우스를 수직으로 드래그
- **THEN** 카메라 피치가 변경된다. 피치는 -89° ~ 89° 범위로 제한된다

#### Scenario: Orbit sensitivity is respected
- **WHEN** orbitSensitivity가 200으로 설정되어 있을 때 마우스를 100px 수평 이동
- **THEN** 회전량은 sensitivity에 비례하여 적용된다

### Requirement: Camera Pan (Gimbal)
시스템은 휠클릭 드래그 입력으로 pivot과 카메라가 보드 평면(Y=0)을 따라 함께 이동하도록 지원해야 한다.

#### Scenario: Middle-drag pans camera on board plane
- **WHEN** 플레이어가 휠클릭을 누른 상태에서 마우스를 드래그
- **THEN** 카메라와 pivot이 보드 평면(Y=0) 상에서 마우스 움직임과 동일한 방향으로 이동한다

#### Scenario: Pan grabs the board point under cursor
- **WHEN** 플레이어가 휠클릭을 누르는 순간
- **THEN** 시스템은 ScreenPointToRay로 Y=0 평면 교차점을 계산하여 pan 앵커로 저장한다

#### Scenario: Pan delta is computed from raycast
- **WHEN** 플레이어가 휠클릭을 누른 상태에서 드래그
- **THEN** 매 프레임마다 Y=0 평면과의 raycast 교차점 delta만큼 pivot이 이동한다

### Requirement: Camera Dolly (Zoom)
시스템은 마우스 휠 스크롤 입력으로 카메라와 pivot 간 거리를 조절하도록 지원해야 한다.

#### Scenario: Scroll up zooms in
- **WHEN** 플레이어가 마우스 휠을 위로 스크롤
- **THEN** 카메라가 pivot에 가까워진다 (distance 감소)

#### Scenario: Scroll down zooms out
- **WHEN** 플레이어가 마우스 휠을 아래로 스크롤
- **THEN** 카메라가 pivot에서 멀어진다 (distance 증가)

#### Scenario: Distance is clamped
- **WHEN** 카메라 distance가 minDistance 이하 또는 maxDistance 이상에 도달
- **THEN** 더 이상 줌인/줌아웃되지 않는다

### Requirement: Input Compatibility
시스템은 기존 FlickInputController(좌클릭)와 입력 충돌 없이 공존해야 한다.

#### Scenario: Right-click does not interfere with left-click flick
- **WHEN** 플레이어가 우클릭이나 휠클릭을 사용
- **THEN** FlickInputController의 알 발사(좌클릭) 입력이 방해받지 않는다

#### Scenario: Camera controls work during all game states
- **WHEN** 게임이 Setup, Aiming, Resolving, CheckingResult, Result, Paused 중 어떤 상태이든
- **THEN** 카메라 컨트롤은 항상 동작한다

### Requirement: Per-Scene Wiring
시스템은 각 씬의 Main Camera에 CameraController를 부착하여 동작해야 한다.

#### Scenario: CameraController attached to Main Camera
- **WHEN** 씬이 로드됨
- **THEN** Main Camera에 부착된 CameraController가 활성화되어 마우스 입력을 감지한다

#### Scenario: CameraController falls back to Camera.main
- **WHEN** inputCamera 필드가 null인 경우
- **THEN** CameraController는 Camera.main을 사용한다
