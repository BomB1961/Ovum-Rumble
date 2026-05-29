# Ovum Rumble

## LAN 방 생성/입장 UI 흐름

브랜치: `feature/C-lan-join-room-button`  

 메인 메뉴에서 LAN 방을 생성하고 입장하는 UI 흐름을 수정한 작업입니다.  
주요 목적은 플레이어가 방을 만들거나 IP로 방에 입장할 때의 흐름을 더 명확하게 만들고, 메인 메뉴 → 맵 선택 → 게임 씬으로 이어지는 네트워크 전환 흐름을 개선하는 것입니다.

### 주요 변경사항

#### 메인 메뉴 UI 흐름

- `Create Room` 버튼 동작을 변경했습니다.
  - 기존: 버튼 클릭 시 바로 호스트 시작
  - 변경: 버튼 클릭 시 `CreateRoom_detail` 패널 표시
- LAN 입장 관련 버튼 핸들러를 추가했습니다.
  - `OnClickDirectConnect`
  - `OnClickInviteCode`
  - `OnClickJoinIpSubmit`
- Join IP 팝업인 `Popup_Panel_JoinIP` 표시 흐름을 수정했습니다.
- Join 입력창의 TMP 컴포넌트가 깨졌을 때 런타임에서 복구하는 `RepairJoinInputField` 로직을 추가했습니다.
- Settings, How-to, Join, Create Room 팝업이 메인 메뉴 위에 제대로 표시되도록 패널 show/hide 방식을 수정했습니다.

#### 네트워크 흐름

- `DinoNetworkManager`를 수정했습니다.
- 클라이언트가 `JoinAccepted`를 받으면 `02_MapSelect` 씬으로 이동하도록 변경했습니다.
- 클라이언트가 맵 선택 메시지를 받으면 `01_Game` 씬으로 이동하도록 변경했습니다.
- 릴레이 스레드 종료를 위한 `relayRunning` 변수를 추가했습니다.
- `OnDestroy`에서 릴레이 정리 로직을 추가했습니다.

#### 맵 선택 흐름

- `MapSelectController`를 수정했습니다.
- 호스트가 맵을 선택하면 클라이언트에게 아래 메시지를 함께 전송하도록 변경했습니다.
  - `MapSelectMessage`
  - `LoadSceneMessage`

이를 통해 클라이언트가 맵 선택 이후 게임 씬으로 정상 이동할 수 있도록 했습니다.

### 함께 포함된 추가 변경사항

이 브랜치에는 LAN Join 버튼 작업 외에도 비교적 큰 범위의 변경사항이 함께 포함되어 있습니다.


### 주의사항

브랜치 이름은 LAN Join Room Button 작업처럼 보이지만, 실제 변경 범위는 작은 버튼 수정에 그치지 않습니다.  
메뉴 UI 흐름, 네트워크 씬 전환, 맵 선택 흐름, 씬 파일, 에셋, 폰트, 일부 이벤트 파일 정리까지 함께 포함되어 있습니다.

따라서 이 브랜치를 리뷰하거나 병합할 때는 코드 변경뿐 아니라 씬 변경과 에셋 변경도 함께 확인해야 합니다.
