# STATE.md — 현재 상태와 다음 단계

## 현재 단계

**SecondScene_extra 극암 던전 완성 → 빌드 리허설 직전 (마감 08-10)**

## 오늘 완료 (2026-08-05)

- git 재편: upstream private 전환 대응 — 새 포크 NAN2026Game1, 리모트 전환, 팀 16커밋 병합(LOG 충돌 봉합)
- SecondScene_extra 신축: 200u 일자 복도(동일 타일셋), 극암(전역 0.03)+토치 17기 실광원(+12px 상향)+시야광 4.5
- 소품 시스템: Unlit→Lit 재질 스윕 2회(46+62개), Door 정렬(Player SortingGroup 500 최전면), 계단 투명 램프 2기(수동 편집형), 귀환 포탈(194.4→SecondScene, 보라 발광)
- 스파이크볼 트랩: 시야x2 점멸 경고→조준 돌진→패링 판정(수평거리 수리), Config·순수로직·테스트 5
- TestScene: 팀 AI 타일셋 4종 슬라이스, 사용자 도면 기반 폐허 레벨(블록 9·경사 2)
- 패링 시트 8프레임 교체(PPU 604), 검기 Z키, NHNDemo 해소 확증
- FAIL#16 수립: 미저장 편집 보호(모든 OpenScene·정지 전 dirty 검사·강제 정지 금지)
- EditMode 130/130

## 즉시 미결 (다음 세션 최우선)

1. [구현] WebGL 빌드 리허설 — 선행: extra를 빌드 씬 목록에 추가(사용자 1클릭: Build Settings → Add Open Scenes)
2. 패링 판정 구분 팝업 패치(PERFECT!/MISS! + 색광 링 — MCP 단절로 미적용, 코드 준비됨)
3. [구현] 대시 / [구현] 보스 페이즈2(패링 방어전 시나리오 반영)

## 미결(누적)
- 하향점프: B안(OneWayDropThrough, 발판 레이어 부착·무침습) 채택. 팀원 PlayerController2D 활선(08-03~06 연속 커밋) 종료·병합 후 A안(컨트롤러 내장) 승격 예정 — 승격 시 B 컴포넌트 전 씬 제거 필수. 마감 전 창 안 열리면 B로 제출
- 제출물: AI 활용 기술 문서는 LOG.md 기반 PDF 생성 예정(요강 수령 대기), 빌드 후 작성

- walk 모션 상승 불가(하강 정상) — 컨트롤러 진단 대기(B안: 덧씌우기 제안됨)
- Effect_Vol.3 결정, GateTestTrigger 제거, 좌클릭 휘두름 사운드, ASSET_CREDITS 기입(사용자), 옛 포크 삭제(PR 병합 후)
- 시나리오 확정본 SCENARIO.md 저장 대기, 프롤로그 6컷 이미지 생성(팀 Opening 중복 확인)

---

# (이전 기록)

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

## SPEC.md 범위 예외 승인 (2026-08-02)
- SPEC.md는 '레벨업'을 범위 밖으로 명시하지만, 사용자가 대화 중 명시적으로 예외 승인함("Spec.md를 수정하지는 말고 그냥 직접적으로 승인할게 구현해줘"). SPEC.md 문서 자체는 미수정 상태로 유지 — 문서와 실제 구현이 이 부분에서 의도적으로 어긋나 있음을 다음 세션이 인지해야 함
- 경험치/레벨/증강(브론즈·실버·골드, 6종) 시스템 구현 완료. PlayerProgression 컴포넌트가 Player에 부착됨
