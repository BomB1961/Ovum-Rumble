## Context

현재 카메라는 Unity 씬에 배치된 static GameObject로, `(0, 7.5, -7.5)` 위치에서 55도 피치로 보드를 바라보고 있습니다. 어떤 스크립트도 부착되어 있지 않으며, 런타임 중 시점 변경이 불가능합니다. `FlickInputController`가 왼쪽 마우스 버튼을 사용하여 알 발사 입력을 처리하고 있습니다.

Presentation 레이어(`Assets/_Project/Scripts/Presentation/`)는 생성되어 있으나 비어 있는 상태입니다. 아키텍처 원칙(Core → Rules → Presentation 단방향 의존)에 따라 카메라 컨트롤은 Presentation 레이어에 위치합니다.

## Goals / Non-Goals

**Goals:**
- 마우스 우클릭+드래그로 pivot 중심 오빗 회전 구현
- 마우스 휠클릭+드래그로 보드 평면 팬 이동 구현
- 마우스 휠 스크롤로 카메라 줌인/줌아웃(Dolly) 구현
- 기존 FlickInputController, GameSessionController와 충돌 없이 공존
- GameState에 관계없이 항상 동작

**Non-Goals:**
- 키보드 단축키 (R키 리셋 등) — 추후 확장
- Cinemachine 도입 — 현재 스코프에서는 과도한 의존성
- 멀티 카메라 / 분할 화면
- 카메라 애니메이션 / 이징
- 모바일 터치 입력

## Decisions

### 1. 카메라 제어 방식: Spherical Coordinate 기반 Pivot Orbit

Unity Editor 스타일의 직관적인 조작을 위해, pivot 지점을 중심으로 하는 구면 좌표계 방식을 채택합니다.

**선택한 방식:**
카메라는 pivot으로부터 distance만큼 떨어진 위치에서 spherical coordinates (yaw, pitch, radius)로 계산되며, 항상 `LookAt(pivot)`을 유지합니다.

**대안 및 기각 사유:**
- **(A) Rigidbody 물리 기반**: 불필요한 물리 연산, 게임 물리와 간섭 가능성 — 기각
- **(B) Transform parent-child rig**: Pitch/Yaw를 분리한 게임오브젝트 계층 구조. gimbal lock 회피에 유리하나, 구조가 복잡해지고 pivot 이동 시 부모-자식 관계 관리가 번거로움 — 기각
- **(C) Cinemachine FreeLook/Virtual Camera**: 강력하지만 패키지 의존성 추가, 현재 단순한 요구사항에 과도함 — 기각

### 2. Pan 방식: Gimbal Pan (평면 레이캐스트 기반)

Pan 시 pivot을 보드 평면(Y=0)에서 raycasting으로 계산하여 이동시킵니다.

**선택한 방식:**
- Middle-click 다운 시점에 `ScreenPointToRay` → Y=0 평면 교차점 저장
- 드래그 중 raycast로 새 교차점 계산 → delta만큼 pivot 이동
- Google Maps 드래그 감각과 동일 — 커서가 보드를 "잡아서" 움직임

**대안 및 기각 사유:**
- **(A) camera.right / camera.forward 기반 변환**: 마우스 델타를 단순히 카메라 로컬 축으로 변환. 피치가 55도일 때 forward 성분이 Y축으로 치우쳐 XY 평면 이동이 부자연스러움 — 기각

### 3. 입력 감지: Unity Legacy Input + InputSystem 양립

기존 `FlickInputController`와 동일한 패턴을 따라 `Input.GetMouseButton()` (Legacy)와 `#if ENABLE_INPUT_SYSTEM` (New Input) 양쪽을 지원합니다.

### 4. Dolly 제한

distance는 `[minDistance, maxDistance]`로 clamp됩니다. 기본값: min=3, max=20 (직렬화 가능).

### 5. Pitch 제한

피치는 -89° ~ 89°로 clamp하여 카메라가 뒤집히는(gimbal flip) 현상을 방지합니다.

### 6. 회전 속도 및 확대 속도

모든 감도 값은 직렬화 필드로 노출되어 Unity Editor에서 조정 가능합니다.
- orbitSensitivity: 200 (degrees/sec normalized)
- zoomSensitivity: 5 (distance units per scroll tick)
- Default distance: 10 (초기 pivot으로부터의 거리)

## Risks / Trade-offs

- **[Risk] Orbit 도중 pivot이 화면에서 멀어질 경우 어색한 움직임** → Mitigation: distance clamp로 pivot이 과도하게 멀어지는 것 방지
- **[Risk] Scene View 의존성** → CameraController는 Camera.main을 fallback으로 사용하며, 컴포넌트가 없으면 아무 동작도 하지 않음 (safe failure)
- **[Risk] Input System 충돌** → Legacy와 New Input을 동일 파일에서 조건부 컴파일로 처리 (FlickInputController 패턴 준용)
