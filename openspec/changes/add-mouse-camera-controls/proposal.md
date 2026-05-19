## Why

현재 프로젝트의 카메라는 (0, 7.5, -7.5) 위치에 55도 피치로 고정된 static 오브젝트입니다. 플레이어가 보드를 여러 각도에서 관찰하거나, 특정 영역을 확대해 전략을 세울 수 있는 수단이 전혀 없습니다. 핫시트 턴제 보드게임에서 플레이어가 자유롭게 시점을 조작할 수 있는 카메라 컨트롤은 기본적인 UX 요구사항입니다.

## What Changes

- `CameraController.cs` 신규 추가: 마우스 입력으로 카메라를 조작하는 MonoBehaviour
- 우클릭 + 드래그: 카메라 위치 고정, pivot 중심 회전 (Orbit/Look)
- 휠클릭 + 드래그: 카메라 평면 이동 (Pan), 시점 고정
- 휠 스크롤: 전진/후진 줌 (Dolly)
- 기존 `FlickInputController`(좌클릭 알 발사)와 입력 충돌 없이 공존
- `GameState` 제약 없이 항상 활성화 (Aiming, Resolving 등 모든 상태에서 사용 가능)

## Capabilities

### New Capabilities
- `camera-controls`: 마우스 기반 카메라 오빗/팬/돌리 컨트롤 시스템

### Modified Capabilities
<!-- 해당 없음 -->

## Impact

- `Assets/_Project/Scripts/Presentation/CameraController.cs` 신규 파일
- `Assets/_Project/Scenes/A_EggPhysics_Prototype.unity`, `Assets/_Project/Scenes/B_EggPhysics_Prototype.unity` — Main Camera에 CameraController 컴포넌트 추가
- 기존 코드 변경 없음 (FlickInputController, GameSessionController 등 영향 없음)
