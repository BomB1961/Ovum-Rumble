# Ovum Rumble

## UI 디자인 작업

# UI Design Merge Summary

## Branch

| 구분 | 브랜치 |
|---|---|
| 기존 UI 작업 브랜치 | `feature/C-add-ui-design` |
| 충돌 해결 브랜치 | `resolve/C-add-ui-design-main-conflicts` |

`feature/C-add-ui-design` 브랜치에서 UI 디자인 작업 중 `main` 브랜치와 충돌이 발생했습니다.  
충돌 해결을 위해 `resolve/C-add-ui-design-main-conflicts` 브랜치를 새로 생성했고, 해당 브랜치에 `main`의 최신 변경 사항을 반영한 뒤 UI 디자인 작업을 정리했습니다.

---

## 현재 구현된 UI

| UI | 내용 |
|---|---|
| 메인 메뉴 UI | 메인 화면 디자인 요소 정리 |
| LAN 서버 UI | LAN 서버 화면에서 방 시작 / 참여하기 UI 정리 |
| 방 시작 UI | LAN 서버에서 방을 생성하는 버튼 UI 적용 |
| 참여하기 UI | 참여하기 화면에서 IP 표시 UI 적용 |
| 현재 차례 UI | 현재 턴의 플레이어를 표시 |
| 조준선 / 힘 조절선 UI | 알 조작 시 조준 방향과 힘 조절 상태 표시 |
| 승리 UI | P1 / P2 승리 결과 표시 |
| 인게임 UI | 게임 진행 상태를 확인할 수 있는 UI 정리 |
| 아이콘 UI | 일부 텍스트 버튼을 아이콘 표시 방식으로 변경 |
| 폰트 UI | 한글 표시를 위한 TMP Font Asset 적용 |

---

## 파일 정리

| 파일 / 리소스 | 내용 |
|---|---|
| `00_MainMenu.unity` | 메인 메뉴 및 LAN 서버 UI 변경 사항 정리 |
| `01_Game.unity` | 인게임 UI, 조준선 / 힘 조절선, 승리 UI 변경 사항 정리 |
| `BlackHanSans-Regular SDF.asset` | 한글 UI 폰트 적용 |
| `Packages/packages-lock.json` | Unity 패키지 변경 사항 확인 |
| UI PNG 리소스 | 실제 인게임 적용 가능한 이미지 에셋 정리 |
| UI Icon 리소스 | 텍스트 일부를 대체하는 아이콘 리소스 정리 |

---

## 기능 - 내용

| 기능 | 내용 |
|---|---|
| 메인 메뉴 | 메인 화면 UI 표시 |
| LAN 서버 | LAN 서버 메뉴 UI 표시 |
| 방 시작 | LAN 서버에서 방 생성 가능 |
| 참여하기 | LAN 서버에서 방 참여 가능 |
| IP 표시 | 참여하기 화면에서 IP 정보 표시 |
| 씬 이동 | 메인 메뉴에서 게임 씬으로 이동 |
| 턴 전환 | 현재 차례 UI를 통해 플레이어 턴 표시 |
| 알 조작 | 기존 인게임 조작 흐름 유지 |
| 조준선 / 힘 조절선 | 알 조작 시 방향과 힘 조절 상태 표시 |
| 승리 처리 | 플레이 결과에 따른 승리 상태 처리 |
| 승리 UI | P1 / P2 승리 결과 UI 출력 |
| 아이콘 표시 | 일부 텍스트 UI를 아이콘 방식으로 변경 |

---

## 확인 사항

- Git 충돌 파일 없음
- 충돌 마커 없음
- `00_MainMenu.unity`, `01_Game.unity` UU 상태 아님
- Unity Console Error 없음
- 기본 플레이 흐름 확인 완료
