# ASSET_CREDITS.md — 외부 에셋 및 생성형 AI 산출물

| 파일명 | 출처 | 라이선스 | 프롬프트 |
|---|---|---|---|
| `Assets/Player/DeadRevolver/PixelPrototypePlayerSprites/` | Dead Revolver — Pixel Prototype Player Sprites (Unity Asset Store) | 상용. 재배포 불가 — 빌드 산출물에만 포함. 저장소에서 제외(.gitignore) | — |

## Knight_SpriteSheet.png — 2026-08-01
- 파일: Assets/Sprites_AI/Player/Knight_SpriteSheet.png (원본: 기사_스프라이트시트.png, 사용자 데스크톱)
- 출처: Google Nano Banana 2 (Gemini) 생성, 사용자 직접 생성
- 라이선스: 사용자 생성 AI 산출물. NAN 2026 생성형 AI 규정 확인 필요 (사용자 확인 대기)
- 생성 프롬프트: Claude 작성 5행 스프라이트 시트 프롬프트 (IDLE/WALK/RUN/SLASH/COMBO, 참조 이미지 기반 스타일 매칭). 원문은 대화 기록 참조
- 후처리: 배경 플러드필 투명화(Claude, Unity 내 처리), 워터마크는 이 시트에선 캐릭터 비간섭 확인

## Knight_SpriteSheet.png(교체) / Knight_AttackSheet.png(신규) — 2026-08-01
- 파일: Assets/Sprites_AI/Player/Knight_SpriteSheet.png(이동 3행 재생성본), Knight_AttackSheet.png(공격 3행)
- 출처: Google Nano Banana 2, 사용자 생성. 얇은 암갈색 아웃라인·프레임 간격 확보·과장된 공격 모션 프롬프트(Claude 작성) 사용
- 라이선스: 사용자 생성 AI 산출물, NAN 2026 규정 확인 대기
- 후처리: 외곽 플러드필 투명화 (시트1 3,081,182px / 시트2 2,879,401px)

## Knight_MoveExtra.png / Knight_Combo2.png — 2026-08-01
- 파일: Assets/Sprites_AI/Player/Knight_MoveExtra.png (걷기4·점프4·착지5), Knight_Combo2.png (보류: 레이아웃 불일치)
- 출처: Google Nano Banana 2, 사용자 생성. 안무 지정·글자 금지 프롬프트(Claude 작성)
- 라이선스: 사용자 생성 AI 산출물, NAN 2026 규정 확인 대기
- 후처리: 외곽 플러드필 투명화

## AttackEffects.png — 2026-08-01
- 파일: Assets/Sprites_AI/Effects/AttackEffects.png (원본: 공격 이펙트.png)
- 출처: 사용자 생성 AI 산출물 (생성 도구·프롬프트 미보고 — 사용자 확인 필요)
- 라이선스: NAN 2026 규정 확인 대기
- 후처리: 가짜 체커보드 배경 제거(무채색+밝음 플러드필 475,048px), 소형 잔해 2,958개 제거, 4행 22프레임 슬라이스

## 공주 보스 시트 5종 — 2026-08-01
- 파일: Assets/Sprites_AI/Boss/Princess_Idle1/Trans1/Trans2/Trans3/Idle2.png
- 출처: 사용자 생성 AI 산출물 (공주 원본: Higgsfield AI 표기 확인. 각 시트 생성 프롬프트: Claude 작성)
- 라이선스: Higgsfield 및 NAN 2026 생성형 AI 규정 확인 필요 (사용자 확인 대기)
- 후처리: 플러드필 투명화, 병합 런 분할 슬라이스 (Trans2는 6f 지시였으나 5f로 생성 확인)

## Knight_Parry.png / BossOrb.png — 2026-08-01
- Knight_Parry.png: 사용자 생성 AI 산출물(나노바나나2, Claude 프롬프트), 파편 소거 후처리
- BossOrb.png: Claude 절차 생성(코드로 그린 분홍 구슬 48px, 임시 — 아트 교체 예정)

## Knight_Roll.png — 2026-08-01
- 파일: Assets/Sprites_AI/Player/Knight_Roll.png (9프레임 구르기)
- 출처: 사용자 생성 AI 산출물, 슬라이스·PPU 보정·클립·컨트롤러 연결 전부 사용자 수작업
- 라이선스: NAN 2026 생성형 AI 규정 확인 대기

## Cainos 에셋 팩 / Map_Castle — 2026-08-01
- Assets/Cainos/ (Pixel Art Platformer - Village Props, Interactive Pixel Water 등): 유니티 에셋 스토어, 표준 라이선스 (사용자 다운로드)
- Assets/Sprites_AI/Map_Castle/: 사용자 생성 AI 산출물 (성 안뜰 구간용)
