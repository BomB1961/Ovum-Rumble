## ADDED Requirements

### Requirement: Spawn zone flattening
생성된 heightfield에서 P1/P2 스폰 영역은 평탄화되어야 한다(MUST).

#### Scenario: Spawn area is flat after generation
- **WHEN** 절차 지형이 생성된 후
- **THEN** P1/P2 스폰 반경 내 모든 셀의 높이 차이가 임계값(0.05) 이하여야 한다

#### Scenario: Spawn zones are at similar elevation
- **WHEN** 두 스폰 존의 높이를 비교하면
- **THEN** 높이 차이가 제한값 이내여야 한다 (공정성 보장)

### Requirement: Slope limitation
플레이 영역 내 경사가 너무 가파른 셀은 완화되어야 한다(SHALL).

#### Scenario: Steep slopes are clamped
- **WHEN** 인접 셀 간 높이 차이가 maxSlopeGradient를 초과하면
- **THEN** 높이가 제한값으로 조정되어야 한다

#### Scenario: Eggs do not slide on valid map
- **WHEN** 검증을 통과한 맵에서 알을 정지시키면
- **THEN** 경사로 인해 알이 자연스럽게 미끄러지지 않아야 한다

### Requirement: Map validation pass
생성된 맵은 gameplay에 적합한지 검증을 통과해야 한다(MUST).

#### Scenario: Validation rejects bad map
- **WHEN** 생성된 맵이 조건을 만족하지 않으면
- **THEN** 새로운 seed로 재생성을 시도해야 한다

#### Scenario: Validation accepts good map
- **WHEN** 모든 검증 조건을 통과하면
- **THEN** 해당 맵이 gameplay에 사용되어야 한다

#### Scenario: Max retry limit
- **WHEN** 최대 재시도 횟수를 초과하면
- **THEN** 마지막 생성된 맵을 사용하고 경고 로그를 출력해야 한다
