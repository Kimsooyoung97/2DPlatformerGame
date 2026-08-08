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

<<<<<<< HEAD
- Effect_Vol.3 (에셋 팩, Assets/Effect_Vol.3): Effect_1.png 9프레임을 플레이어 스킬 내려찍기 이펙트로 사용. 라이선스: 팀 도입 팩(확인 필요 시 팀 문의)
- 기사_스킬대기.png (생성형 AI 산출물, Assets/Map): 플레이어 스킬 모션용. 생성 프롬프트 미제공 — 불투명 배경으로 슬라이싱 보류 중

- Forest Platformer Pixel Art Tileset (sanctum pixel, Assets/sanctum_pixel/forest_side_pack): 에셋스토어 구매. 재배포 불가 — gitignore 등재, 각 팀원 개별 임포트 필요. 숲 타일 27종·패럴랙스 배경
=======
## sanctum_pixel/forest_side_pack (2026-08-02)
- 파일: Assets/sanctum_pixel/forest_side_pack/** (Sprites/Background/{sky,sky_cloud,cloud,mountain,pine1,pine2}.png, Sprites/Tileset/forest_tileset.png + Palette 27종, Sprites/Props/{Bush,Pine,Pine_dead,Rock,Tree,Tree_dead,Upper_grass,Flower(5색)}
- 출처: 프로젝트에 기존 임포트되어 있던 에셋(누가/언제 추가했는지 이번 세션에서는 확인 불가). 사용자 승인 하에 FirstScene 배경(Ground/Platforms/Backdrop/Decoration)에 신규 사용
- 라이선스: 미확인 (두 Biome 팩과 동일하게 라이선스 확인 전까지 git 커밋에서 제외함)
- 생성 프롬프트: 해당 없음 (기성 에셋, AI 생성 아님)
>>>>>>> 707c9ff87baa8d651e7b4c541f07bd73fdeaf1c4

- 기사_스킬대기.png·기사_패링(Knight_Parry).png 수정본 교체(2026-08-03): 동일 그림, 생성 단계에서 배경 세심 제거 — 재조립·재분할로 반영

- Knight_MoveExtra(기사_걷기,점프,착지).png 수정본 교체(2026-08-03): 배경 세심 제거본

- Assets/Audio/BGM/ 4곡(게임시작BGM·감옥 입장·전투소리·공주 만남.mp3, 2026-08-04): 사용자 제공 — 출처·라이선스 기입 필요(제출 전 확인)

- 돌무더기붕괴.wav(2026-08-04): 사용자 AI 생성 SFX — 서비스·프롬프트 기입 필요

- 진동소음2.wav(2026-08-04): 사용자 AI 생성 SFX — 서비스·프롬프트 기입 필요

- 발소리1~3.wav·검기발사1.wav(2026-08-04): 사용자 ElevenLabs 생성 SFX — 프롬프트 기입 필요

- RuinedCastleWall.png(2026-08-05): 팀 AI 생성 타일셋(카톡 수신) — 생성자·프롬프트 기입 필요

- Sprites_AI/FX/SlashFX_White456.png — SlashFX FREE v1.2 (7.13.23) GameEndeavor 흰색 프레임 합성 | 라이선스: 팩 동봉 문서 확인 요

- Sprites_AI/MidBoss/idle_1~8.png — Elementals: Fire Knight FREE v1.1 (chierit) | 라이선스: 배포 페이지 확인 요

- Map/Showroom/Medieval/* — GandalfHardcore FREE Pixel Art Sidescroller Asset Pack 32x32 Overworld (itch.io) | 라이선스: 상업·비상업 게임 사용/수정 허용, 재배포·AI학습·NFT 금지 (READ ME.txt 동봉 확인) | 사용: 타일 380·배경 5레이어·나무 3종

- Map/Showroom/Medieval/(장식류: 나무·텐트·조각상·분재·광석 등) — GandalfHardcore FREE Platformer Assets 동일 팩 | 동일 라이선스

- Map/Showroom/Medieval/WaterTiles·WaterAnimSheet·Pine Trees — GandalfHardcore 동일 팩 | 동일 라이선스


## 투척무기 SFX 4종 — 2026-08-08
- 파일: 도끼착탄.wav, 발사기전조_철컥1.wav, 발사기전조_철컥2.wav, 화살1.wav, 회전무기.wav (Assets/Audio/Effect/투척무기/)
- 출처: ElevenLabs SFX 생성 (동현 생성)
- 라이선스: ElevenLabs 구독 약관에 따른 상업 이용
- 생성 프롬프트:
  - 화살1: "Bow string twang followed by fast arrow whoosh flying past, sharp and quick, retro action game, 0.5 seconds"
  - 발사기전조_철컥1: "Mechanical trap arming click, metal latch tension winding up, short ominous clank in a stone dungeon, slight echo, 0.7 seconds"
  - 회전무기: "Spinning blade whirling through the air, rhythmic whoosh-whoosh rotation, metallic edge, seamless loop, 1.5 seconds"
  - 도끼착탄: "Heavy axe blade slamming into stone wall, deep thunk with metallic ring, dungeon reverb, 0.8 seconds"


## 미노타우르스 보스 (mino_v1.1_full) — 2026-08-08
- 파일: Assets/Art/Mino/ (idle16·walk12·atk_1x16·atk_2x14·take_hit7·death19 + 체력바 UI 3장)
- 출처: 팀 확보 에셋 팩 (원드라이브 '공주를 구하라/mino_v1.1_full')
- 라이선스: 팩 동봉 라이선스 준수 — 원 배포처 확인 필요 시 팀 문의

- Assets/Art/Demon/** (130장): boss_demon_final(boss_demon_slime_FULL_v1.0) 유래 — Sprites_AI 폴더(생성형 AI 산출물, 사용자 반입). 데몬 보스 전 동작+투사체. 생성 프롬프트: 사용자 보관분
