# STATE.md — 현재 상태와 다음 단계

## 현재 단계

**S1 조작감(플레이어 완성 단계) + 보스 파트 착수**

## 완료

- S0 문서 체계 (기존)
- 플레이어 스프라이트 시트 임포트·슬라이스 34프레임, 클립 4종(Idle/Walk/Run/Slash) + 컨트롤러
- MovementConfig(SO) + PlayerLocomotionLogic(순수, NAN2026.Core) + PlayerController2D 구현
- 조작: ←→/AD 이동, Shift 달리기, Space/↑ 점프, 좌클릭 공격(지상, 이동잠금 0.5s)
- EditMode 테스트 15/15 (신규 7 포함)
- PPU 160 (임시), 캐릭터 월드 크기 0.96x1.69u

## 다음 단계

- 사용자 플레이 확인 → 수치 튜닝 (MovementConfig)
- COMBO2/COMBO3 시트 수급 → 3연타 콤보 구현
- FeelConfig / CombatFormula 구축, 대시·패링
- PPU 확정 (타일셋 기준)

## 대기 중

- 컨셉 시트 잔여: 제목, 참조 이미지(적 4종 생성물)


## 팀 통합 메모 (2026-08-01)
- 우리 스테이지 = Assets/Scenes/SecondScene.unity (팀 규약). 쇼룸(BiomeActionMap)은 테스트장으로 유지
- Player.prefab / Princess_Boss.prefab 사용 가능
- 차단 요소: 바이옴 팩 2종(American Forest/Plains) 미커밋 — 라이선스 확인 시 커밋 필요 (없으면 팀원 화면에서 타일 깨짐)

## FirstScene 배경 작업 (2026-08-02, 사용자 지시로 SecondScene 대신 FirstScene에서 진행)
- BackgroundFirstScene 하위에 Grid(Tilemap_Ground/Tilemap_Platforms) + Backdrop + Walls + Decoration 구성
- 타일/배경/소품 에셋을 Assets/sanctum_pixel/forest_side_pack 으로 전환 완료 (레퍼런스 이미지의 원본 팩으로 확인됨). 이전에 쓰던 두 Biome 팩(American Forest/Plains)은 더 이상 배경에 사용하지 않음
- Ground: x=-12~113, 3단(forest_tileset 상단/채움/하단 오토타일)
- Platforms: 14개 뜬 섬(계단형 노치 3개 포함)
- Backdrop: sky/cloud/mountain/pine1/pine2 5레이어, 기존 ParallaxLayer.cs(Assets/Scripts) 부착·계수 설정(0.05~0.7) — 실제 카메라 연동 움직임 있음
- Decoration 75개 (지면 61 + 섬 위 14), forest_side_pack 소품(pine/tree/bush/rock/flower) 사용
- Walls: x=-12.5 / x=114.5 BoxCollider2D로 낙사 방지
- **차단 요소 추가**: sanctum_pixel 폴더도 라이선스 미확인 — git 커밋 제외 중 (Biome 팩과 동일 상황)
- **미해결**: CameraBoundary(PolygonCollider2D)와 Portal 위치는 이번 배경 확장(3배, x=-12~114)에 맞춰 갱신되지 않음 — 수동 배치 오브젝트라 임의 수정하지 않음. 실제 플레이 시 카메라가 확장된 구간을 못 따라갈 수 있음, 사람 확인 필요
