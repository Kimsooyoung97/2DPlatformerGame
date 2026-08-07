using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NAN2026;

namespace NAN2026.Showroom
{
    // Undead Executioner 보스.
    // Dormant(평소 idle) → 감지 → Combat(idle2) → 근접 attacking / 주기 summon(유도탄 3기)
    // 패링 5회 → 그로기(받는 데미지 1.5배, 반짝임) → 회복. HP 0 → death.
    public class ExecutionerBoss : MonoBehaviour
    {
        public enum State { Dormant, Combat, Attack, Summon, Groggy, Death }

        [Header("Sprites")]
        public Sprite[] idlePassiveFrames;
        public Sprite[] idleCombatFrames;
        public Sprite[] attackFrames;
        public Sprite[] summonFrames;
        public Sprite[] deathFrames;
        public Sprite groggySprite;
        public Sprite[] spiritAppearFrames;
        public Sprite[] spiritIdleFrames;

        [Header("Stats")]
        public int maxHealth = 14;
        public float aggroRadius = 9f;
        public float meleeRange = 2.6f;
        public float attackCooldown = 2.4f;
        public float summonCooldown = 10f;
        public int parriesForGroggy = 5;
        public float groggyDuration = 4.5f;
        public float groggyDamageMultiplier = 1.5f;
        public int meleeDamage = 1;
        public int spiritDamage = 1;
        public float spiritLaunchInterval = 2f;
        public float spiritFloatTime = 1.5f;
        public float spiritSpeed = 3.5f;

        [Header("Anim")]
        public float idleFps = 6f;
        public float attackFps = 14f;
        public float windupFps = 6f;   // 예비 동작(0~3프레임) 속도
        public float summonFps = 8f;
        public float deathFps = 10f;
        public bool spriteFacesLeft = false;   // Kronovi 시트는 기본 우향
        public int attacksPerSummon = 4;
        public float spiritScale = 2f;

        [Header("Trigger")]
        public bool aggroByProximity = true;

        private State state = State.Dormant;
        private SpriteRenderer sr;
        private Transform player;
        private float animT;
        private float attackCd;
        private float summonCd;
        private float groggyT;
        private int hp;
        private int parryCount;
        private TextMesh pips;
        private readonly List<SpiritMissile> spirits = new List<SpiritMissile>();
        private float launchTimer;
        private bool strike1Done, strike2Done, summonSpawned, telegraphed;
        private int attackCounter;
        private float attackDir = -1f;

        public bool IsGroggy { get { return state == State.Groggy; } }
        public State CurrentState { get { return state; } }
        public int Hp { get { return hp; } }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            hp = maxHealth;
            summonCd = summonCooldown * 0.5f;
            var pc = FindAnyObjectByType<PlayerController2D>();
            if (pc != null) player = pc.transform;
            CreatePips();
            UpdatePips();
        }

        private void CreatePips()
        {
            var go = new GameObject("ParryPips");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            pips = go.AddComponent<TextMesh>();
            pips.characterSize = 0.14f;
            pips.fontSize = 48;
            pips.anchor = TextAnchor.MiddleCenter;
            pips.color = new Color(1f, 0.9f, 0.3f);
            pips.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pips.GetComponent<MeshRenderer>().material = pips.font.material;
            pips.GetComponent<MeshRenderer>().sortingOrder = 50;
        }

        private void UpdatePips()
        {
            if (pips == null) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < parriesForGroggy; i++) sb.Append(i < parryCount ? '◆' : '◇');
            pips.text = sb.ToString();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            animT += dt;

            if (state == State.Death)
            {
                int df = (int)(animT * deathFps);
                sr.sprite = deathFrames[Mathf.Min(df, deathFrames.Length - 1)];
                return;
            }
            if (player == null) return;
            float dist = Mathf.Abs(player.position.x - transform.position.x);
            if (state == State.Attack) ApplyFacing(attackDir);
            else if (state != State.Groggy) FacePlayer();

            switch (state)
            {
                case State.Dormant:
                    Loop(idlePassiveFrames, idleFps);
                    if (aggroByProximity && Vector2.Distance(player.position, transform.position) < aggroRadius) Enter(State.Combat);
                    break;

                case State.Combat:
                    Loop(idleCombatFrames, idleFps);
                    attackCd -= dt;
                    if (attackCounter >= attacksPerSummon) { attackCounter = 0; summonSpawned = false; Enter(State.Summon); }
                    else if (dist < meleeRange && attackCd <= 0f)
                    {
                        strike1Done = strike2Done = telegraphed = false;
                        attackDir = player.position.x > transform.position.x ? 1f : -1f;
                        Enter(State.Attack);
                    }
                    break;

                case State.Attack:
                {
                    // 예비 동작(0~3프레임)은 느리게, 정점에서 번쩍+경고, 스윙(4~)은 빠르게
                    float windupDur = 4f / windupFps;
                    int f;
                    if (animT < windupDur)
                    {
                        f = (int)(animT * windupFps);
                        if (!telegraphed && f >= 3) { telegraphed = true; StartCoroutine(TelegraphFlash()); }
                    }
                    else
                    {
                        f = 4 + (int)((animT - windupDur) * attackFps);
                    }
                    sr.sprite = attackFrames[Mathf.Min(f, attackFrames.Length - 1)];
                    if (!strike1Done && f >= 5) { strike1Done = true; SpawnStrike(); }
                    if (!strike2Done && f >= 9) { strike2Done = true; SpawnStrike(); }
                    if (f >= attackFrames.Length) { attackCd = attackCooldown; attackCounter++; Enter(State.Combat); }
                    break;
                }

                case State.Summon:
                {
                    int f = (int)(animT * summonFps);
                    sr.sprite = summonFrames[Mathf.Min(f, summonFrames.Length - 1)];
                    if (!summonSpawned && f >= summonFrames.Length - 1) { summonSpawned = true; SpawnSpirits(); }
                    if (f >= summonFrames.Length) { summonCd = summonCooldown; Enter(State.Combat); }
                    break;
                }

                case State.Groggy:
                    groggyT -= dt;
                    if (groggySprite != null) sr.sprite = groggySprite;
                    else Loop(idleCombatFrames, idleFps * 0.5f);
                    float blinkSpeed = groggyT < 1.2f ? 14f : 7f;
                    float k = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                    sr.color = Color.Lerp(Color.white, new Color(1f, 0.95f, 0.35f), k);
                    if (groggyT <= 0f) EndGroggy();
                    break;
            }

