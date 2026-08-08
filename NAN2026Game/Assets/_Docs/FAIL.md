# FAIL.md — 과거 실패 목록

같은 실수를 반복하지 않기 위한 기록. 증상 / 원인 / 방지 규칙.

---

## 1. execute_code에서 git 실행이 차단됨
- **증상**: `Process.Start` 호출이 거부되어 git 명령을 실행할 수 없음
- **원인**: execute_code의 기본 safety_checks가 프로세스 실행을 차단
- **방지 규칙**: git을 실행하는 execute_code 호출에는 `safety_checks=false`를 반드시 명시한다

## 2. 문서 부재를 이유로 [조사]까지 중단
- **증상**: `_Docs/` 문서가 없다는 이유로 조사·설계 요청까지 멈춤
- **원인**: 문서 선행 읽기 규칙을 모든 동사에 일괄 적용
- **방지 규칙**: [조사]와 [설계]는 파일을 수정하지 않으므로 문서가 없어도 그대로 진행한다. 중단은 [구현] [수정] [복구]에만 적용

## 3. 에디터 메뉴에 의존하다 작업 중단
- **증상**: `Tools/Git/체크포인트` 메뉴가 없어 체크포인트 커밋을 못 하고 사람에게 요청하며 멈춤
- **원인**: 존재하지 않는 에디터 메뉴에 커밋을 의존
- **방지 규칙**: 커밋은 메뉴가 아니라 `execute_code(safety_checks=false)`로 직접 실행한다. 사람에게 커밋을 요청하지 않는다

## 4. git 실행 시 Unity 응답 타임아웃 반복
- **증상**: execute_code로 git을 돌리면 `Timeout receiving Unity response`가 빈발. 실행 여부를 알 수 없어 상태가 불명해짐
- **원인**: `Process.Start` + `StandardOutput.ReadToEnd()`가 Unity 메인 스레드를 블록하여 MCP 응답 시한 초과
- **방지 규칙**: git 호출 시 출력 리다이렉트를 쓰지 말고 `WaitForExit(ms)`로 exit code만 받는다. 출력이 필요하면 `cmd /c "... > 파일"`로 파일에 받은 뒤 읽는다. 타임아웃이 나면 재시도 전에 반드시 상태를 재확인한다

## 5. 재생 모드 중 씬 저장·테스트 실행 실패
- **증상**: `EditorSceneManager.SaveScene`이 `This cannot be used during play mode`로, `run_tests`가 `Cannot start a test run while the Editor is in or entering Play Mode`로 실패
- **원인**: 에디터가 재생 중이면 씬 저장과 테스트 실행이 모두 차단됨. 재생 중 만든 씬 오브젝트는 재생 종료 시 소멸
- **방지 규칙**: 씬을 건드리거나 테스트를 돌리기 전에 `EditorApplication.isPlaying`을 먼저 확인한다. 재생 중이면 자동으로 정지하고 진행한다(사용자 지시 2026-08-01). 정지 후에는 도메인 리로드 완료와 오브젝트 존재 여부를 반드시 재확인한다

## 6. 트리거끼리는 Rigidbody2D 없이 충돌하지 않음
- **증상**: 검기가 더미를 통과하는데 `OnTriggerEnter2D`가 한 번도 호출되지 않음. 콘솔 에러도 없어 원인이 드러나지 않음
- **원인**: Unity 2D 물리는 두 콜라이더 중 최소 하나에 non-static Rigidbody2D가 있어야 접촉 이벤트를 발생시킨다. 검기와 더미 모두 Collider2D만 가진 트리거였음
- **방지 규칙**: 트리거로 피격을 받는 오브젝트에는 Kinematic Rigidbody2D를 붙이고 `useFullKinematicContacts=true`로 둔다. 위치 고정이 필요하면 `constraints=FreezeAll`. 새 피격 대상을 만들 때마다 Rigidbody2D 유무를 먼저 확인한다

## 7. 기존 asmdef 미확인으로 중복 asmdef 생성, 컴파일 무력화
- **증상**: 새 asmdef 2개를 만들자 해당 폴더 어셈블리가 아예 컴파일되지 않고 테스트 0건 발견. CS 에러 필터에는 안 잡힘
- **원인**: 폴더에 이미 asmdef(NAN2026.Core, NAN2026.Tests.EditMode)가 있는데 확인 없이 같은 폴더에 새 asmdef를 생성 → 'multiple assembly definition files' 충돌
- **방지 규칙**: 스크립트·asmdef를 만들기 전에 대상 폴더와 상위 폴더의 기존 asmdef를 먼저 조회한다. 콘솔 확인은 CS 필터가 아니라 무필터 error로 본다

