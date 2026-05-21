## ADDED Requirements

### Requirement: Perlin noise heightfield generation
시스템은 seed값 기반으로 `Mathf.PerlinNoise`를 사용하여 2D heightfield(float[,])를 생성해야 한다(MUST).

#### Scenario: Different seeds produce different maps
- **WHEN** 서로 다른 seed값으로 heightfield를 생성하면
- **THEN** 두 heightfield는 서로 다른 높이 분포를 가져야 한다

#### Scenario: Same seed produces identical map
- **WHEN** 동일한 seed값으로 heightfield를 두 번 생성하면
- **THEN** 두 heightfield는 바이트 단위로 동일해야 한다

### Requirement: Runtime mesh generation from heightfield
시스템은 heightfield 데이터로부터 Unity Mesh와 MeshCollider를 런타임에 생성해야 한다(MUST).

#### Scenario: Mesh is created with correct vertex count
- **WHEN** NxN 해상도의 heightfield로 mesh를 생성하면
- **THEN** 생성된 Mesh의 vertex 수는 N*N이어야 한다

#### Scenario: MeshCollider is assigned
- **WHEN** mesh 생성이 완료되면
- **THEN** 동일 GameObject의 MeshCollider에 생성된 mesh가 할당되어야 한다

### Requirement: Seed randomization on play
시스템은 게임 시작 시(플레이 버튼)마다 새로운 무작위 seed를 생성해야 한다(MUST).

#### Scenario: New seed on each play session
- **WHEN** 게임이 시작되면(Start/Awake)
- **THEN** 이전 세션과 다른 seed값이 사용되어야 한다

#### Scenario: Manual seed override
- **WHEN** Inspector에 특정 seed값이 입력되어 있으면
- **THEN** 해당 seed값이 사용되어야 한다

### Requirement: Configurable noise parameters
노이즈 생성 파라미터는 Inspector를 통해 조절 가능해야 한다(MUST).

#### Scenario: Scale parameter affects terrain detail
- **WHEN** noiseScale 값을 변경하면
- **THEN** 지형의 세밀함/거칠기가 그에 맞게 변화해야 한다

#### Scenario: Height amplitude is adjustable
- **WHEN** heightMultiplier 값을 변경하면
- **THEN** 지형의 최대 높이가 그에 맞게 변화해야 한다

### Requirement: FeatureFlag toggle
Procedural map generation은 FeatureFlags.enableProceduralMap으로 켜고 끌 수 있어야 한다(MUST).

#### Scenario: Disabled flag skips generation
- **WHEN** enableProceduralMap이 false이면
- **THEN** 기존 고정 보드가 그대로 사용되며 절차 생성 코드가 실행되지 않아야 한다

#### Scenario: Enabled flag triggers generation
- **WHEN** enableProceduralMap이 true이면
- **THEN** 게임 시작 시 절차 지형이 생성되어야 한다
