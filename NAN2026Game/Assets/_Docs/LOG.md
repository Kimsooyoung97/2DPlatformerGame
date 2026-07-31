# LOG.md — 작업 기록

`[구현]` `[수정]` `[복구]` 작업 종료 시 맨 아래에 항목을 추가한다.
명령 1개 = 커밋 1개 = LOG 항목 1개.

---

## [구현] 베이스라인 커밋 + S0 문서 체계 — 2026-08-01 04:25

### 프롬프트
1~2번을 지금 실행하라. 끝까지 진행하고 중간에 확인을 구하지 마라. index.lock은 Unity나 다른 프로세스가 git을 잡고 있다 남긴 잔재다. 현재 git 프로세스가 없는지 확인한 뒤 제거하라. 나무 기능은 SPEC 범위 밖이다. S0에서는 SPEC에 적지 마라.

### 조작 내역
- `execute_code(safety_checks=false)` — git 프로세스 확인 → stale `index.lock` 제거
- `execute_code(safety_checks=false)` — `git add -A`, `git commit -F`
- 생성: `Assets/_Docs/SPEC.md`, `STATE.md`, `LOG.md`, `FAIL.md`, `ASSET_CREDITS.md`
- `.gitignore` 미수정 (요청 블록이 이미 존재)

### 검증
- `Process.GetProcessesByName("git")` → 0개. `index.lock` 0바이트 / 164분 경과 → stale 판정 후 삭제, `exists after: False`
- `git add -A` → exit=0
- `git diff --cached --name-only` → 50개. 아트 폴더 유출 0건 (`Assets/Player/`, `Assets/Map/`, `Assets/Art/`, `Assets/Screenshots/`, `_Recovery` 모두 False)
- `git check-ignore -v` 8경로 → 전부 IGNORED
- `git commit` → exit=0, `cf7bcc4 chore: baseline — 기존 스크립트 및 프로젝트 설정`
- `git status --porcelain` → 출력 없음 (clean)

### 실패와 수정
- `git add -A` 최초 시도가 `index.lock: File exists`로 실패 → git 프로세스 0개 확인 후 stale lock 제거하여 해결
- `Process.Start` + `ReadToEnd()` 조합에서 `Timeout receiving Unity response` 반복 발생 → 출력 리다이렉트를 끄고 `WaitForExit(ms)`만 쓰거나, `cmd /c ... > file` 로 파일에 받은 뒤 읽는 방식으로 해결. FAIL.md #4로 기록

## [수정] SPEC.md (작성 예정) 섹션 작성 — 2026-08-01 04:38

### 프롬프트
[수정] SPEC.md의 (작성 예정) 섹션을 채워라
내용은 아래 그대로. 새로 지어내지 마라.
## 제품 정의
Unity 2D 횡스크롤 액션. 개발자 1인. NAN 2026 사전과제.
플레이 시간 3~5분. 죽으면 처음부터.
## 핵심 루프
짧은 횡스크롤 구간 → 아레나 웨이브 3개 → 보스 1
## 조작
이동, 점프, 근접 공격 3연타 콤보, 대시(무적 프레임)
## 제출 범위
- 적 2종 (돌진형, 원거리형)
- 보스 1 (패턴 2개, 체력 50%에서 페이즈 전환)
- 맵 1개
- 빌드 타깃: WebGL (GitHub Pages). .exe 제출 불가
## 범위 밖
저장, 인벤토리, 스킬트리, 레벨업, 상점, 여러 맵, 스토리 컷신,
옵션 메뉴, 파괴 가능한 환경 오브젝트
## 미정 — S1 조작감 판정 후 결정
세계관, 캐릭터 설정, 무기 종류, 적 디자인, 제목
## 기술 기준
- 플레이어 스프라이트 캔버스 96×84px, 본체 약 16×40px 하단 정렬
- 콜라이더는 96×84가 아니라 16×40 기준
- PPU: 미정 (타일셋 타일 크기 확인 후 확정)
## 단일 기준 모듈
- MovementConfig — 이동·점프 수치
- FeelConfig — 히트스톱, 넉백, 무적, 화면 흔들림, 선딜/후딜, 입력 버퍼
- CombatFormula — 데미지 공식 (순수 static 클래스)
MonoBehaviour에 숫자 리터럴 금지.

### 조작 내역
- `execute_code(safety_checks=false)` — `Assets/_Docs/SPEC.md` 덮어쓰기 (충돌 우선순위 섹션 보존)
- `execute_code(safety_checks=false)` — `Assets/_Docs/STATE.md` 갱신 (S0 완료, 다음 단계 S1)
- 코드·씬 변경 없음

### 검증
- SPEC.md 읽기 재검증: `(작성 예정) 남은 개수=0`, `헤딩 개수=9`, `bytes=1754`
- 전문 read-back으로 지시 원문과 일치 확인 (신규 섹션 3개 포함: 미정 / 기술 기준 / 단일 기준 모듈)
- `refresh_unity` → resulting_state=idle
- `read_console(error)` → 0건