## 8. 시트 내 라벨 텍스트·행 병합으로 슬라이싱 오염 반복
- **증상**: 라벨 글자가 프레임에 섞여 인게임 표시, 검기가 프레임·행 경계를 침범해 포즈가 반토막
- **원인**: 생성 시트에 텍스트 라벨 포함 + 여러 애니메이션 행을 한 이미지에 배치
- **방지 규칙**: 생성 프롬프트에 텍스트 금지(NO text/labels). 이펙트가 큰 모션은 1애니메이션=1이미지로 뽑는다. 간격은 캐릭터 1인분 폭 이상

## 9. 병합 프레임 절단 시 이웃 파편 잔존
- **증상**: 슬라이스된 프레임 재생 시 좌우에 이웃 포즈 조각이 유령처럼 표시
- **원인**: 최소값 절단이 겹침 구간을 지나며 이웃 콘텐츠 일부가 rect에 포함됨
- **방지 규칙**: 병합 런을 절단한 시트는 슬라이스 직후 프레임별 연결요소 검사로 절단 경계 접촉 파편을 소거하는 후처리를 기본 적용한다

## 10. 신규 시트 캐릭터 스케일 불일치
- **증상**: 특정 모션 재생 시 캐릭터가 갑자기 커지거나 작아짐
- **원인**: 생성마다 캐릭터 픽셀 높이가 달라지는데 동일 PPU를 일괄 적용
- **방지 규칙**: 시트 임포트 시 기준 IDLE 콘텐츠 높이(447px@240)와 대조해 PPU를 프레임 실측 기반으로 산정한다

## 11. 미검증 에셋 팩 커밋으로 팀 전체 컴파일 파괴
- **증상**: Cainos 팩 커밋 후 프로젝트 열 때 Safe Mode (구식 API가 Unity 6000.5.3f1에서 에러 승격)
- **원인**: 임포트 직후 컴파일 확인 없이 커밋·공유
- **방지 규칙**: 에셋 팩은 임포트 → 콘솔 에러 0 확인 → 커밋 순서를 지킨다. 스크립트 포함 팩은 특히 주의

- #11 시트 기준선·몸통 측정에 산재 픽셀(먼지·워터마크) 오염 — 최소 y가 아니라 '폭 임계 이상 최대 연속 행 대역'으로 발끝·몸통을 실측할 것 (스킬대기 PPU 2회 오산의 원인)

## 12. 저장 전 테스트 실행으로 씬 편집 내용 소실
- **증상**: execute_code로 씬에 다수 GameObject(Grid/Tilemap/배경 등)를 만든 뒤 저장 없이 refresh_unity → run_tests(EditMode) 순으로 진행하자, 테스트 종료 후 씬이 편집 전 원본 상태로 복귀. GameObject.Find로 확인한 결과 신규 오브젝트가 전부 사라짐. git checkpoint 커밋과 최종 저장 파일이 바이트 단위로 동일해 편집이 아예 반영되지 않았음을 뒤늦게 발견
- **원인**: EditMode 테스트 실행이 씬을 리로드하면서 저장되지 않은(dirty) 변경사항을 버림. 작업 방식 SOP의 '컴파일→콘솔→테스트→저장' 순서를 스크립트 수정이 없는 순수 씬 편집 작업에도 그대로 적용한 것이 원인
- **방지 규칙**: 씬(GameObject/Tilemap 등)만 변경하고 C# 스크립트 변경이 없는 작업은 refresh_unity/run_tests 이전에 먼저 manage_scene(action=save)로 저장한다. 저장 후 파일 내용에 신규 오브젝트명이 실제로 포함되는지 텍스트로 재확인한 뒤 테스트를 실행한다. 테스트 실행 후에도 GameObject.Find로 씬 오브젝트 생존 여부를 반드시 재확인한다

## 13. 커밋 메시지용 임시 파일이 git add -A에 함께 스테이징됨
- **증상**: git commit -F용 임시 파일(_commit_msg.txt)을 프로젝트 루트에 만들고 커밋 후 삭제했는데, `git add -A`가 삭제 전 시점에 실행되어 임시 파일이 커밋 이력에 포함됨
- **원인**: 임시 파일을 저장소 내부(projRoot)에 만들고 커밋 프로세스 종료 후에야 삭제함. add→commit 사이에 파일이 여전히 디스크에 존재
- **방지 규칙**: git commit -F에 쓰는 메시지 임시 파일은 저장소 밖(OS temp 디렉터리, 예: %TEMP%)에 만든다. 저장소 내부에 임시 파일을 꼭 만들어야 한다면 git add -A 실행 전에 반드시 삭제하거나, add 범위를 -A 대신 특정 경로로 제한한다

