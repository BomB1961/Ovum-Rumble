# WORKLOG

## 2026-05-19

- Person A MVP의 기본 폴더 구조와 `EggController` 구현을 완료했다. Unity 컴파일, 콘솔 에러/워닝, 타입 로드 검증을 통과했다.
- `EggSpawner` 구현을 완료했다. P1/P2 알 생성, 2x3 배치, 중복 생성 정리, 생성 목록 제공을 추가하고 Unity 컴파일 및 콘솔 검증을 통과했다.
- Person A 작업 운영 워크플로우를 `Docs/PersonA_MVP_Implementation_Plan.md`에 보강했다. 기능 단위 구현, Codex Unity 검증, 사용자 테스트 승인, 커밋/푸시, main 병합, 작업일지 갱신 루프를 명시했다.
- `FlickInputController`를 구현했다. 현재 플레이어의 살아있는 알 선택, 드래그 반대 방향 발사, 힘 제한, 입력 활성화/플레이어 전환 API, 선택/발사 이벤트를 추가했다.
- `Egg.prefab`과 `Egg_PhysicMaterial.physicMaterial`을 구성했다. Rigidbody, SphereCollider, EggController, 물리 마찰/탄성 기본값 연결을 Unity에서 검증했다.
- `A_EggPhysics_Prototype.unity` 테스트 Scene을 조립했다. Camera, Light, 임시 보드, EggSpawner, FlickInputController, Player Root를 배치하고 Play Mode에서 P1/P2 알 6개씩 총 12개 생성을 확인했다.
- Unity 컴파일, Console Error 확인, Play Mode 생성 검증을 통과했다. 사용자 Unity Editor 시각 테스트에서 선택, 드래그 발사, 충돌 동작에 문제가 없음을 승인받았다.
- 마감 품질 보강으로 `Egg` Layer를 추가하고 `Egg.prefab`과 `FlickInputController.eggLayerMask`를 해당 Layer로 제한했다. Unity 컴파일, Console Error 확인, Play Mode 알 12개 생성 및 Layer 적용 검증을 통과했다.

## 2026-05-19 (Person D)

- `AudioSettings` ScriptableObject를 구현했다. BGM/SFX 볼륨, 충돌 임팩트 스케일링, 풀 사이즈 등 오디오 설정을 Inspector에서 튜닝 가능하게 했다.
- `AudioManager`를 구현했다. GameEvents 구독으로 발사/충돌/낙하/승패/턴시작 SFX 재생, 충돌 강도에 따른 볼륨 스케일링, AudioSource 풀링, BGM 루프 재생, 랜덤 피치 변화를 추가했다. AudioClips은 Inspector 연결, 실제 오디오 파일은 나중에 추가 예정.
- `EffectController`를 구현했다. EggSpawner 참조로 각 EggController의 CollisionOccurred 이벤트를 구독하여 충돌 위치에 먼지 파티클 생성, GameEvents.OnEggFell로 낙하 파티클 생성, ParticleSystem 풀링, 충돌 강도에 따른 파티클 스케일링을 추가했다.
- `CameraController`에 Cinemachine Impulse 셰이크를 추가했다. GameEvents.OnEggCollision 구독으로 충돌 강도에 비례하는 카메라 흔들림, CinemachineImpulseSource 연결, 패키지 매니페스트에 Cinemachine 3.1.3 추가.
- `BombEventController` 스캐폴드를 작성했다. FeatureFlags.enableBomb으로 제어, 턴 카운트 기반 폭발물 스폰, 카운트다운 후 OverlapSphere 폭발, 거리 비례 힘 적용, 폭발 파티클/SFX 재생.
- [확인필요] Unity Editor에서 Cinemachine 패키지 resolve 후 컴파일 확인 필요. AudioMixer 에셋, CinemachineImpulseSource 컴포넌트, 파티클 프리팹은 Unity Editor에서 수동 연결 필요.
