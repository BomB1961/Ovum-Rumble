# WORKLOG

## 2026-05-19

- Person A MVP의 기본 폴더 구조와 `EggController` 구현을 완료했다. Unity 컴파일, 콘솔 에러/워닝, 타입 로드 검증을 통과했다.
- `EggSpawner` 구현을 완료했다. P1/P2 알 생성, 2x3 배치, 중복 생성 정리, 생성 목록 제공을 추가하고 Unity 컴파일 및 콘솔 검증을 통과했다.
- Person A 작업 운영 워크플로우를 `Docs/PersonA_MVP_Implementation_Plan.md`에 보강했다. 기능 단위 구현, Codex Unity 검증, 사용자 테스트 승인, 커밋/푸시, main 병합, 작업일지 갱신 루프를 명시했다.
- `FlickInputController`를 구현했다. 현재 플레이어의 살아있는 알 선택, 드래그 반대 방향 발사, 힘 제한, 입력 활성화/플레이어 전환 API, 선택/발사 이벤트를 추가했다.
- `Egg.prefab`과 `Egg_PhysicMaterial.physicMaterial`을 구성했다. Rigidbody, SphereCollider, EggController, 물리 마찰/탄성 기본값 연결을 Unity에서 검증했다.
- `A_EggPhysics_Prototype.unity` 테스트 Scene을 조립했다. Camera, Light, 임시 보드, EggSpawner, FlickInputController, Player Root를 배치하고 Play Mode에서 P1/P2 알 6개씩 총 12개 생성을 확인했다.
- Unity 컴파일, Console Error 확인, Play Mode 생성 검증을 통과했다. 사용자 Unity Editor 시각 테스트에서 선택, 드래그 발사, 충돌 동작에 문제가 없음을 승인받았다.