## 14. 저장 후에도 재생모드 진입 이력으로 Tilemap 데이터가 이전 턴 상태로 부분 되돌아감
- **증상**: GameObject.Find("Grid")는 살아있고 Backdrop/Walls/Decoration 개수도 이번 턴에 만든 값과 일치하는데, Tilemap_Ground의 실제 타일 내용(GetTile)만 이전 턴에 저장했던 옛 패턴(TileGround1이 top/fill 양쪽에 중복 사용되는 구식 2단 스킴)으로 나타남. manage_scene(action=save)는 매번 성공 메시지를 반환했음
- **원인**: 정확히 특정하지 못함. 세션 도중 사용자가 에디터에서 재생모드를 실행했다가 종료한 시점이 있었던 것으로 추정되며, 재생 종료 시 GameObject 구조(계층)는 유지되지만 Tilemap 컴포넌트의 타일 데이터만 재생 시작 시점 스냅샷으로 되돌아간 것으로 보임. 또한 저장된 씬 파일 텍스트에서 타일 에셋 이름(예: "TileGround8")을 문자열로 검색하면 항상 실패함 — Tilemap의 타일 참조는 GUID/fileID 기반 바이너리 인코딩이라 텍스트 검색으로는 검증 불가능(이전 항목들에서 이 방법으로 오탐/미탐이 있었을 수 있음)
- **방지 규칙**: Tilemap을 다루는 작업은 (1) 페인트 직후 GetTile로 즉시 라이브 검증 (2) save (3) **manage_scene(action=load)로 씬을 디스크에서 강제 재로드한 뒤 다시 GetTile로 검증** — 이 세 단계를 반드시 거친다. 씬 파일을 텍스트로 열어 타일 에셋 이름을 grep하는 방식은 GameObject 이름 확인에는 유효하지만 Tilemap 타일 참조 확인에는 사용하지 않는다. 재생모드 이력이 의심되면(isPlaying 체크가 중간에 실패했거나 응답이 없었던 경우 등) 반드시 재로드 검증을 한 번 더 수행한다

## 15. col.Cast/Physics2D 다운캐스트가 트리거 콜라이더까지 지면으로 오판
- **증상**: 지면 판정에 법선(normal) 필터까지 추가했는데도 벽/경계 접촉 시 점프 카운트 리셋이 간헐적으로 실패
- **원인**: `Collider2D.Cast(dir, results, distance)` 기본 오버로드는 ContactFilter2D 없이 호출하면 Physics2D 기본 설정상 트리거 콜라이더도 결과에 포함시킨다. 카메라 경계(PolygonCollider2D, isTrigger=true) 같은 비물리 콜라이더가 결과 배열에 섞여 들어와 (1) 고정 크기 배열을 오염시켜 진짜 지면 히트를 밀어내거나 (2) 트리거의 옆방향 법선이 오판을 유발할 수 있다
- **방지 규칙**: 지면/충돌 판정용 캐스트는 항상 `ContactFilter2D`를 명시하고 `useTriggers=false`로 트리거를 제외한다. 물리 판정 버그는 가설(코드 리딩)만으로 고치지 말고, 재생 모드에서 실제 캐스트 결과(히트 콜라이더 이름·법선·거리)를 직접 찍어 확정한 뒤 수정한다
- **재발 사례 (2026-08-03)**: MiddleBossAttackPatterns.DoCharge의 벽 감지 Physics2D.Raycast도 동일한 이유로 Stage_CameraBounds 트리거에 거리 0으로 항상 걸려 돌진이 즉시 끊기는 버그 발생. 몬스터의 이동/충돌 판정 코드를 새로 짤 때마다 이 체크리스트를 먼저 적용할 것


- #16 사용자 미저장 타일 편집 소실: OpenScene(Single)·강제 Play 정지가 미저장 편집을 무경고 파괴 → 원인: 열기/정지 전 isDirty 미검사 → 방지: 모든 OpenScene·강제 정지 전 로드된 전 씬 isDirty 검사, dirty면 작업 중단하고 사용자에게 저장 여부 확인. 사용자 편집 세션 중엔 씬 전환 금지

