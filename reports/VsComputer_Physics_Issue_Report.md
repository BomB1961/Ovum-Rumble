# VsComputer 테스트 — 물리 이슈 리포트

> 대상: Person A (Prefab/물리 담당)
> 작성: Person B (코드 담당)
> 날짜: 2026-05-26

---

## 개요

`02_VsComputer_Test.unity`에서 컴퓨터 대전 모드 테스트 중,
P1 발사 후 P2(AI) 턴이 시작되지 않는 현상 발생.

**원인: 알 Prefab의 Rigidbody/Physics Material 설정으로 인해 물리 폭주**

---

## 문제 1: razorbill_egg.prefab — Rigidbody 설정 매우 불량

**파일:** `Assets/_Project/Prefabs/Eggs/razorbill_egg.prefab`

### 현재 상태

| 항목 | 현재 값 | 문제점 |
|------|---------|--------|
| `LinearDamping` | **0** | 속도 감쇠가 전혀 없음 |
| `AngularDamping` | **0.05** | 회전 감쇠가 거의 없음 |
| `CollisionDetection` | **Discrete (0)** | 고속 충돌 시 터널링 발생 |
| `MeshCollider.m_Material` | **{fileID: 0}** (없음) | 물리 재질 미할당 |
| `Interpolate` | **None (0)** | 렌더링과 물리 불일치 |
| `MeshCollider.m_Convex` | **true** | 복잡한 convex mesh 충돌 불안정 |

### 현상

```
P1 발사 (force=2, 최소)
  → P1 알이 P2 알(razorbill)과 충돌
  → MeshCollider(Convex)끼리 충돌 → PhysX가 극단적 충격량 산출
  → Damping=0이라 에너지 소멸 안 됨
  → razorbill이 2000+ m/s로 폭주
  → 한 번 튀어나간 알이 영원히 안 멈춤
  → MotionResolver.CheckAllEggsStopped() 계속 false
  → OnAllEggsStopped 미발생
  → 게임 루프 정지
```

### 수정 방법 (정확한 값)

| 항목 | 수정 값 | 이유 |
|------|---------|------|
| `LinearDamping` | **0.5** | 적당한 속도 감쇠 (기존 Egg.prefab 기준) |
| `AngularDamping` | **1.0** | 회전 빠르게 정지 (angular→linear 재가속 차단) |
| `CollisionDetection` | **Continuous (2)** | 고속 충돌 터널링 방지 (Egg.prefab과 동일) |
| `MeshCollider.m_Material` | `{fileID: 13400000, guid: 46557bcdb3189ad47834b5971194ac35}` | **Egg_PhysicMaterial 할당** |
| `Interpolate` | **Interpolate (1)** | 물리-렌더링 동기화 (Egg.prefab과 동일) |

**참고:** `m_Material`의 guid는 `Egg_PhysicMaterial.physicMaterial`의 guid임.
Egg.prefab의 SphereCollider가 참조하는 것과 동일한 PhysicsMaterial을 MeshCollider에 할당하면 됨.

---

## 문제 2: YoshiEgg.prefab — MeshCollider 5개로 인한 충돌 불안정

**파일:** `Assets/_Project/Prefabs/Eggs/YoshiEgg.prefab`

### 현재 상태

커밋 `197ba21 Use mesh colliders for eggs`에서 SphereCollider를 MeshCollider(Convex)로 변경.
Yoshi 모델의 각 body part마다 총 **5개의 MeshCollider**가 개별 할당됨.

### 현상

- 복잡한 convex mesh 5개가 동시에 충돌 연산에 참여
- MeshCollider(Convex)는 원본 메시의 convex hull 근사치
- 여러 convex hull이 서로 맞물리면 PhysX가 극단적 충격량 산출
- 특히 `Time.timeScale = 2.5` (resolving 중)에서는 FixedUpdate 간격이 짧아져 불안정加剧

### 수정 방법 (택1)

**방안 A (권장): SphereCollider로 복원**
- 커밋 `197ba21` 이전 상태로 복원
- SphereCollider는 충돌 연산이 가장 안정적이고 빠름
- 알까기 게임에서 MeshCollider가 필수적인 이유가 없으면 이 방법이 가장 안전

**방안 B: MeshCollider 유지 시 개선**
- 각 MeshCollider에 `Egg_PhysicMaterial` 할당 확인 (현재는 5개 모두 할당되어 있긴 함)
- `CollisionDetection`을 `Continuous`로 변경
- `LinearDamping` 0.5, `AngularDamping` 1.0으로 상향
- 5개 MeshCollider를 가능하면 1개로 통합

---

## 참고: 테스트 재현 방법

1. Unity Editor에서 `Assets/Scenes/02_VsComputer_Test.unity` 열기
2. Play Mode 진입
3. VsComputerTestBootstrap이 자동으로 01_Game 로드
4. P1 알을 드래그해서 발사
5. 콘솔 로그 확인:
   - `[MotionResolver] Resolving started` → resolving 진입
   - 시간이 지나도 `[MotionResolver] All eggs stopped` 안 찍히면 문제
6. 알 속도가 0.08 이하로 안 내려가는지 확인

---

## 연관 파일 목록

| 파일 | 역할 |
|------|------|
| `Assets/_Project/Prefabs/Eggs/razorbill_egg.prefab` | P2(AI) 알 — **수정 필요** |
| `Assets/_Project/Prefabs/Eggs/YoshiEgg.prefab` | P1 알 — **수정 권장** |
| `Assets/_Project/Prefabs/Eggs/Egg.prefab` | 기본 알 — **현재 정상 (참조용)** |
| `Assets/_Project/Physics/Egg_PhysicMaterial.physicMaterial` | 알 물리 재질 |
| `Assets/_Project/Prefabs/Board/Board_Desert_Wrapper.prefab` | 데저트 맵 보드 |
| `Assets/_Project/Prefabs/Board/Board_Ice_Wrapper.prefab` | 아이스 맵 보드 |
