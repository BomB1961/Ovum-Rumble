# 공룡 알까기 게임 기획 (Game Plan)

> **한 줄 정의**: 공룡알을 드래그해서 튕기고, 상대 알을 보드 밖으로 밀어내는 3D 턴제 알까기 게임.

---

## 1. 프로젝트 개요

| 항목 | 내용 |
|---|---|
| 소재 | 공룡, 공룡알 |
| 장르 | 3D 턴제 물리 보드게임 |
| 기본 조작 | 드래그 후 놓기 (마우스) |
| 기본 승리 조건 | 상대 알을 모두 보드 밖으로 떨어뜨리면 승리 |
| 플레이 방식 | 2인 핫시트 (같은 PC에서 번갈아 조작) |
| 개발 방식 | 전원 vibe coding 활용 |
| 개발 전략 | 기본 알까기 코어 완성 후 기능을 모듈식으로 추가 |
| 1차 목표 | 2인 핫시트로 한 판이 안정적으로 끝나는 빌드 |

---

## 2. 가장 중요한 개발 원칙

### 2-1. 기본 알까기가 항상 먼저다

폭발물, 바람, 공룡 NPC, 능력 알, 네트워크가 없어도 게임은 반드시 돌아가야 한다.

모든 확장 기능은 다음 조건을 만족해야 한다.
- 꺼도 게임이 정상 작동한다.
- 실패해도 기본 알까기 코어를 망가뜨리지 않는다.
- 개발 중 언제든 발표 가능한 빌드를 유지한다.

### 2-2. 기능은 모듈로 붙인다

기본 코어 위에 기능을 하나씩 추가한다.

```
기본 알까기
  -> UI/HUD
  -> 공룡알/보드 비주얼
  -> 사운드/카메라/파티클
  -> 폭발물
  -> 바람
  -> HP/능력 알
  -> 공룡 NPC
  -> 4인 모드
  -> LAN 멀티
```

### 2-3. 매일 빌드가 돌아가야 한다

마지막 날에 한 번에 통합하지 않는다. 매일 플레이 가능한 빌드를 유지한다.

---

## 3. MVP와 확장 구분

### 3-1. MVP 필수 범위

| 분류 | MVP 필수 기능 |
|---|---|
| 플레이 | 2인 핫시트, 턴 교대, 알 발사 |
| 물리 | 알끼리 충돌, 보드 밖 낙하, 정지 판정 |
| 룰 | 생존 알 수 관리, 승패 판정, 재시작 |
| UI | 현재 턴, 남은 알 수, 결과 화면 |
| 아트 | 공룡알 외형, 단순 보드/둥지 분위기 |
| 사운드 | 발사, 충돌, 낙하, 승리/패배 SFX |
| 카메라 | 탑다운 카메라, 최소한의 흔들림 |

### 3-2. MVP에서 제외하는 것

| 기능 | 제외 이유 |
|---|---|
| LAN 멀티 | 통합 리스크가 크다. 핫시트로 발표 가능하다. |
| CSV 자동 파서 | 초반 튜닝은 Inspector와 ScriptableObject로 충분하다. |
| 바람 | 조작감과 공정성을 해칠 수 있어 튜닝 비용이 크다. |
| 공룡 NPC | NavMesh, 애니메이션, 충돌 룰이 추가되어 범위가 커진다. |
| HP/능력 알 | 기본 알까기 룰이 안정된 뒤에 붙이는 것이 안전하다. |
| 4인 모드 | 보드 크기, UI, 룰 밸런스가 다시 필요하다. |

---

## 4. 단계별 버전 계획

### v0.1-Core: 기본 알까기 코어
회색 박스 형태라도 한 판이 끝나는 것.
- 알 생성/배치, 드래그 발사, 알 충돌, 낙하 감지, 턴 교대, 승패 판정, 재시작

### v0.1-Release: 발표 가능한 MVP
팀 발표에 올릴 수 있는 최소 완성판.
- 공룡알 모델/머티리얼, 둥지/숲 느낌 보드, HUD, 결과 화면, 기본 사운드, 충돌 파티클, 카메라 흔들림, Credits.md 정리

### v0.2: 환경 모듈
"환경이 개입하는 공룡 알까기"의 첫 인상. 폭발물 > 바람 > 지진 > 공룡 NPC 순서.

### v0.3: 룰 확장
HP 시스템, 능력 알, 부화 시스템, 4인 모드, 고급 UI

### v0.4: 네트워크/장기 확장
LAN 멀티, 로비, 4인 LAN, 챔피언십 모드

---

## 5. 발표 성공 기준

- 처음 보는 사람이 10초 안에 규칙을 이해한다.
- 두 명이 번갈아 플레이할 수 있다.
- 한 판이 끝까지 진행된다.
- 알 충돌과 낙하가 시각적으로 잘 보인다.
- 승패 화면이 나온다.
- 사용한 외부 에셋의 출처가 정리되어 있다.