## 16. Physics2D.IgnoreCollision은 물리 밀림만 막지, 캐스트/레이캐스트 쿼리에는 영향 없음
- **증상**: 몬스터-플레이어 IgnoreCollision을 확인하면 True인데도 실제 플레이에서는 여전히 '막힌다'고 느껴짐
- **원인**: PlayerController2D의 벽 감지(WallInDirection, Collider2D.Cast 기반)는 IgnoreCollision 설정과 무관하게 동작한다 — IgnoreCollision은 물리 시뮬레이션의 충돌 반응(밀림)만 억제할 뿐, Cast/Raycast 같은 쿼리 API의 히트 결과에는 전혀 영향을 주지 않는다. 즉 두 콜라이더가 서로 안 밀려도 캐스트로는 여전히 '보인다'
- **방지 규칙**: '몬스터/오브젝트를 안 막히게 해달라'는 요청은 IgnoreCollision 확인만으로 끝내지 말고, 이동을 제어하는 캐스트/레이캐스트 기반 로직(벽 감지, 지면 판정 등)에서도 해당 오브젝트를 제외하고 있는지 함께 확인한다. 컴포넌트(MonsterHealth 등) 또는 레이어 기반으로 캐스트 필터링에서 명시적으로 제외해야 한다

## 17. uGUI 버튼 onClick.AddListener가 씬에 EventSystem이 없으면 절대 발동 안 함
- **증상**: LevelUpSkillManager에서 Button.onClick.AddListener로 리스너를 정상적으로 붙였는데도(RemoveAllListeners 후 재등록 확인됨) 실제 클릭이 전혀 반응 안 함
- **원인**: 씬에 EventSystem 오브젝트가 아예 없었음. uGUI의 Button/GraphicRaycaster 클릭 파이프라인은 EventSystem이 있어야 마우스/터치 입력을 UI로 라우팅한다 — 리스너가 아무리 정확히 등록돼 있어도 EventSystem이 없으면 그 리스너까지 도달하는 경로 자체가 없다
- **주의**: onClick.Invoke()로 직접 호출해서 '작동한다'고 검증하면 이 문제를 못 잡는다. Invoke()는 EventSystem/GraphicRaycaster 경로를 건너뛰고 리스너를 바로 실행하기 때문. 실제 클릭 경로까지 검증하려면 UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject, pointerEventData, ExecuteEvents.pointerClickHandler)로 재현해야 한다
- **방지 규칙**: uGUI(Canvas/Button)를 쓰는 씬을 새로 만들거나 넘겨받으면 EventSystem 존재 여부를 가장 먼저 확인한다. 버튼 클릭 검증은 onClick.Invoke()가 아니라 ExecuteEvents.pointerClickHandler로 한다
- #17 입력 분기 부분 replace 시 기존 else 가지를 덮어써 기능 소실 위험 → 다분기 블록은 중괄호 매칭으로 통째 재작성하고 EditMode로 회귀 확인

- #18 큐 소비형 공격을 코드로 캔슬할 때 attackTimer만 0으로 하면 같은 프레임 attacking 로컬이 true로 남아 CanAttack 게이트가 새 큐를 막음 → attacking도 함께 false. 추측 3회보다 Debug.Log 실측이 빨랐음

- #19 진화한 파일에 기억 기준 주입 → 중복 선언. 클래스 수정 전 현재 필드·시그니처 실독 필수

- #20 재생 중 컴파일=반낡은 어셈블리 오동작 가능(패링 오인) → 증상 확인은 완전 정지→재생 / #21 UnityEngine.Object에 ?? 연산자 무효(가짜 null) → 명시적 null 체크

- #22 타일 시공 시 SetTile은 기존 칸을 무기록 덮어씀 → 사용자 작업 위 시공 금지: 빈 칸 검사 후 배치하거나 전용 타일맵 분리. 대규모 지형은 청사진 합의 후

- #23 커스텀 윗면 엣지 베이커가 신설 씬에서 미작동(푹꺼짐) — 발판은 TilemapCollider+Composite+PlatformEffector 정석 조합 사용

- timeScale 히트스톱: 복구 책임자(FX)의 수명이 히트스톱보다 짧으면 timeScale 0 영구 정지 — 히트스톱 수치 올릴 땐 FX 수명·OnDestroy 안전핀 확인

- 팀 병합이 우리 파일을 리팩터하면 기존 문자열 치환 앵커가 전멸 — 병합 직후엔 파일 실측 후 통짜 재작성 우선, '치환 성공' 보고 전 결과 문자열 검증 필수

- 입력 게이트(kb=null)로 락을 걸면 '뗌 이벤트'가 유실돼 Held 계열 상태가 갇힘 — 게이트 도입 시 모든 Held 필드에 isPressed 기반 자가 회복 필수

- 프리팹 개명 병합 후엔 씬·프리팹의 '슬롯 배선(SerializedProperty)'까지 전수 검사 — 코드 컴파일 통과와 무관하게 유령 참조가 침묵 가드에서 기능을 무음 사망시킴
