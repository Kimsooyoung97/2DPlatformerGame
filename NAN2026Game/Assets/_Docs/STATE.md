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
