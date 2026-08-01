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