            // 떠 있는 소환수 순차 발사
            if (state != State.Groggy && spirits.Count > 0)
            {
                launchTimer -= dt;
                if (launchTimer <= 0f)
                {
                    while (spirits.Count > 0)
                    {
                        var s = spirits[0];
                        spirits.RemoveAt(0);
                        if (s != null) { s.LaunchHoming(spiritSpeed); break; }
                    }
                    launchTimer = spiritLaunchInterval;
                }
            }
        }

        private void Enter(State s) { state = s; animT = 0f; }

        private void Loop(Sprite[] frames, float f)
        {
            if (frames == null || frames.Length == 0) return;
            sr.sprite = frames[(int)(animT * f) % frames.Length];
        }

        private void FacePlayer()
        {
            ApplyFacing(player.position.x > transform.position.x ? 1f : -1f);
        }

        private void ApplyFacing(float dir)
        {
            sr.flipX = spriteFacesLeft ? dir > 0f : dir < 0f;
        }

        private IEnumerator TelegraphFlash()
        {
            FloatingText.Spawn(transform.position + Vector3.up * 2.6f, "!", new Color(1f, 0.3f, 0.25f));
            for (int i = 0; i < 2; i++)
            {
                if (state != State.Attack) yield break;
                sr.color = new Color(1f, 0.55f, 0.45f);
                yield return new WaitForSeconds(0.07f);
                sr.color = Color.white;
                yield return new WaitForSeconds(0.05f);
            }
        }

        private void SpawnStrike()
        {
            float dir = attackDir;
            var go = new GameObject("Executioner_Strike");
            go.transform.position = transform.position + new Vector3(dir * 1.7f, 1.0f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.4f, 1.9f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            go.AddComponent<MeleeStrike>().Init(this, meleeDamage, 0.30f);
        }

        private void SpawnSpirits()
        {
            Vector3[] offs = { new Vector3(-1.7f, 2.4f, 0f), new Vector3(0f, 3.1f, 0f), new Vector3(1.7f, 2.4f, 0f) };
            foreach (var off in offs)
            {
                var go = new GameObject("Executioner_Spirit");
                go.transform.position = transform.position + off;
                go.transform.localScale = Vector3.one * spiritScale;
                var srr = go.AddComponent<SpriteRenderer>();
                srr.sortingOrder = 2;
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.45f;
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                var sm = go.AddComponent<SpiritMissile>();
                sm.Init(this, player, off, spiritAppearFrames, spiritIdleFrames, spiritDamage);
                spirits.Add(sm);
            }
            launchTimer = spiritFloatTime;
        }

        public void ForceCombat()
        {
            if (state == State.Dormant) Enter(State.Combat);
        }

        public void RegisterParry()
        {
            if (state == State.Death || state == State.Groggy) return;
            parryCount++;
            UpdatePips();
            if (parryCount >= parriesForGroggy) StartGroggy();
        }

        private void StartGroggy()
        {
            foreach (var s in spirits) if (s != null) Destroy(s.gameObject);
            spirits.Clear();
            groggyT = groggyDuration;
            Enter(State.Groggy);
            FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "GROGGY!", new Color(1f, 0.85f, 0.2f));
            StartCoroutine(HitStop());
        }

        private IEnumerator HitStop()
        {
            float prev = Time.timeScale;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(0.18f);
            Time.timeScale = prev;
        }

        private void EndGroggy()
        {
            parryCount = 0;
            UpdatePips();
            sr.color = Color.white;
            attackCd = 1f;
            summonCd = summonCooldown * 0.6f;
            Enter(State.Combat);
        }

        public void TakeHit(int damage, float dir)
        {
            if (state == State.Death) return;
            int final = state == State.Groggy ? Mathf.RoundToInt(damage * groggyDamageMultiplier) : damage;
            final = Mathf.Max(1, final);
            hp -= final;
            FloatingText.Spawn(transform.position + Vector3.up * 2.2f, final.ToString(),
                state == State.Groggy ? new Color(1f, 0.85f, 0.2f) : Color.white);
            if (state != State.Groggy) StartCoroutine(HitFlash());
            if (state == State.Dormant) Enter(State.Combat);
            if (hp <= 0) Die();
        }

        private IEnumerator HitFlash()
        {
            sr.color = new Color(1f, 0.35f, 0.3f);
            yield return new WaitForSeconds(0.1f);
            if (state != State.Groggy) sr.color = Color.white;
        }

        private void Die()
        {
            foreach (var s in spirits) if (s != null) Destroy(s.gameObject);
            spirits.Clear();
            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
            if (pips != null) Destroy(pips.gameObject);
            sr.color = Color.white;
            Enter(State.Death);
        }
    }
}
