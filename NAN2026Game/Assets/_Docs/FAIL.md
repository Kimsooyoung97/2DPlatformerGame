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
