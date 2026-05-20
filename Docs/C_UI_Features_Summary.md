# C UI Features Summary

## 구현 범위

- `00_MainMenu`
  - 메인 메뉴 패널 구성
  - 방 만들기, 방 참가, 테스트 참가 버튼 연결 구조
  - 설정 패널 열기/닫기
  - BGM/SFX 슬라이더 UI 연결
  - 게임 종료 버튼 처리

- `01_Game`
  - HUD 패널 구성
  - 현재 턴, P1/P2 알 개수 표시
  - 남은 턴 시간 표시
  - 전체 게임 시간 표시
  - 결과 패널 구성
  - 재시작, 메인 메뉴, 게임 종료 버튼 처리
  - 설정 패널과 게임 종료 버튼 처리

## 추가 스크립트

- `MainMenuController`
- `GameUIController`
- `GameSessionUiBridge`
- `HudPresenter`
- `ResultScreen`

## 연동 대기 항목

- 방 만들기, 방 참가는 Mirror 네트워크 동기화 연동 필요
- 실제 턴 제어는 `TurnController` 연동 필요
- 실제 승패 판단은 `WinConditionChecker` 연동 필요
- 알 개수 변화는 B/Core의 `GameSessionController` 또는 게임플레이 로직에서 UI 쪽으로 전달 필요
- 승패 결과는 B/Core의 `GameSessionController`와 `WinConditionChecker` 결과를 `ResultScreen`에 연결 필요
