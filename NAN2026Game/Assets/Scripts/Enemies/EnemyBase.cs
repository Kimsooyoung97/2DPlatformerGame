using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    /// 잡몹 공통 뼈대. 상태 판단은 EnemyStateLogic(순수)에 위임하고
    /// 여기서는 스프라이트 재생·이동·판정만 담당한다.
    public abstract class EnemyBase : MonoBehaviour, IPlayerDamageable
    {
        public EnemyConfig config;
        public Sprite[] idleFrames, walkFrames, attackFrames, hurtFrames, deathFrames;

        protected int state = EnemyStateLogic.Idle;
        protected float stateT;
        protected int hits;
        protected float nextAtk;
        protected bool dealtThisSwing;
        private float lockedFace = 1f;   // 방향 고정 시점에 얼려둔 정면. 이후 스프라이트와 판정이 같은 값을 쓴다
        protected SpriteRenderer sr;
        protected Transform player;
        private Coroutine flashCo;
        private bool flashing;      // 피격 플래시 중에는 그로기 금빛 틴트를 양보한다
        private float spawnY;
        private float patrolMinX, patrolMaxX;   // 배치 지점이 속한 '같은 단' 의 좌우 끝
        private Component parryController;                 // PlayerController2D
        private System.Reflection.MethodInfo tryParry;      // TryParry(GameObject)
        private System.Reflection.MethodInfo parryActive;   // IsParryWindowActive() — 방향 검사 없는 판정              // 배치 높이를 접지 기준으로 (config.groundY 고정 시 다층 배치 불가)
        private static readonly System.Collections.Generic.List<EnemyBase> All = new System.Collections.Generic.List<EnemyBase>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnPlay() { All.Clear(); }   // DisableDomainReload 대응

        protected virtual void Start()
        {
            if (config == null) { Debug.LogError("[" + name + "] EnemyConfig 미배선", this); enabled = false; return; }
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.useFullKinematicContacts = true; // FAIL#6
            player = PlayerLocator.FindTransform();
            if (player != null)
            {
                foreach (var mb in player.GetComponents<MonoBehaviour>())
                {
                    var m = mb.GetType().GetMethod("TryParry",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (m != null)
                    {
                        parryController = mb; tryParry = m;
                        // 잡몹은 좌우 양쪽에서 붙는다. TryParry 의 정면 판정을 우회하려고
                        // 방향을 보지 않는 IsParryWindowActive 를 함께 잡아둔다(팀 파일 무수정).
                        parryActive = mb.GetType().GetMethod("IsParryWindowActive",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        break;
                    }
                }
            }
            spawnY = transform.position.y;
            ComputePatrolBounds();
            nextAtk = Time.time + EnemyStateLogic.InitialDelay(config.fireStagger, Random.value); // 첫 발 산개
            if (!All.Contains(this)) All.Add(this);
            SetState(EnemyStateLogic.Idle);
        }

        /// 배치 지점에서 좌우로 훑어 '같은 높이의 지면' 이 이어지는 구간을 순찰 범위로 잡는다.
        /// 잡몹은 transform 으로 직접 움직여 지형 충돌이 없다 — 그래서 단차를 뚫고 지나가는 대신
        /// 애초에 자기 단 밖으로 못 나가게 막는다.
        private void ComputePatrolBounds()
        {
            float x0 = transform.position.x;
            if (config.patrolRange <= 0f) { patrolMinX = float.NegativeInfinity; patrolMaxX = float.PositiveInfinity; return; }
            patrolMinX = ProbeEdge(-1f);
            patrolMaxX = ProbeEdge(1f);
        }

        private float ProbeEdge(float sign)
        {
            float x0 = transform.position.x;
            float limit = x0;
            float stepSize = config.patrolProbeStep > 0f ? config.patrolProbeStep : 0.5f;
            for (float d = stepSize; d <= config.patrolRange; d += stepSize)
            {
                float x = x0 + sign * d;
                float surface;
                if (!GroundLevelAt(x, out surface)) break;                                    // 낭떠러지
                if (!EnemyStateLogic.SameLevel(surface, spawnY, config.patrolLevelTolerance)) break;  // 단차
                limit = x;
            }
            return limit;
        }

        /// x 위치의 지면 높이. **타일맵 지형만** 인정한다.
        /// 컴포넌트 종류로 걸러내면 팀 몬스터(KeyMonster 등)의 non-trigger 콜라이더가
        /// 지면으로 잡혀 순찰 범위가 엉뚱하게 잘린다 — 실제로 그렇게 잘렸다.
        private bool GroundLevelAt(float x, out float surfaceY)
        {
            surfaceY = 0f;
            var hits = Physics2D.RaycastAll(new Vector2(x, spawnY + 1.5f), Vector2.down, 4f);
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i].collider;
                if (c == null || c.isTrigger) continue;
                if (!(c is CompositeCollider2D) && !(c is UnityEngine.Tilemaps.TilemapCollider2D)) continue;
                surfaceY = hits[i].point.y;
                return true;
            }
            return false;
        }

        protected virtual void OnDestroy() { All.Remove(this); }

        protected virtual void Update()
        {
            if (config == null) return;
            stateT += Time.deltaTime;
            if (config.snapToGround)
            {
                var p = transform.position;
                if (!Mathf.Approximately(p.y, spawnY)) transform.position = new Vector3(p.x, spawnY, p.z);
            }

            if (state == EnemyStateLogic.Death)
            {
                Anim(deathFrames, false);
                if (stateT >= config.deathLinger) Destroy(gameObject);
                return;
            }
            if (state == EnemyStateLogic.Hurt)
            {
                Anim(hurtFrames, false);
                if (stateT >= config.hurtLock) SetState(EnemyStateLogic.Idle);
                return;
            }
            // 패링 성공 보상: 무방비로 굳는다. 이동·공격 없음, 피격으로 끊기지 않는다.
            if (state == EnemyStateLogic.Groggy)
            {
                Anim(hurtFrames, false);   // 마지막 프레임에서 멈춘 채 비틀거림
                if (sr != null && !flashing && config.groggyFlashSpeed > 0f)
                    sr.color = Color.Lerp(Color.white, config.groggyFlashColor,
                                          EnemyStateLogic.FlashPulse01(stateT, config.groggyFlashSpeed));
                if (EnemyStateLogic.GroggyFinished(stateT, config.groggyDuration))
                {
                    if (sr != null) sr.color = Color.white;
                    SetState(EnemyStateLogic.Idle);
                }
                return;
            }

            // 공격 예열: 제자리에서 색상 점멸로 경고. 이 시간이 플레이어의 반응 시간이다.
            if (state == EnemyStateLogic.Windup)
            {
                // 예열 동안은 계속 겨눈다. 여기서 안 돌면 1.2초간 옛 방향으로 굳는다
                if (player != null) sr.flipX = FlipFor(EnemyStateLogic.FaceSign(transform.position.x, player.position.x));
                Anim(idleFrames, true);
                if (sr != null && config.windupFlashSpeed > 0f)
                    sr.color = Color.Lerp(Color.white, config.windupFlashColor,
                                          EnemyStateLogic.FlashPulse01(stateT, config.windupFlashSpeed));
                if (EnemyStateLogic.WindupFinished(stateT, config.attackWindup))
                {
                    if (sr != null) sr.color = Color.white;
                    SetState(EnemyStateLogic.Attack);
                }
                return;
            }

            if (player == null) { player = PlayerLocator.FindTransform(); Anim(idleFrames, true); return; }

            // 연출(인트로·컷) 중에는 적도 멈춘다. 플레이어만 묶이면 일방적으로 맞는다.
            if (PlayerController2D.InputLocked)
            {
                if (state != EnemyStateLogic.Attack)
                {
                    if (sr != null) sr.color = Color.white;
                    SetState(EnemyStateLogic.Idle); Anim(idleFrames, true); return;
                }
            }

            float dx = Mathf.Abs(player.position.x - transform.position.x);
            float face = EnemyStateLogic.FaceSign(transform.position.x, player.position.x);
            bool faceLocked = state == EnemyStateLogic.Attack
                && EnemyStateLogic.FaceLocked(stateT / config.attackDur, FaceLockFrac);
            if (faceLocked) face = lockedFace;             // 보이는 방향과 맞는 방향을 일치시킨다
            else { lockedFace = face; sr.flipX = FlipFor(face); }

            if (state == EnemyStateLogic.Attack) { DoAttack(dx, face); return; }

            int want = EnemyStateLogic.DecideWithHold(dx, config.aggroRange, config.attackRange, Time.time >= nextAtk);
            if (want == EnemyStateLogic.Attack) { SetState(EnemyStateLogic.Windup); return; }
            if (want == EnemyStateLogic.Walk && !BlockedAhead(face))
            {
                float step = EnemyStateLogic.MoveStep(dx, config.stopDistance, config.walkSpeed, Time.deltaTime);
                if (step > 0f)
                {
                    step = EnemyStateLogic.PatrolStep(transform.position.x, step, face, patrolMinX, patrolMaxX);
                    transform.position += new Vector3(face * step, 0f, 0f);
                    Anim(walkFrames, true);
                    return;
                }
            }
            Anim(idleFrames, true);
        }

        // ===== 범위 표시 (config.showRangesInGame) — 보스와 별개 구현 =====
        // 실판정과 같은 함수(BossRangeLogic)로 좌표를 만들어 표시가 거짓말하지 않게 한다.
        private LineRenderer[] bands;
        private TextMesh rangeLabel;

        private LineRenderer MakeBand(string n, Color c, float w, int order)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.loop = true; lr.positionCount = 4;
            lr.startWidth = w; lr.endWidth = w;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = c; lr.endColor = c;
            lr.sortingOrder = order;
            return lr;
        }

        private void BuildBands()
        {
            bands = new LineRenderer[3];
            bands[0] = MakeBand("Band_Aggro", new Color(1f, 0.9f, 0.2f, 0.30f), 0.04f, 860);   // 노랑: 인지(양쪽)
            bands[1] = MakeBand("Band_Stop", new Color(0.35f, 0.7f, 1f, 0.55f), 0.05f, 861);   // 파랑: 정지거리
            bands[2] = MakeBand("Band_Attack", new Color(1f, 0.25f, 0.25f, 0.75f), 0.06f, 862); // 빨강: 타격 사거리
            var go = new GameObject("RangeLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, config.rangeBandHeight + 0.5f, 0f);
            rangeLabel = go.AddComponent<TextMesh>();
            rangeLabel.fontSize = 28; rangeLabel.characterSize = 0.05f;
            rangeLabel.anchor = TextAnchor.MiddleCenter;
            rangeLabel.color = new Color(0.85f, 1f, 0.85f);
            go.GetComponent<MeshRenderer>().sortingOrder = 863;
        }

        private void DestroyBands()
        {
            if (bands != null) { foreach (var lr in bands) if (lr != null) Destroy(lr.gameObject); bands = null; }
            if (rangeLabel != null) { Destroy(rangeLabel.gameObject); rangeLabel = null; }
        }

        private void SetRect(LineRenderer lr, float xMin, float xMax, float yMin, float yMax)
        {
            lr.SetPosition(0, new Vector3(xMin, yMin, 0f));
            lr.SetPosition(1, new Vector3(xMax, yMin, 0f));
            lr.SetPosition(2, new Vector3(xMax, yMax, 0f));
            lr.SetPosition(3, new Vector3(xMin, yMax, 0f));
        }

        private void LateUpdate()
        {
            if (config == null) return;
            if (!config.showRangesInGame) { if (bands != null || rangeLabel != null) DestroyBands(); return; }
            if (bands == null) BuildBands();

            float bx = transform.position.x;
            float y0 = spawnY, y1 = spawnY + config.rangeBandHeight;
            float face = player != null ? EnemyStateLogic.FaceSign(bx, player.position.x) : 1f;
            float dz = config.frontDeadZone;

            // 인지·정지거리는 |dx| 기준이라 좌우 대칭, 타격은 정면 판정이라 바라보는 쪽만
            SetRect(bands[0], bx - config.aggroRange, bx + config.aggroRange, y0, y1);
            SetRect(bands[1], bx - config.stopDistance, bx + config.stopDistance, y0, y0 + config.rangeBandHeight * 0.45f);
            SetRect(bands[2], BossRangeLogic.BandMinX(bx, config.attackRange, face, dz),
                              BossRangeLogic.BandMaxX(bx, config.attackRange, face, dz), y0, y0 + config.rangeBandHeight * 0.75f);

            bool win = state == EnemyStateLogic.Attack
                       && BossRangeLogic.WindowOpen(stateT / config.attackDur, config.hitWinS, config.hitWinE);
            var lr2 = bands[2];
            lr2.startWidth = lr2.endWidth = win ? 0.16f : 0.06f;
            var c = win ? new Color(1f, 1f, 0.5f, 0.95f) : new Color(1f, 0.25f, 0.25f, 0.75f);
            lr2.startColor = lr2.endColor = c;

            if (rangeLabel != null)
            {
                if (!config.showRangeLabels) { rangeLabel.text = string.Empty; return; }
                float dx = player != null ? Mathf.Abs(player.position.x - bx) : -1f;
                bool hit = player != null && BossRangeLogic.InHitBand(bx, player.position.x, config.attackRange, face, dz);
                string st = state == EnemyStateLogic.Attack ? "ATK " + (stateT / config.attackDur).ToString("F2")
                          : state == EnemyStateLogic.Walk ? "WALK"
                          : state == EnemyStateLogic.Hurt ? "HURT"
                          : state == EnemyStateLogic.Death ? "DEAD" : "IDLE";
                rangeLabel.text = string.Format("dx {0:F1} | atk {1:F1}{2} | stop {3:F1}\n{4}{5}",
                    dx, config.attackRange, hit ? "O" : "X", config.stopDistance, st, win ? "  <HIT>" : "");
            }
        }

        /// 플레이어가 패링 창을 열고 있으면 데미지를 취소하고 격돌 연출·MP 를 준다.
        /// 보스·함정과 같은 계약(PlayerController2D.TryParry)을 쓴다.
        protected bool TryParried()
        {
            if (parryController == null) return false;
            bool ok;
            if (parryActive != null)
            {
                // 전방위 패링: 등 뒤에서 때리는 잡몹도 패링된다. 다수에 둘러싸이는 구간에서
                // '눌렀는데 조용히 실패' 를 없애기 위한 것.
                object r = parryActive.Invoke(parryController, null);
                ok = r is bool && (bool)r;
            }
            else if (tryParry != null)
            {
                object r = tryParry.Invoke(parryController, new object[] { gameObject });
                ok = r is bool && (bool)r;
            }
            else return false;
            if (!ok) return false;
            if (config.clashConfig != null && player != null)
                ParryClashFx.Play((transform.position + player.position) * 0.5f + Vector3.up, config.clashConfig);
            PlayerMana.RewardParry(player);
            return true;
        }

        /// 진행 방향 앞에 같은 종류의 동료가 separation 안에 있으면 멈춘다(겹침 방지).
        protected bool BlockedAhead(float moveSign)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var o = All[i];
                if (o == null || o == this) continue;
                if (o.state == EnemyStateLogic.Death) continue;
                if (EnemyStateLogic.BlockedByNeighbor(transform.position.x, o.transform.position.x, moveSign, config.separation))
                    return true;
            }
            return false;
        }

        /// 다음 공격까지의 대기. 쿨다운에 편차를 줘 개체 간 동기화를 깬다.
        protected float NextAttackAt()
        {
            return Time.time + EnemyStateLogic.JitteredCooldown(config.attackCooldown, config.cooldownJitter, Random.value);
        }

        /// 시트 기본 바라보는 방향에 따라 반전 규칙이 다르다.
        protected abstract bool FlipFor(float face);

        /// 공격 진행. 타격 시간창에서 ResolveHit(), 발사형은 오버라이드.
        protected virtual void DoAttack(float dx, float face)
        {
            Anim(attackFrames, false, SwingFps);
            float frac = stateT / config.attackDur;
            int act = EnemyStateLogic.SwingResolve(frac, config.hitWinS, config.hitWinE, dealtThisSwing);
            bool inBand = BossRangeLogic.InHitBand(transform.position.x, player.position.x, config.attackRange, face, config.frontDeadZone,
                                                   transform.position.y, player.position.y, config.attackHeight);
            // 판정을 창의 첫 프레임이 아니라 창 끝에서 내린다.
            // 창이 열려 있는 동안은 매 프레임 패링만 접수하고(성공하면 즉시 종료),
            // 창이 닫히는 프레임에 패링이 없었으면 그때 데미지를 준다.
            if (act != 0)
            {
                if (inBand && TryParried()) { EnterGroggy(); return; }   // 패링 성공 — 보상 구간
                if (act == 2)
                {
                    dealtThisSwing = true;
                    if (inBand) player.SendMessage("TakeDamage", (float)config.damage, SendMessageOptions.DontRequireReceiver);
                }
            }
            if (frac >= 1f) { nextAtk = NextAttackAt(); SetState(EnemyStateLogic.Idle); }
        }

        /// 패링당했을 때의 보상 구간. 그로기가 끝나고도 바로 때리지 못하게 쿨다운을 얹는다.
        protected void EnterGroggy()
        {
            nextAtk = NextAttackAt() + config.groggyDuration;
            SetState(EnemyStateLogic.Groggy);
        }

        public void TakeDamage(int amount)
        {
            if (state == EnemyStateLogic.Death) return;
            if (sr != null) sr.color = Color.white;   // 예열 점멸 색이 남지 않게
            hits++;
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(FlashRed());
            if (EnemyStateLogic.IsDead(hits, config.hitsToDie)) { SetState(EnemyStateLogic.Death); return; }
            if (state == EnemyStateLogic.Groggy) return;   // 보상 구간을 때려서 끊어먹지 않게
            SetState(EnemyStateLogic.Hurt);
        }

        private System.Collections.IEnumerator FlashRed()
        {
            if (sr == null) yield break;
            flashing = true;
            sr.color = new Color(1f, 0.4f, 0.4f);
            yield return new WaitForSeconds(config.hitFlash);
            if (sr != null) sr.color = Color.white;
            flashing = false;
            flashCo = null;
        }

        /// 공격 모션 중 방향을 고정하기 시작하는 지점(frac).
        /// 기본은 타격창이 열리는 순간 — 칼이 나가는 방향으로 굳는다.
        protected virtual float FaceLockFrac { get { return config.hitWinS; } }

        protected virtual void SetState(int s) { state = s; stateT = 0f; dealtThisSwing = false; }

        protected void Anim(Sprite[] arr, bool loop) { Anim(arr, loop, config.fps); }

        protected void Anim(Sprite[] arr, bool loop, float fps)
        {
            if (arr == null || arr.Length == 0 || sr == null) return;
            sr.sprite = arr[EnemyStateLogic.AnimIndex(stateT, fps, arr.Length, loop)];
        }

        /// 공격 모션 재생 fps (config.attackFps 우선, 0 이면 공용 fps)
        protected float SwingFps { get { return EnemyStateLogic.AttackFps(config.attackFps, config.fps); } }
    }
}
