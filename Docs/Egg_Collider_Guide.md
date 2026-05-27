# 알 충돌 경고 정리

## 1. 현재 확인된 경고

게임에서 알이 생성될 때 아래 경고가 반복된다.

```text
Couldn't create a Convex Mesh from source mesh, within the maximum polygons limit (256).
The partial hull will be used.
```

현재 경고는 `egg-design`의 새 알 4종이 아니라, 실제 게임에서 스폰되는 기존 알에서 나온다.

```text
P1: Assets/_Project/Prefabs/Eggs/YoshiEgg.prefab
P2: Assets/_Project/Prefabs/Eggs/razorbill_egg.prefab
```

현재 구조:

```text
YoshiEgg.prefab       MeshCollider 6개
razorbill_egg.prefab MeshCollider 1개
```

## 2. 원인

알 외형이 화려해서 생긴 문제가 아니다.

현재 알은 단순한 알 형태에 얼룩이 있는 정도다.

문제는 **화면에 보이는 알 모델의 Mesh를 충돌 판정에도 그대로 쓰는 것**이다.

Unity가 이 Mesh를 충돌 판정용으로 처리하려고 하지만, 기준보다 복잡해서 일부만 사용한다.

그래서 실제 충돌 모양이 의도와 다르게 잡힐 수 있다.

## 3. 생길 수 있는 문제

- 알끼리 부딪힐 때 이상한 방향으로 튈 수 있다.
- 복잡한 MeshCollider 때문에 충돌 접점이 불안정해지면, 알이 서로 붙은 상태에서 계속 흔들릴 수 있다.
- 알이 미세하게 계속 움직여 턴이 안 끝날 수 있다.
- 낙하 판정이나 승패 판정 확인이 어려워질 수 있다.
- Console 경고가 많아져 다른 문제를 찾기 어렵다.

## 4. 해결 방안

화면용 모델과 충돌용 모양을 분리한다.

권장 구조:

```text
EggPrefab
  Root
    EggController
    Rigidbody
    단순한 충돌 판정
  Visual
    화면에 보이는 알 모델
```

`Visual`은 화면에 보이는 역할만 한다.

충돌 판정은 `Root`에 단순하게 둔다.

## 5. 추천 방식

### 1순위: 단순 충돌 모양 조합

`SphereCollider`, `CapsuleCollider`를 조합해서 알 형태를 대략 맞춘다.

장점:

- 경고가 줄어든다.
- 충돌이 더 안정적이다.
- 성능 부담이 적다.
- 튜닝하기 쉽다.

### 2순위: 충돌 전용 단순 Mesh 사용

MeshCollider가 꼭 필요하면 화면용 모델이 아니라 충돌 전용 단순 Mesh를 따로 만든다.

기준:

- 외곽만 알 형태로 단순하게 만든다.
- 세부 장식은 넣지 않는다.
- Unity가 처리하기 쉬운 낮은 복잡도로 만든다.

## 6. 같이 봐야 할 설정

충돌 모양을 바꾼 뒤 아래 값도 같이 확인한다.

- 알 시작 간격
- 알 감속값
- 알 튕김 정도
- 정지 판정 기준
- 턴 최대 대기 시간

## 7. 결론

현재 문제는 알 디자인 문제가 아니다.

문제는 **게임용 충돌 판정에 복잡한 MeshCollider를 쓰는 것**이다.

해결 방향:

```text
화면용 모델은 그대로 둔다
충돌 판정은 단순하게 만든다
턴 종료 안전장치를 추가한다
```

## 8. 작업 일지

### 2026-05-27

- 대상: `Assets/_Project/Prefabs/Eggs/Egg.prefab`, `YoshiEgg.prefab`, `razorbill_egg.prefab`
- 조치: 기존 알 프리팹의 `MeshCollider`를 제거하고, `CapsuleCollider`와 `SphereCollider` 조합으로 교체했다.
- 배치 기준: `EggSpawner`가 실제 생성하는 자세(`Quaternion.identity`)에서 Renderer bounds를 확인한 뒤, 시각 모델보다 살짝 안쪽으로 충돌 bounds를 맞췄다.
- 체감 기준: 허공 충돌보다 알끼리 파고드는 느낌을 줄이는 쪽을 우선하되, 복잡한 Mesh 충돌 경고가 재발하지 않도록 단순 Collider만 사용했다.
- 검증:
  - `Egg.prefab`: `MeshCollider=0`, `SphereCollider=2`, `CapsuleCollider=1`
  - `YoshiEgg.prefab`: `MeshCollider=0`, `SphereCollider=2`, `CapsuleCollider=1`
  - `razorbill_egg.prefab`: `MeshCollider=0`, `SphereCollider=2`, `CapsuleCollider=1`
  - `01_Game.unity`의 `EggSpawner`가 참조하는 P1/P2 프리팹도 같은 결과임을 Unity Editor에서 확인했다.

---

# Skybox 경고 정리

## 1. 현재 확인된 경고

빌드 중 Skybox 관련 Shader 경고가 나온다.

대표 파일:

```text
Assets/_Project/Art/Skybox/Shaders/GenshinSky.shader
Assets/_Project/Art/Skybox/Shaders/GenshinCloud.shader
```

대표 경고:

```text
implicit truncation of vector type
pow(f, e) will not work for negative f
```

## 2. 원인

하늘과 구름을 그리는 계산식 일부가 Unity에서 경고를 발생시킨다.

현재 빌드를 실패시키는 문제는 아니다.

## 3. 생길 수 있는 문제

- 특정 환경에서 하늘 색이 이상하게 보일 수 있다.
- 구름 효과가 일부 깨질 수 있다.
- 빌드 로그에 경고가 쌓인다.

## 4. 해결 방안

우선순위는 낮다.

먼저 턴, 승패, 알 충돌 문제를 고친다.

그 뒤 아래 중 하나를 선택한다.

```text
1. 현재 Skybox Shader 계산식을 정리한다
2. MVP용 단순 Skybox로 교체한다
```

## 5. 결론

Skybox 경고는 현재 빌드 실패 원인은 아니다.

하지만 발표 전에는 정리하는 것이 좋다.
