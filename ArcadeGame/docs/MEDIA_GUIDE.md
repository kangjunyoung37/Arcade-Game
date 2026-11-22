# README 기능별 GIF 추가 가이드

README에 GIF가 들어갈 공간 12곳을 마련했습니다. 적 AI는 행동별 8곳으로 나눴고, 나머지 주요 기능에도 각각 자리가 있습니다. 현재는 회색 안내 이미지가 표시되며, 각 제목 바로 아래에서 실제 GIF로 교체할 수 있습니다.

## 기능별 GIF

| 기능 | 담으면 좋은 장면 | 파일 경로 |
| --- | --- | --- |
| 시야 | 장애물 뒤의 적이 가려졌다가 시야에 들어오는 순간 | `docs/media/vision.gif` |
| AI · 시각·청각 감지 | 시야에 들어오는 플레이어와 시야 밖 총성·발소리에 대한 반응 | `docs/media/enemy-perception.gif` |
| AI · 순찰·경계 | 순찰하던 적이 감지 위치를 향해 멈춰 서는 장면 | `docs/media/enemy-patrol-alert.gif` |
| AI · 추격·수색 | 플레이어를 놓친 뒤 마지막 위치와 주변을 살피다가 재발견 | `docs/media/enemy-chase-search.gif` |
| AI · 전투 이동 | 플레이어와의 거리에 따른 접근·후퇴·좌우 이동 | `docs/media/enemy-combat-movement.gif` |
| AI · 엄폐·재장전 | 잔탄이 부족한 적이 엄폐 지점으로 이동해 재장전 | `docs/media/enemy-cover-reload.gif` |
| AI · 좌우 피킹 | 엄폐물 옆으로 나와 사격하고 다시 숨는 과정 | `docs/media/enemy-cover-peek.gif` |
| AI · 피격·회피 | 연속 피격이나 근접 교전에서 이동 공간을 확보해 회피 | `docs/media/enemy-hit-dodge.gif` |
| AI · 제압 사격 | 플레이어가 숨은 뒤 마지막 조준 위치에 사격하고 추격 | `docs/media/enemy-suppression.gif` |
| 사격과 전투 피드백 | 연속 사격, 크로스헤어 변화, 탄피와 피격 효과 | `docs/media/shooting.gif` |
| 탄창과 약실 관리 | 탄창 교체 전후의 잔탄·약실 상태 변화 | `docs/media/ammo-management.gif` |
| 인벤토리 | 아이템 드래그·회전과 탄창 잔탄 표시 | `docs/media/inventory.gif` |

기능별 GIF는 한 동작을 알아보기 쉽게 짧게 잘라 넣으면 됩니다. 탄약 관리 장면은 현재 에디터 디버그 메뉴를 이용해 촬영할 수 있습니다.

AI 제압 사격은 해당 행동을 허용한 프로필로 촬영하세요. 확률이나 재사용 대기 시간이 적용되는 행동은 발동 장면을 골라 넣으면 됩니다.

## 기존 영상 활용

로컬에 `Recordings/ArcadeGame-play.mp4`가 있습니다. 내용을 확인한 뒤 현재 기능을 잘 보여준다면 이 영상을 사용할 수 있습니다.

`Recordings/`는 현재 `.gitignore`에 포함돼 있으므로 README에서 이 로컬 경로를 바로 연결하면 GitHub 방문자는 영상을 볼 수 없습니다. GitHub 이슈·PR 작성 화면의 첨부 기능이나 YouTube 등에 영상을 업로드한 뒤, 실제로 열리는 링크를 README에 넣으세요. README의 Markdown 문법으로 MP4 파일을 이미지처럼 삽입하기보다는 영상 링크를 사용하는 편이 간단합니다.

## GIF 교체 방법

1. 위 파일명으로 GIF를 `docs/media/` 폴더에 추가합니다. GIF 파일은 현재 저장소의 Git LFS 규칙을 따릅니다.
2. README에서 해당 기능 아래 이미지 경로를 `docs/media/gif-placeholder.svg`에서 실제 GIF 경로로 바꿉니다. 각 이미지 아래 HTML 주석에도 교체할 경로를 적어뒀습니다.

   ```markdown
   ![시야 시스템](docs/media/vision.gif)
   ```

3. GIF를 GitHub에 직접 첨부했다면 이미지 경로 대신 발급된 URL을 넣어도 됩니다.

   ```markdown
   ![시야 시스템](실제-GIF-주소)
   ```

4. 교체한 기능의 안내 주석을 지우고 GitHub 미리보기에서 재생을 확인합니다. 아직 GIF가 준비되지 않은 기능은 안내 이미지를 유지하면 됩니다.

상단 `플레이 미리보기`에는 대표 스크린샷이나 전체 플레이 영상 링크를 선택적으로 추가할 수 있습니다.

프로젝트의 정식 이름, 개발 기간, 담당 범위를 공개하고 싶다면 확인된 정보를 README 소개에 추가할 수 있습니다. 현재 문서에는 해당 정보를 임의로 넣지 않았습니다.