### 실패와 수정
없음

## [구현] FeelConfig ScriptableObject 생성 — 2026-08-01 04:52

### 프롬프트
[구현] FeelConfig ScriptableObject 생성
히트스톱 시간, 넉백 세기, 무적 시간, 화면 흔들림 강도, 공격 선딜/후딜, 입력 버퍼 시간.
값은 비워두고 필드만 만들어라. Assets/Settings/FeelConfig.asset 으로 생성.

### 조작 내역
- `create_script` — `Assets/Scripts/Config/FeelConfig.cs` 신규 생성
- `refresh_unity` — 컴파일
- `execute_code(safety_checks=false)` — `ScriptableObject.CreateInstance` + `AssetDatabase.CreateAsset`로 `Assets/Settings/FeelConfig.asset` 생성
- 기존 스크립트·씬·프리팹 변경 없음

### 검증
- `isCompiling=False | FeelConfig type resolved=True | base=ScriptableObject`
- `read_console(error)` → 0건
- 에셋 생성 확인: `created=True | type=FeelConfig | path=Assets/Settings/FeelConfig.asset`
- SerializedObject 순회로 직렬화 필드 10개 전부 확인, 값 전부 0 (미설정):
  hitStopDuration / knockbackForce / knockbackDuration / invincibilityDuration /
  dashInvincibilityDuration / screenShakeAmplitude / screenShakeDuration /
  attackStartupTime / attackRecoveryTime / inputBufferTime
- .asset YAML 원문 확인: `m_EditorClassIdentifier: Assembly-CSharp::FeelConfig`, 필드 10개 직렬화됨

### 실패와 수정
없음

## [구현] 검기 피격 시 깜빡이는 오브젝트 생성 — 2026-08-01 05:15

### 프롬프트
[구현] 검기를 날리면 피격을 받아 깜빡거리는 오브젝트를 맵에 생성하라

(후속 지시) 추천으로 가자 — 조사 결과 제시한 C+A안: Map/을 gitignore에서 해제하고 BiomeActionMap.unity에 배치

### 조작 내역
- `.gitignore` — `NAN2026Game/Assets/Map/` 2줄, `Assets/Map.meta` 1줄 제거 (총 3줄)
- `create_script` — `Assets/Scripts/Core/HitFlashBlinker.cs` (순수 static, UnityEngine 비의존)
- `create_script` — `Assets/Scripts/Combat/HitFlashOnSlash.cs` (MonoBehaviour)
- `create_script` — `Assets/Tests/EditMode/HitFlashBlinkerTests.cs` (테스트 8개)
- `execute_code` — `NAN2026.Core.asmdef`, `NAN2026.Tests.EditMode.asmdef` 생성
- `execute_code` — `FeelConfig.cs`에 `hitFlashDuration` / `hitFlashInterval` 2필드 추가
- `execute_code` — `FeelConfig.asset`의 깜빡임 2값만 설정 (0.3 / 0.05). 나머지 8개는 0 유지
- `execute_code` — `BiomeActionMap.unity`에 `HitFlashDummy_S1` 생성 후 씬 저장
- **`SlashProjectile.cs` 미수정** — 더미가 자기 트리거로 검기를 감지하는 구조

### 검증
- 컴파일: `isCompiling=False`, `HitFlashBlinker resolved=True asm=NAN2026.Core`,
  `HitFlashOnSlash resolved=True asm=Assembly-CSharp`, `hitFlashDuration/Interval field=True`
- EditMode 테스트: `total=8, passed=8, failed=0, skipped=0, resultState=Passed` (0.226초)
- `.gitignore` 검증: 제거 3줄, `남은 'Assets/Map' 언급: 0`,
  Player/Art/Screenshots/_Recovery/Biome 제외는 전부 유지됨
- `Map/` 내용물 확인: `.unity` 4개 + `.meta` 5개뿐, 아트 바이너리 0개 → 제3자 에셋 재배포 아님
- 씬 저장: `SaveScene=True`, `isDirty(저장후)=False`
- 배치 검증: `HitFlashDummy_S1 pos=(7.00, 1.80, 0.00)`, `feelConfig=FeelConfig`,
  `targetRenderer=연결됨`, `BoxCollider2D isTrigger=True`, `bounds=(6.50,0.80) ~ (7.50,2.80)`
- `read_console(error)`: 1건 — `Failed to store screen shot (.../NHNDemo/ShowcasePreview.png)`.
  기존 NHNDemo 스크린샷 저장 실패로 본 작업과 무관

### 실패와 수정
- EditMode 테스트 1차 실행이 `Cannot start a test run while the Editor is in or entering Play Mode`로 실패.
  재생 정지를 임의로 하지 않고 사람에게 요청 → 정지 후 재실행하여 8/8 통과
- 씬 저장 1차 시도가 `This cannot be used during play mode`로 실패. 재생이 순간적으로 걸린 상태였음.
  재생 종료 확인(`isPlaying=False`, `dummy개수=1`) 후 저장 성공. FAIL.md #5로 기록
