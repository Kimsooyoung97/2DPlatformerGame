# 인계서 (HANDOFF)

**작성:** 2026-08-09 02:01 · **마감:** 2026-08-10 · **Unity 6000.5.3f1** · **WebGL + GitHub Pages**

> 새 세션 시작 시: `[조사] HANDOFF.md 기준으로 현재 상태 파악하고 남은 작업 확인해줘`
> 상세 이력은 STATE.md 말미 '인계 요약'과 LOG.md, 커밋 히스토리 참조.

## 1. 완성된 시스템

- **Scene2 보스전 풀코스**: 어둠 → 스파이크 패링 5회(상단 라벨 + 보스 위 노랑 ◆핍) → '어둠이 걷혔다!' 밝아짐 → 보스 개막 카메라 팬 → 미노 보스전 → 10타 격파
- **SecondSceneBoss**: atk_1 이단 패링(프레임 5~8·11~14) / atk_2 시간창(0.62~0.82) / 선입력 버퍼 0.2s / 공격 패링 5회 → 그로기(주황 ◆ 5칸) / 피격 = take_hit + 빨간 점멸 + 'HP n/10' 팝업
- **그로기 버스트**: 'Z 연타!' 안내 + Z 자동 대시 + 공속 2배 + 금빛 반짝
- **데몬 보스(Scene4)**: 플레이어 7배(PPU 9.9) · 투사체 3배 · transform 32f 인트로 → idle/walk/cleave/smash/cast(투사체) / 그로기 5회 / 10타 death
- **MP 이코노미**: 총량 10 · 모든 패링 +1 · 좌상단 파란 하트 10개(독립 캔버스 1920 기준). TryUseMp는 API만 대기(소모량 팀 결정)
- **연출 락 통일**: `PlayerController2D.InputLocked` 정적 게이트(컨트롤러 계속 구동, 입력만 차단) — Scene2 밝아짐 / 그로기 대시 / Scene3 인트로
- **Scene3 토치 인트로**: 스킵 제거(완주 보장) + 이동 락 + 오디오 락(BGM 예외)
- **레벨/쇼룸**: 공중 발판 원웨이 통일(Stage_Ground 이관), 접지 법선 필터, 소품 분리(Furnace/Sawmill·Decor_40·Decor_40_b·Pine_16), 애니 소품 진열대(Furnace·Sawmill·Boat·WaterFall·TallGrass·Flame)

## 2. 하드 교훈 (FAIL.md 동기화 — 재범 금지)

1. Kinematic끼리 트리거는 `useFullKinematicContacts=true` 필수 (미노에서 재범, Z 대미지 전멸)
2. 프리팹 개명 병합 후 **슬롯 배선 전수 검사** — 유령 참조가 침묵 가드로 기능을 무음 사망시킴
3. DisableDomainReload 프로젝트: static은 세션 간 생존 → `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` 리셋 동봉 (현재 4곳 적용)
4. timeScale 히트스톱: 복구 담당 FX 수명 < 히트스톱이면 영구 정지 → 수명 보정 + OnDestroy 안전핀
5. 입력 게이트 락은 '뗌 이벤트' 유실 → Held 상태에 isPressed 자가 회복 필수
6. 병합이 우리 파일을 리팩터하면 치환 앵커 전멸 → 통짜 재작성 우선 (Scene2Director는 확정판)
7. 배선 후 반드시 재읽기 검증 (데몬 config 유실로 재생 전체가 멈춤)
8. Tilemap 작업은 페인트→저장→**디스크 재로드 후 재검증** 3단계

## 3. 남은 작업 (우선순위)

1. **데몬 보스 재생 검증·수치 튜닝** (DemonBossConfig)
2. Scene2 풀코스 완주 후 봉인
3. 제출 전 디버그 OFF: MinoBossConfig.showParryDebug / showRangesInGame
4. **WebGL 지뢰**: `SlashProjectile.cs`가 gitignored 폴더의 `NHNDemo.MonsterHealth` 참조 → 신규 클론 컴파일 실패 위험. 제출 전 필수
5. PR: 'feat: 2번째 씬 보스전 + 투척 함정 패링 시스템 (어둠·MP 이코노미)' — 데몬 반영 필요
6. 팀 공지 5건 (아래)
7. 잔무: Scene2 재진입 밝기 유지, 키맵 README, AI 활용 문서

## 4. 팀 전달 사항 (미발송)

1. Scene2 Player를 프리팹 인스턴스로 교체 권장 (손조립이라 배선 유실 2회)
2. 팀 SkillImage 흰 네모(스프라이트 유실)
3. Scene2Director 전면 재작성 고지
4. 보스 씬별 배정 정리 (팀 데몬/미드보스 vs 우리 SecondSceneBoss·DemonBoss)
5. PlayerController2D 접지 법선 필터 추가 고지 (병합 충돌 가능)
6. 스킬 MP 소모량·시작 MP 결정 요청

## 5. 규약

- 수치는 전부 ScriptableObject Config 소유 / **Player 프리팹 수정은 예외 허가됨**
- push·pull·reset은 사람만. AI는 진단·add·commit까지
- 명령 1개 = LOG 1개 + (수정 시) 커밋 1개
- '테스트 시작' 선언 시 컴파일 유발 작업 전면 중지
- 키맵: Z=2콤보 / X=검기 / **Space=패링** / C·Enter·싱글톤=보류

## 6. 실측 참고치

- 플레이어 콜라이더 높이 **1.52u**(모든 비율 기준) / 데몬 PPU 9.9 · 투사체 33.3 / 미노 scale 1.3 @x188
- Scene2 전역광 0.03 → 0.55 / 데몬 배치 Scene4 (22.5, 7.82)
- Test1 발판 원웨이: Stage_Ground(Composite usedByEffector + PlatformEffector2D arc170)

## 7. .gitignore 주의

`_Docs/*.md` 5종이 무시 목록에 있으나 **이미 추적 중이라 규칙이 무력**하다(공유됨). 그래서 병합 때마다 LOG.md 충돌이 난다 — 팀 것 채택으로 넘기면 된다. 로컬 전용으로 돌리려면 `git rm --cached` 필요(사람이 실행).
