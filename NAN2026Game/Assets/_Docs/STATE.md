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

- [제출 전 필수] Scene2DirectorConfig.debugSkipToBoss = false 로 끌 것 (보스전 테스트 스위치)

- [제출 전 필수] MinoBossConfig.showParryDebug = false (패링 디버그 팝업)


---

# 인계 요약 (2026-08-08 21:16 기준, D-2 야간 세션 종료분)

## 이번 세션에 완성된 것
- **Scene2(AdventureScene2) 보스전 풀코스**: 어둠 → 천장/돌진 스파이크 패링 5회(상단 라벨 n/5 + 보스 위 노랑 ◆핍) → '어둠이 걷혔다!' 밝아짐 → **보스 개막 카메라 팬** → 미노 보스전
- **SecondSceneBoss**(구 MinoBossAI→NanMinoBoss→SecondSceneBoss 2회 개명, 팀 MidBoss와 충돌 회피): atk_1 이단 패링(프레임 5~8·11~14) / atk_2 시간창(0.62~0.82) / 선입력 버퍼 0.2s / 패링 5회 그로기 / 10타 death
- **그로기 버스트**: 'Z 연타! 공격 찬스!' + Z 자동 대시 + 공속 2배(PC2D.AttackSpeedMul) + 금빛 펄스·✦ 반짝
- **피격 피드백**(배너 UI 철거 후 대체): 빨간 점멸 0.12s + 머리 위 'HP n/10' 붉은 팝업 + take_hit
- **MP 시스템**: ManaConfig(총량 10·패링 +1) + PlayerMana + 좌상단 파란 하트 10개(독립 캔버스 1920 기준). 전 패링(구체·트랩·보스)이 MP 지급. TryUseMp는 API만 대기(소모 연동은 팀 결정)
- **연출 락 체계 통일**: PlayerController2D.InputLocked 정적 게이트(컨트롤러 계속 구동, 입력만 차단) — Scene2 밝아짐 / 그로기 Z대시 / Scene3 토치 인트로 3곳. parryHeld 자가 회복 포함
- **Scene3 토치 인트로**: 아무키 스킵 제거(완주 보장) + 이동 락 + 오디오 락(BGM 예외)
- **데몬 보스(AdventureScene4)**: 플레이어 7배(PPU 9.9, 10.61u)·투사체 3배(PPU 33.3), transform 32f 인트로 → idle/walk/cleave(패링)/smash(접근 공격·패링)/cast_spell(투사체 비행 3f 루프→명중·패링·타일맵 충돌 시 폭발 11f) / 패링 5회 그로기 / 10타 death / 바닥 접지

## 반드시 지켜야 할 하드 교훈 (FAIL.md 참조 필수)
1. **Kinematic 트리거**: 양쪽 Kinematic이면 useFullKinematicContacts=true 필수 (미노에서 재범 — Z 대미지 전멸)
2. **프리팹 개명 병합 후**: 씬·프리팹 '슬롯 배선(SerializedProperty)' 전수 검사 — 컴파일 통과는 보증 아님. Skill1/2 유령 참조로 Z 대미지 무음 사망 겪음
3. **DisableDomainReload 프로젝트**: 모든 static은 세션 간 생존 — static 추가 시 RuntimeInitializeOnLoadMethod(SubsystemRegistration) 리셋 동봉. 적용된 4곳: PC2D(InputLocked·AttackSpeedMul)/ThrownProjectile(Alive)/Launcher(waveBudget·reserved)/SpikeParryEvents(Count·OnParry)
4. **timeScale 히트스톱**: 복구 담당 FX 수명 < 히트스톱이면 영구 정지 — 수명 보정 + OnDestroy 안전핀
5. **입력 게이트 락**: '뗌 이벤트' 유실 → Held 계열은 isPressed 기반 자가 회복 필수
6. **팀 병합이 우리 파일을 리팩터하면** 치환 앵커 전멸 → 통짜 재작성 우선. Scene2Director는 이미 확정판 재작성됨(이후 이 파일은 부분 치환 금지)
7. **배선 후엔 반드시 재읽기 검증** (데몬 config 유실로 재생 전체가 멈춘 사고)

## 남은 작업 (우선순위)
1. 데몬 보스 재생 검증 및 수치 튜닝(사거리·속도·투사체) — DemonBossConfig 다이얼
2. Scene2 풀코스 최종 완주 후 봉인
3. 제출 전 디버그 OFF: MinoBossConfig.showParryDebug / showRangesInGame (debugSkipToBoss는 이미 OFF)
4. **WebGL 지뢰**: Assets/Scripts/SlashProjectile.cs가 gitignored 폴더의 NHNDemo.MonsterHealth 참조 → 신규 클론 컴파일 실패 위험. 제출 전 해결 필수
5. PR: 타이틀 'feat: 2번째 씬 보스전 + 투척 함정 패링 시스템 (어둠·MP 이코노미)' (데몬 추가분 반영 필요)
6. 팀 공지 4건: Scene2 Player를 프리팹 인스턴스로 교체 권장(손조립이라 배선 유실 2회) / 팀 SkillImage 흰네모(스프라이트 유실) / Scene2Director 재작성 고지 / 보스 씬별 배정(팀 데몬·미드보스 vs 우리 SecondSceneBoss)
7. 잔무: Scene2 재진입 시 밝기 유지(정적 플래그 미적용), 키맵 README, AI 활용 문서

## 작업 규약 재확인
- Z=2콤보 / X=검기 스킬 / **스페이스=패링** / C=스킬변경(보류) / 엔터=대화(보류) / 싱글톤·DontDestroy(보류)
- 모든 수치는 SO Config 소유. Player 프리팹 수정은 **예외 허가됨**
- push/pull/reset은 사람만. 나는 add·commit까지
- '테스트 시작' 선언 시 컴파일 유발 작업 전면 중지(도메인 리로드 렉 방지)

## 기록 파일 분리 (2026-08-09)
- **내 작업 기록은 이제 `Assets/_Docs/LOG_donghyun.md` 에만 쓴다.** 팀 공용 `LOG.md` 에는 더 이상 추가하지 않는다
- 이유: 팀원(NoImpMe)도 LOG.md 끝에 덧붙이는 구조라 병합할 때마다 충돌했다(LOG.md 누적 484커밋, 팀원 1164행)
- 분리 방식: git blame 으로 worldgreatkim 작성분 6370행만 추출. LOG.md 원본은 **무변경**으로 남겨 팀원 영향 0
- 제출용 'AI 활용 기술 문서' 는 LOG_donghyun.md 하나만 읽으면 된다
- FAIL.md / ASSET_CREDITS.md 는 **공유가 목적**이라 분리하지 않고 .gitattributes 의 merge=union 으로 자동 병합 처리