---

## 6. 기술 스택

### Unity 내장/공식 기능

| 영역 | 사용할 기능 |
|---|---|
| 물리 | Rigidbody, SphereCollider, Physic Material |
| 낙하 | Trigger Collider |
| 입력 | Unity Input System |
| UI | UGUI, Canvas, Button, TextMeshPro |
| 카메라 | Cinemachine, Cinemachine Impulse |
| 사운드 | AudioSource, AudioMixer |
| 이펙트 | Particle System |
| 데이터 | ScriptableObject |
| 저장 | PlayerPrefs |
| NPC | NavMesh (v0.3+) |

### 오픈소스/무료 에셋 계획

| 이름 | 용도 | 라이선스 |
|---|---|---|
| Mirror | LAN 멀티 | MIT |
| LitMotion | 트윈 애니메이션 | MIT |
| DOTween Free | 트윈 애니메이션 | 상업 사용 가능 |
| Quaternius | 공룡 3D 에셋 | CC0 |
| Kenney | UI/3D 에셋 | CC0 |
| Google Fonts | 폰트 | SIL OFL |
| Freesound | 효과음 | CC0/CC-BY |
| Game-icons.net | 아이콘 | CC-BY |
| Pixabay | 음악/효과음 | 약관 확인 필수 |

**사용 금지**: CC-BY-NC, CC-BY-ND, GPL, 라이선스 불명확 무료 에셋, 유명 IP/팬아트/상업 게임 모방 에셋

---

## 7. 구현 전략

```
1. Basic Core: 알 발사, 충돌, 낙하, 턴, 승패
2. Playable MVP: UI, 결과 화면, 재시작, 기본 사운드
3. Presentation: 공룡알 비주얼, 보드 아트, 카메라, 파티클
4. Environment Modules: 폭발물, 바람, 지진, 공룡 NPC
5. Expansion Modules: HP, 능력 알, 부화, 4인 모드, LAN 멀티
```

**새 기능이 실패해도 Basic Core는 계속 플레이 가능해야 한다.**

---

## 8. Unity 폴더 구조

```
Assets/
  _Project/
    Scenes/          Main.unity, Prototype.unity
    Scripts/
      Core/          게임 핵심 로직
      Input/         드래그/발사 입력
      Rules/         턴/승패/판정
      Presentation/  UI, 카메라, 사운드, 이펙트
      Environment/   폭발물, 바람, 지진, NPC
      Data/          ScriptableObject 설정
      Utility/       공통 유틸리티
    Prefabs/         Eggs, Board, UI, Effects, Environment
    ScriptableObjects/ Rules, Physics, Audio, FeatureFlags
    Art/             Models, Materials, Textures, UI
    Audio/           BGM, SFX
    ThirdParty/      외부 라이브러리/에셋 (출처+라이선스 기록 필수)
    Resources/
```

---

## 9. 4인 역할 분담

| 팀원 | MVP 필수 책임 | 확장 책임 |
|---|---|---|
| Person A | 알 물리/입력/발사 (EggController, FlickInputController, EggSpawner) | HP, 능력 알, 알별 외형 |
| Person B | 턴/정지/낙하/승패/빌드 (TurnController, MotionResolver, BoardFallZone, WinConditionChecker) | 4인 룰, LAN 실험 |
| Person C | 게임 흐름/UI/HUD/결과 (GameSessionController, HudPresenter, ResultScreen, MainMenuController) | 로비, 설정, 고급 UI |
| Person D | 카메라/사운드/파티클/에셋/라이선스 (CameraController, AudioManager, EffectController) | 폭발물, 바람, 공룡 NPC |

---

## 10. 2주 개발 일정

| Day | 목표 | A | B | C | D |
|---|---|---|---|---|---|
| 1 | 프로젝트 세팅 | 알 Prefab 초안 | 보드/낙하 구조 초안 | UI 씬/Canvas 초안 | 아트 폴더/Credits 초안 |
| 2 | 알 발사 | 드래그 발사 | 낙하 Trigger | 임시 HUD | 임시 카메라 |
| 3 | 턴/승패 | 물리 튜닝 | 턴/정지/승패 | 턴 표시 | 기본 SFX |
| 4 | Basic Core 완성 | 알 충돌 안정화 | 한 판 완주 | 결과 화면 | 충돌 파티클 |
| 5 | 플레이 테스트 | 힘/마찰 튜닝 | 룰 버그 수정 | UI 가독성 개선 | 카메라 개선 |
| 6 | MVP UI | 입력 polish | 빌드 테스트 | 메뉴/HUD/재시작 | 사운드 정리 |
| 7 | 공룡 테마 | 알 외형 적용 | 보드 판정 점검 | UI 톤 적용 | 보드/둥지 에셋 |
| 8 | 연출 강화 | 충돌감 조정 | 디버그 UI | 결과 화면 polish | 셰이크/파티클 |
| 9 | 전원 테스트 | 버그 수정 | 버그 수정 | 버그 수정 | 버그 수정 |
| 10 | 폭발물 모듈 | 폭발 힘 반응 | 폭발 후 판정 | 폭발 UI 표시 | 폭발물 구현 |
| 11 | 확장 튜닝 | 알 물리 보호 | 룰 안정화 | UI 정리 | 폭발/바람 선택 |
| 12 | 기능 동결 | 치명 버그 수정 | 빌드 안정화 | 발표 UI 마감 | Credits 마감 |
| 13 | 발표 빌드 고정 | 테스트 | 최종 빌드 | 테스트 | 테스트 |
| 14 | 발표 | 시연 지원 | 시연 지원 | 시연 지원 | 시연 지원 |

