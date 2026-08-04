# STATE.md — 현재 상태와 다음 단계

## 현재 단계

**SecondScene 연출·사운드 완성 → 빌드 리허설 직전 (D-3, 마감 08-10)**

## 오늘 완료 (2026-08-04)

- 인트로 연출 완성: 암전 1.0s → 토치 3기 왼쪽부터 순차 점화(간격 0.6·각 1.2s) → 전역 확장 → 전투소리 BGM. 아무 키 스킵
- 게이트 붕괴 연출 완성(젤다식): 접근 1.6s(진동소음 램프+흔들림 2.0/주파수 0.5+줌 0.85, 타깃 47.5,5.0) → 붕괴사운드+벽돌 33개 캐스케이드 1.6s → 정적 1.4s. 돌무더기 비주얼(Locked 렌더러 OFF·충돌 ON)
- 2층 소품 30개 상시 숨김 → 붕괴 완료 시 활성 (인트로 게이트와 담당 분리)
- 사운드 시스템: SoundConfig(SO) + PlayerSoundPlayer(발소리1~3 순환·점프 0.35·검기발사1 Z키/피치 0.85) + SceneBgmPlayer(SecondScene_1=공주 만남)
- NHNDemo 의존 해소 확증: MonsterHealth.cs는 Assets/Player/Scripts/ 소재·git 추적 중 — fresh clone 컴파일 정상
- 임시 GateTestTrigger 부착 중(몬스터 정지·우클릭 붕괴 재생) — 튜닝 끝나면 제거 필요
- EditMode 테스트 113/113

## 내일 수순 (우선순위)

1. [구현] WebGL 빌드 리허설 → GitHub Pages 배포 확인 (최우선)
2. [구현] 대시 (Ctrl·무적 프레임, Knight_Roll 9프레임·구르기/대시 사운드 준비됨)
3. [구현] 보스 페이즈2 (HP 50% 전환)
4. 임시 테스트 트리거 제거, 좌클릭 휘두름 사운드 복원 여부 결정

## 미결

- Effect_Vol.3/ 미추적 (팀원 스킬 이펙트 깨짐 리스크) — 커밋/패키지 전달/방치 결정 대기
- ASSET_CREDITS: BGM 4곡 + SFX(돌무더기붕괴·진동소음1~2·발소리1~3·검기발사1) 출처·프롬프트 기입 (사용자 몫)
- 사망 사운드 배선 보류(사망 이벤트 소스 확인 필요), 석상 내려찍기·죽음 시트 수정본 대기

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