---

## 11. 브랜치 전략

```
main
  feature/A-egg-physics
  feature/B-rules
  feature/C-ui-flow
  feature/D-presentation
  feature/D-bomb-module
```

규칙:
- `main` 직접 수정 금지. 기능별 브랜치에서 작업.
- 병합 전 최소 1명이 실행 확인.
- 충돌이 나면 기능보다 Basic Core 보존을 우선한다.

---

## 12. 완료 기준 (DoD)

### 기능 완료 기준
- Unity Editor에서 실행된다. Basic Core를 망가뜨리지 않는다.
- Inspector 연결 누락이 없다. 에러 로그가 없다.
- 담당자가 아닌 1명이 실행해 봤다. 변경 내용을 Git에 커밋했다.

### 발표 빌드 완료 기준
- Windows 빌드가 실행된다. 2인 핫시트 한 판이 끝난다.
- 결과 화면이 나온다. 사운드 볼륨이 적절하다.
- 외부 에셋 출처가 기록되어 있다. 발표 씬이 명확하다.

---

## 13. 매일 작업 루틴

**시작 전 10분**: 오늘 작업 확인, 어제 빌드 상태 확인, 충돌 가능 파일 확인

**작업 중**: AI에게 작은 단위로 요청, 생성 코드를 읽어보기, 새 Manager/Singleton 지양, 기존 클래스명/이벤트명 유지

**종료 전 30분**: main 기준 실행 확인, 변경사항 커밋, 외부 에셋 사용 시 Credits 기록, 다음날 할 일 3개 정리

---

## 14. Vibe Coding 공통 규칙

### 공통 프롬프트
```
Unity 6.3 LTS, C# 기준입니다.
이 프로젝트는 2인 핫시트 3D 공룡 알까기입니다.
직접 물리 엔진을 만들지 말고 Rigidbody, Collider, Physic Material을 사용해 주세요.
기존 클래스명은 GameSessionController, TurnController, EggController, FlickInputController, MotionResolver입니다.
새 기능은 기본 알까기 코어를 망가뜨리지 않아야 하며, 가능하면 Inspector에서 켜고 끌 수 있게 만들어 주세요.
복잡한 새 아키텍처를 만들지 말고 현재 구조에 맞춰 작게 구현해 주세요.
```

### AI 코드 리뷰 규칙
AI가 코드를 만들면 다른 AI에게 아래 질문으로 검토한다.
```
이 Unity C# 코드에서 null reference, 턴이 끝나지 않는 문제, 이벤트 중복 호출,
Rigidbody 물리 처리 문제, Inspector 연결 누락 가능성이 있는지 리뷰해 주세요.
```

---

## 15. 팀 공통 금지사항

- Basic Core 완성 전 네트워크 작업을 main에 병합하지 않는다.
- 라이선스 불명 에셋을 쓰지 않는다.
- AI가 만든 코드를 이해하지 않고 붙이지 않는다.
- 새 기능 때문에 턴/승패가 망가지면 즉시 되돌리거나 비활성화한다.
- Day 12 이후 새 기능을 추가하지 않는다.

---

## 16. 매일 확인할 테스트

- 프로젝트가 실행되는가?
- Basic Core 한 판이 끝나는가?
- 턴이 멈추지 않고 넘어가는가?
- 알이 보드 밖으로 떨어지면 제거되는가?
- 한 플레이어의 알이 0개가 되면 결과 화면이 나오는가?
- 새 기능을 꺼도 게임이 돌아가는가?

---

## 17. 참고 링크

- Unity Asset Store 상업 사용 안내: https://support.unity.com/hc/en-us/articles/205623589-Can-I-use-assets-from-the-Asset-Store-in-my-commercial-game-
- Mirror GitHub: https://github.com/MirrorNetworking/Mirror
- LitMotion GitHub: https://github.com/annulusgames/LitMotion
- DOTween License: https://dotween.demigiant.com/license.php
- Kenney: https://kenney.nl/support
- Quaternius Dinosaurs: https://quaternius.itch.io/animated-lowpoly-dinosaurs
- Google Fonts FAQ: https://developers.google.com/fonts/faq
- Game-icons FAQ: https://game-icons.net/faq.html
