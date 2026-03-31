using System.Linq;
using UnityEngine;
using KindredSiege.AI.BehaviourTree;

namespace KindredSiege.Battle
{
    /// <summary>
    /// Factory for all Pre-Built Gambit behaviour tree branches (GDD §4.1).
    ///
    /// Each method returns a BTNode (Sequence or Selector) ready to be injected
    /// at Priority 1 or 2 in a unit's behaviour tree via UnitController.SetGambits().
    ///
    /// The unit's default class behaviour remains as a fallback below all gambits.
    /// Gambits are pure AI configurations — no visual node editor required.
    /// </summary>
    public static class GambitLibrary
    {
        // ════════════════════════════════════════════════════════
        // LOOKUP
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Return the BTNode branch for a given gambit type.
        /// Returns null for GambitType.None (no injection).
        /// </summary>
        public static BTNode GetGambit(GambitType type)
        {
            return type switch
            {
                // Warden
                GambitType.Warden_HoldTheLine        => Warden_HoldTheLine(),
                GambitType.Warden_ProtectTheWeak      => Warden_ProtectTheWeak(),
                GambitType.Warden_HuntTheLeader       => Warden_HuntTheLeader(),
                GambitType.Warden_RecklessAbandon     => Warden_RecklessAbandon(),

                // Marksman
                GambitType.Marksman_KeepDistance      => Marksman_KeepDistance(),
                GambitType.Marksman_FocusFire         => Marksman_FocusFire(),
                GambitType.Marksman_SuppressiveShots  => Marksman_SuppressiveShots(),
                GambitType.Marksman_PrecisionTarget   => Marksman_PrecisionTarget(),

                // Occultist
                GambitType.Occultist_TendTheWounded   => Occultist_TendTheWounded(),
                GambitType.Occultist_PsychicBarrier   => Occultist_PsychicBarrier(),
                GambitType.Occultist_DespairWave      => Occultist_DespairWave(),
                GambitType.Occultist_SacredRite       => Occultist_SacredRite(),

                // Berserker
                GambitType.Berserker_BloodFrenzy      => Berserker_BloodFrenzy(),
                GambitType.Berserker_DeathCharge      => Berserker_DeathCharge(),
                GambitType.Berserker_BerserkRage      => Berserker_BerserkRage(),
                GambitType.Berserker_HuntThePack      => Berserker_HuntThePack(),

                // Investigator
                GambitType.Investigator_ObserveReport   => Investigator_ObserveReport(),
                GambitType.Investigator_WeaknessExploit => Investigator_WeaknessExploit(),
                GambitType.Investigator_ForbiddenScan   => Investigator_ForbiddenScan(),
                GambitType.Investigator_TacticalWithdraw=> Investigator_TacticalWithdraw(),

                // Shadow
                GambitType.Shadow_AssassinProtocol    => Shadow_AssassinProtocol(),
                GambitType.Shadow_VanishAndStrike     => Shadow_VanishAndStrike(),
                GambitType.Shadow_LeaderAssassination => Shadow_LeaderAssassination(),
                GambitType.Shadow_GhostStep           => Shadow_GhostStep(),

                // Herald
                GambitType.Herald_SanityBeacon        => Herald_SanityBeacon(),
                GambitType.Herald_BattleCry           => Herald_BattleCry(),
                GambitType.Herald_LastRites           => Herald_LastRites(),
                GambitType.Herald_MartyrSignal        => Herald_MartyrSignal(),

                // Vessel
                GambitType.Vessel_GhostProtocol       => Vessel_GhostProtocol(),
                GambitType.Vessel_SacredWard          => Vessel_SacredWard(),
                GambitType.Vessel_DesperatePlea       => Vessel_DesperatePlea(),

                _ => null
            };
        }

        // ════════════════════════════════════════════════════════
        // WARDEN GAMBITS
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Hold the Line — stay at spawn position, attack anything in range, never advance.
        /// The Warden becomes an immovable anchor: enemies must come to them.
        /// </summary>
        static BTNode Warden_HoldTheLine()
        {
            return new Sequence("HoldTheLine",
                new HasEnemies(),
                new Selector("HoldOrReturn",
                    // Attack any enemy already in range — don't chase
                    new Sequence("AttackInPlace",
                        new FindNearestEnemy(),
                        new IsTargetInRange(),
                        new AttackTarget()
                    ),
                    // Drift back to spawn if pushed away
                    new ActionNode("ReturnToSpawn", ctx =>
                    {
                        Vector3 home = ctx.Owner.SpawnPosition;
                        float dist = Vector3.Distance(ctx.Owner.transform.position, home);
                        if (dist > 0.5f)
                        {
                            Vector3 dir = (home - ctx.Owner.transform.position).normalized;
                            ctx.Owner.transform.position += dir * ctx.Owner.MoveSpeed * ctx.DeltaTime;
                            return NodeState.Running;
                        }
                        return NodeState.Success;
                    })
                )
            );
        }

        /// <summary>
        /// Protect the Weak — when any ally is below 30% HP, rush to their side and draw aggro.
        /// </summary>
        static BTNode Warden_ProtectTheWeak()
        {
            return new Sequence("ProtectTheWeak",
                new Condition("AllyInDanger", ctx =>
                    ctx.Allies.Any(a => a != null && a.IsAlive && a != ctx.Owner
                        && (float)a.CurrentHP / a.MaxHP < 0.30f)
                ),
                new ActionNode("RushToProtect", ctx =>
                {
                    var endangered = ctx.Allies
                        .Where(a => a != null && a.IsAlive && a != ctx.Owner
                            && (float)a.CurrentHP / a.MaxHP < 0.30f)
                        .OrderBy(a => (float)a.CurrentHP / a.MaxHP)
                        .First();

                    float dist = Vector3.Distance(ctx.Owner.transform.position, endangered.transform.position);
                    if (dist > 1.5f)
                    {
                        Vector3 dir = (endangered.transform.position - ctx.Owner.transform.position).normalized;
                        ctx.Owner.transform.position += dir * ctx.Owner.MoveSpeed * ctx.DeltaTime;
                        return NodeState.Running;
                    }

                    // In position — attack the nearest enemy threatening that ally
                    var threat = ctx.Enemies
                        .Where(e => e != null && e.IsAlive)
                        .OrderBy(e => Vector3.Distance(e.transform.position, endangered.transform.position))
                        .FirstOrDefault();

                    if (threat != null)
                    {
                        ctx.Set("Target", threat);
                        ctx.Owner.PerformAttack(threat);
                    }
                    return NodeState.Success;
                })
            );
        }

        /// <summary>
        /// Hunt the Leader — fixate on the highest-HP enemy (rival proxy), ignore all others.
        /// </summary>
        static BTNode Warden_HuntTheLeader()
        {
            return new Sequence("HuntTheLeader",
                new HasEnemies(),
                new FindHighestHPEnemy(),
                new Selector("ApproachLeader",
                    new Sequence("StrikeLeader", new IsTargetInRange(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        /// <summary>
        /// Reckless Abandon — ignore retreat thresholds, charge nearest, +15% damage, -5 sanity/tick.
        /// Modifies GambitDamageMultiplier on the owner unit.
        /// </summary>
        static BTNode Warden_RecklessAbandon()
        {
            return new Sequence("RecklessAbandon",
                new HasEnemies(),
                new ActionNode("AbandonSelf", ctx =>
                {
                    ctx.Owner.GambitIgnoreRetreat    = true;
                    ctx.Owner.GambitDamageMultiplier = 1.15f;

                    // Sanity cost per tick (Dark Gambit)
                    float procRate = 5f; // ~5 sanity/sec
                    if (KindredSiege.City.MythosExposure.Instance != null &&
                        KindredSiege.City.MythosExposure.Instance.Exposure >= 26) // Scholar Tier or higher
                    {
                        procRate = 3f; // ~3 sanity/sec (Discount applied)
                    }

                    if (Random.value < procRate * ctx.DeltaTime)
                        ctx.Owner.ModifySanity(-1, "RitualGambit");

                    return NodeState.Success; // Let rest of tree run
                }),
                new FindNearestEnemy(),
                new Selector("RecklessCharge",
                    new Sequence("AttackIfInRange", new IsTargetInRange(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        // ════════════════════════════════════════════════════════
        // MARKSMAN GAMBITS
        // ════════════════════════════════════════════════════════

        /// <summary>Keep Distance — always maintain maximum attack range, never close in.</summary>
        static BTNode Marksman_KeepDistance()
        {
            return new Sequence("KeepDistance",
                new HasEnemies(),
                new FindNearestEnemy(),
                new ActionNode("MaintainMaxRange", ctx =>
                {
                    var target = ctx.Get<UnitController>("Target");
                    if (target == null) return NodeState.Failure;

                    float dist = Vector3.Distance(ctx.Owner.transform.position, target.transform.position);
                    float maxRange = ctx.Owner.AttackRange;

                    if (dist < maxRange * 0.85f)
                    {
                        // Too close — back away
                        Vector3 away = (ctx.Owner.transform.position - target.transform.position).normalized;
                        ctx.Owner.transform.position += away * ctx.Owner.MoveSpeed * ctx.DeltaTime;
                        return NodeState.Running;
                    }

                    // In range — shoot
                    if (ctx.Owner.CanAttack())
                    {
                        ctx.Owner.PerformAttack(target);
                        return NodeState.Success;
                    }
                    return NodeState.Running;
                })
            );
        }

        /// <summary>Focus Fire — lock on a single target until it dies, then pick the next.</summary>
        static BTNode Marksman_FocusFire()
        {
            return new Sequence("FocusFire",
                new HasEnemies(),
                new ActionNode("LockTarget", ctx =>
                {
                    var locked = ctx.Get<UnitController>("FocusLock");
                    if (locked == null || !locked.IsAlive)
                    {
                        // Pick weakest living enemy as new lock
                        locked = ctx.Enemies
                            .Where(e => e != null && e.IsAlive)
                            .OrderBy(e => e.CurrentHP)
                            .FirstOrDefault();

                        if (locked == null) return NodeState.Failure;
                        ctx.Set("FocusLock", locked);
                    }
                    ctx.Set("Target", locked);
                    return NodeState.Success;
                }),
                new Selector("ShootLocked",
                    new Sequence("ShootIfInRange", new IsTargetInRange(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        /// <summary>Suppressive Shots — target nearest enemy but never advance; hold ground.</summary>
        static BTNode Marksman_SuppressiveShots()
        {
            return new Sequence("SuppressiveShots",
                new HasEnemies(),
                new FindNearestEnemy(),
                new Condition("InRange", ctx =>
                {
                    var t = ctx.Get<UnitController>("Target");
                    return t != null && Vector3.Distance(ctx.Owner.transform.position, t.transform.position) <= ctx.Owner.AttackRange;
                }),
                new AttackTarget()
            );
        }

        /// <summary>Precision Target — prioritise the lowest-sanity enemy; breaking them cascades.</summary>
        static BTNode Marksman_PrecisionTarget()
        {
            return new Sequence("PrecisionTarget",
                new HasEnemies(),
                new ActionNode("FindLowestSanity", ctx =>
                {
                    var target = ctx.Enemies
                        .Where(e => e != null && e.IsAlive)
                        .OrderBy(e => e.CurrentSanity)
                        .FirstOrDefault();
                    if (target == null) return NodeState.Failure;
                    ctx.Set("Target", target);
                    return NodeState.Success;
                }),
                new Selector("ShootBroken",
                    new Sequence("ShootIfInRange", new IsTargetInRange(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        // ════════════════════════════════════════════════════════
        // OCCULTIST GAMBITS
        // ════════════════════════════════════════════════════════

        /// <summary>Tend the Wounded — only heal; retreat from combat; attack only as last resort.</summary>
        static BTNode Occultist_TendTheWounded()
        {
            return new Selector("TendWounded",
                new Sequence("HealAny",
                    new HasWoundedAlly(0.85f),  // Wider threshold than default
                    new HealAlly(20)
                ),
                new Sequence("FleeToHeal",
                    new HasEnemies(),
                    new FindNearestEnemy(),
                    new Condition("EnemyClose", ctx =>
                    {
                        var t = ctx.Get<UnitController>("Target");
                        return t != null && Vector3.Distance(ctx.Owner.transform.position, t.transform.position) < ctx.Owner.AttackRange * 1.5f;
                    }),
                    new Retreat()
                )
            );
        }

        /// <summary>Psychic Barrier — prioritise sanity restoration over HP healing.</summary>
        static BTNode Occultist_PsychicBarrier()
        {
            return new Sequence("PsychicBarrier",
                new HasStressedAlly(75),          // Trigger if any ally below 75 sanity
                new BoostStressedAlly(20, 8f)     // Restore 20 sanity, wider range
            );
        }

        /// <summary>Despair Wave — instead of healing, attack to drain enemy sanity (eldritch dmg).</summary>
        static BTNode Occultist_DespairWave()
        {
            return new Sequence("DespairWave",
                new HasEnemies(),
                new FindNearestEnemy(),
                new Selector("WaveOrApproach",
                    new Sequence("CastWave",
                        new IsTargetInRange(),
                        new ActionNode("SanityAttack", ctx =>
                        {
                            var target = ctx.Get<UnitController>("Target");
                            if (target == null || !ctx.Owner.CanAttack()) return NodeState.Running;

                            // Inflict eldritch sanity damage instead of physical
                            target.ModifySanity(-12, "EldritchHit");
                            ctx.Owner.ResetAttackCooldown();
                            return NodeState.Success;
                        })
                    ),
                    new MoveToTarget()
                )
            );
        }

        /// <summary>Sacred Rite — stand still and channel to restore 30 self-sanity; no movement.</summary>
        static BTNode Occultist_SacredRite()
        {
            return new ActionNode("SacredRite", ctx =>
            {
                // Channel in place — restore sanity over time
                if (Random.value < 30f * ctx.DeltaTime)   // ~30 sanity/sec
                    ctx.Owner.ModifySanity(1, "SacredRite");
                return NodeState.Running; // Always running — keeps the Occultist still
            });
        }

        // ════════════════════════════════════════════════════════
        // BERSERKER GAMBITS
        // ════════════════════════════════════════════════════════

        /// <summary>Blood Frenzy — attack nearest target regardless of team; pure aggression.</summary>
        static BTNode Berserker_BloodFrenzy()
        {
            return new Sequence("BloodFrenzy",
                new ActionNode("FindNearest_AnyTeam", ctx =>
                {
                    // Merge allies and enemies, find closest — Berserker doesn't care
                    var allTargets = ctx.Allies.Concat(ctx.Enemies)
                        .Where(u => u != null && u.IsAlive && u != ctx.Owner)
                        .OrderBy(u => Vector3.Distance(ctx.Owner.transform.position, u.transform.position))
                        .FirstOrDefault();

                    if (allTargets == null) return NodeState.Failure;
                    ctx.Set("Target", allTargets);
                    return NodeState.Success;
                }),
                new Selector("FrenzyAttack",
                    new Sequence("AttackIfInRange", new IsTargetInRange(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        /// <summary>Death Charge — ignore all retreats, always charge the strongest enemy.</summary>
        static BTNode Berserker_DeathCharge()
        {
            return new Sequence("DeathCharge",
                new ActionNode("NeverRetreat", ctx =>
                {
                    ctx.Owner.GambitIgnoreRetreat = true;
                    return NodeState.Success;
                }),
                new HasEnemies(),
                new FindHighestHPEnemy(),
                new Selector("ChargeStrongest",
                    new Sequence("AttackIfInRange", new IsTargetInRange(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        /// <summary>Berserk Rage — damage scales with HP lost (+5% per 10% HP missing).</summary>
        static BTNode Berserker_BerserkRage()
        {
            return new Sequence("BerserkRage",
                new ActionNode("ScaleDmgToWounds", ctx =>
                {
                    float hpPercent   = (float)ctx.Owner.CurrentHP / ctx.Owner.MaxHP;
                    float missingPercent = 1f - hpPercent;
                    // +5% damage per 10% HP missing, capped at 5 stacks (50%)
                    float stacks = Mathf.Min(missingPercent / 0.10f, 5f);
                    ctx.Owner.GambitDamageMultiplier = 1f + stacks * 0.05f;
                    return NodeState.Success;
                }),
                new HasEnemies(),
                new FindHighestHPEnemy(),
                new Selector("RageAttack",
                    new Sequence("AttackIfInRange", new IsTargetInRange(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        /// <summary>Hunt the Pack — target whichever enemy cluster has the most allies nearby.</summary>
        static BTNode Berserker_HuntThePack()
        {
            return new Sequence("HuntThePack",
                new HasEnemies(),
                new ActionNode("FindDensestCluster", ctx =>
                {
                    UnitController packLeader = null;
                    int maxNearby = -1;

                    foreach (var enemy in ctx.Enemies.Where(e => e != null && e.IsAlive))
                    {
                        int nearby = ctx.Enemies.Count(e => e != null && e.IsAlive
                            && Vector3.Distance(enemy.transform.position, e.transform.position) < 3f);
                        if (nearby > maxNearby)
                        {
                            maxNearby   = nearby;
                            packLeader  = enemy;
                        }
                    }

                    if (packLeader == null) return NodeState.Failure;
                    ctx.Set("Target", packLeader);
                    return NodeState.Success;
                }),
                new Selector("ChargePack",
                    new Sequence("AttackIfInRange", new IsTargetInRange(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        // ════════════════════════════════════════════════════════
        // INVESTIGATOR GAMBITS
        // ════════════════════════════════════════════════════════

        /// <summary>Observe and Report — only analyse enemies; never engage directly.</summary>
        static BTNode Investigator_ObserveReport()
        {
            return new Sequence("ObserveReport",
                new HasEnemies(),
                new FindHighestHPEnemy(),
                new Condition("NotAnalysed", ctx =>
                {
                    var t = ctx.Get<UnitController>("Target");
                    return t != null && !ctx.Get<bool>($"Analysed_{t.UnitId}");
                }),
                new AnalyseTarget(),
                // After analyse — retreat, never attack
                new Retreat()
            );
        }

        /// <summary>Weakness Exploit — deal +30% damage to any analysed target.</summary>
        static BTNode Investigator_WeaknessExploit()
        {
            return new Sequence("WeaknessExploit",
                new HasEnemies(),
                new ActionNode("FindAnalysed", ctx =>
                {
                    var analysed = ctx.Enemies
                        .Where(e => e != null && e.IsAlive && ctx.Get<bool>($"Analysed_{e.UnitId}"))
                        .OrderBy(e => e.CurrentHP)
                        .FirstOrDefault();

                    if (analysed == null) return NodeState.Failure;
                    ctx.Set("Target", analysed);
                    ctx.Owner.GambitDamageMultiplier = 1.30f;
                    return NodeState.Success;
                }),
                new Selector("ExploitWeakness",
                    new Sequence("StrikeIfInRange", new IsTargetInRange(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        /// <summary>
        /// Forbidden Scan — analyse every enemy on hit.
        /// Each scan permanently reduces the Investigator's MaxSanity (GDD §5.4 Forbidden Knowledge).
        /// </summary>
        static BTNode Investigator_ForbiddenScan()
        {
            return new Sequence("ForbiddenScan",
                new HasEnemies(),
                new FindHighestHPEnemy(),
                new ActionNode("ScanOnHit", ctx =>
                {
                    var target = ctx.Get<UnitController>("Target");
                    if (target == null) return NodeState.Failure;
                    if (!ctx.Owner.CanAttack()) return NodeState.Running;

                    // Auto-analyse all living enemies (Forbidden Knowledge cost)
                    foreach (var enemy in ctx.Enemies.Where(e => e != null && e.IsAlive))
                    {
                        string key = $"Analysed_{enemy.UnitId}";
                        if (!ctx.Get<bool>(key))
                        {
                            ctx.Set(key, true);
                            // Forbidden Knowledge: permanently lowers MaxSanity ceiling by 2 (GDD §5.4)
                            ctx.Owner.ApplyForbiddenKnowledge(2);
                        }
                    }

                    ctx.Owner.PerformAttack(target);
                    return NodeState.Success;
                })
            );
        }

        /// <summary>Tactical Withdrawal — always retreat to maximum range before analysing.</summary>
        static BTNode Investigator_TacticalWithdraw()
        {
            return new Sequence("TacticalWithdraw",
                new HasEnemies(),
                new FindHighestHPEnemy(),
                new Selector("WithdrawThenAnalyse",
                    // If too close, retreat first
                    new Sequence("Retreat_IfClose",
                        new Condition("TooClose", ctx =>
                        {
                            var t = ctx.Get<UnitController>("Target");
                            return t != null && Vector3.Distance(ctx.Owner.transform.position, t.transform.position) < ctx.Owner.AttackRange * 1.5f;
                        }),
                        new Retreat()
                    ),
                    // Safe distance — analyse
                    new Sequence("SafeAnalyse",
                        new Condition("NotAnalysed2", ctx =>
                        {
                            var t = ctx.Get<UnitController>("Target");
                            return t != null && !ctx.Get<bool>($"Analysed_{t.UnitId}");
                        }),
                        new AnalyseTarget()
                    )
                )
            );
        }

        // ════════════════════════════════════════════════════════
        // SHADOW GAMBITS
        // ════════════════════════════════════════════════════════

        /// <summary>Assassin Protocol — wait for an enemy to be engaged, then strike its flank.</summary>
        static BTNode Shadow_AssassinProtocol()
        {
            return new Sequence("AssassinProtocol",
                new HasEnemies(),
                new ActionNode("FindEngaged", ctx =>
                {
                    // Look for an enemy that has at least one ally adjacent (is engaged)
                    var engaged = ctx.Enemies
                        .Where(e => e != null && e.IsAlive)
                        .Where(e => ctx.Allies.Any(a => a != null && a.IsAlive
                            && Vector3.Distance(a.transform.position, e.transform.position) < a.AttackRange + 0.5f))
                        .OrderByDescending(e => e.CurrentHP)
                        .FirstOrDefault();

                    if (engaged == null) return NodeState.Failure;
                    ctx.Set("Target", engaged);
                    return NodeState.Success;
                }),
                new Selector("FlankEngaged",
                    new Sequence("StrikeEngaged", new IsTargetInRange(), new AttackTarget()),
                    new Sequence("CircleIn", new CircleToFlank(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        /// <summary>Vanish and Strike — retreat 3 units after every attack (hit-and-run).</summary>
        static BTNode Shadow_VanishAndStrike()
        {
            return new Sequence("VanishAndStrike",
                new HasEnemies(),
                new FindHighestHPEnemy(),
                new Selector("StrikeOrApproach",
                    new Sequence("StrikeAndVanish",
                        new IsTargetInRange(),
                        new AttackTarget(),
                        // After attacking, immediately dash back
                        new ActionNode("Vanish", ctx =>
                        {
                            var target = ctx.Get<UnitController>("Target");
                            if (target == null) return NodeState.Failure;
                            Vector3 away = (ctx.Owner.transform.position - target.transform.position).normalized;
                            ctx.Owner.transform.position += away * 3f; // 3-unit dash back
                            return NodeState.Success;
                        })
                    ),
                    new MoveToTarget()
                )
            );
        }

        /// <summary>Leader Assassination — tunnel-vision on highest-HP enemy only.</summary>
        static BTNode Shadow_LeaderAssassination()
        {
            return new Sequence("LeaderAssassination",
                new HasEnemies(),
                new FindHighestHPEnemy(),
                new Selector("KillLeader",
                    new Sequence("StrikeLeader", new IsTargetInRange(), new AttackTarget()),
                    new Sequence("CircleToLeader", new CircleToFlank(), new AttackTarget()),
                    new MoveToTarget()
                )
            );
        }

        /// <summary>Ghost Step — only move when not targeted (no enemies aiming at this unit).</summary>
        static BTNode Shadow_GhostStep()
        {
            return new Sequence("GhostStep",
                new HasEnemies(),
                new FindHighestHPEnemy(),
                new Selector("GhostAttack",
                    // Always attack if in range — being stationary doesn't mean inactive
                    new Sequence("StrikeIfInRange", new IsTargetInRange(), new AttackTarget()),
                    // Only move when no nearby enemy is already in attack range of us
                    new Sequence("MoveWhenSafe",
                        new Condition("NotTargeted", ctx =>
                        {
                            return !ctx.Enemies.Any(e => e != null && e.IsAlive
                                && Vector3.Distance(e.transform.position, ctx.Owner.transform.position) <= e.AttackRange);
                        }),
                        new MoveToTarget()
                    )
                )
            );
        }

        // ════════════════════════════════════════════════════════
        // HERALD GAMBITS
        // ════════════════════════════════════════════════════════

        /// <summary>Sanity Beacon — stand still, continuously emit a sanity restoration aura.</summary>
        static BTNode Herald_SanityBeacon()
        {
            return new ActionNode("SanityBeacon", ctx =>
            {
                // Pulse sanity to all allies in range every ~3 seconds
                if (Random.value < (10f / 3f) * ctx.DeltaTime)
                {
                    foreach (var ally in ctx.Allies)
                    {
                        if (ally == null || !ally.IsAlive || ally == ctx.Owner) continue;
                        if (Vector3.Distance(ctx.Owner.transform.position, ally.transform.position) <= 8f)
                            ally.ModifySanity(5, "SanityBeacon");
                    }
                }
                return NodeState.Running; // Always running — keeps Herald rooted in place
            });
        }

        /// <summary>Battle Cry — boost nearby ally damage instead of sanity for a period.</summary>
        static BTNode Herald_BattleCry()
        {
            return new ActionNode("BattleCry", ctx =>
            {
                // Every tick — apply a damage buff to all nearby allies
                foreach (var ally in ctx.Allies)
                {
                    if (ally == null || !ally.IsAlive || ally == ctx.Owner) continue;
                    if (Vector3.Distance(ctx.Owner.transform.position, ally.transform.position) <= 6f)
                        ally.GambitDamageMultiplier = 1.20f; // +20% damage
                }
                return NodeState.Running;
            });
        }

        /// <summary>Last Rites — when an ally just died, immediately boost survivors.</summary>
        static BTNode Herald_LastRites()
        {
            return new Sequence("LastRites",
                new Condition("AllyJustDied", ctx =>
                    ctx.Get<bool>("AllyDiedThisTick")
                ),
                new ActionNode("ComfortSurvivors", ctx =>
                {
                    foreach (var ally in ctx.Allies)
                    {
                        if (ally == null || !ally.IsAlive) continue;
                        ally.ModifySanity(20, "LastRites");
                    }
                    ctx.Set("AllyDiedThisTick", false);
                    return NodeState.Success;
                })
            );
        }

        /// <summary>Martyr Signal — take +50% incoming damage, but every hit boosts nearby ally resolve.</summary>
        static BTNode Herald_MartyrSignal()
        {
            return new ActionNode("MartyrSignal", ctx =>
            {
                // The +50% damage taken is handled by the TakeDamage override flag
                ctx.Set("MartyrMode", true);

                // Pulse small sanity boost to allies
                if (Random.value < 3f * ctx.DeltaTime)
                {
                    foreach (var ally in ctx.Allies)
                    {
                        if (ally == null || !ally.IsAlive || ally == ctx.Owner) continue;
                        if (Vector3.Distance(ctx.Owner.transform.position, ally.transform.position) <= 5f)
                            ally.ModifySanity(3, "MartyrPulse");
                    }
                }
                return NodeState.Running;
            });
        }

        // ════════════════════════════════════════════════════════
        // VESSEL GAMBITS
        // ════════════════════════════════════════════════════════

        /// <summary>Ghost Protocol — always flee to the furthest possible point from all enemies.</summary>
        static BTNode Vessel_GhostProtocol()
        {
            return new Sequence("GhostProtocol",
                new HasEnemies(),
                new ActionNode("FleeToEdge", ctx =>
                {
                    Vector3 centroid = Vector3.zero;
                    int count = 0;
                    foreach (var enemy in ctx.Enemies.Where(e => e != null && e.IsAlive))
                    {
                        centroid += enemy.transform.position;
                        count++;
                    }
                    if (count == 0) return NodeState.Failure;
                    centroid /= count;

                    Vector3 away = (ctx.Owner.transform.position - centroid).normalized;
                    ctx.Owner.transform.position += away * ctx.Owner.MoveSpeed * 1.5f * ctx.DeltaTime;
                    return NodeState.Running;
                })
            );
        }

        /// <summary>Sacred Ward — stay directly behind the nearest friendly Warden.</summary>
        static BTNode Vessel_SacredWard()
        {
            return new ActionNode("StayBehindWarden", ctx =>
            {
                var warden = ctx.Allies
                    .Where(a => a != null && a.IsAlive && a.UnitClass == UnitClass.Warden)
                    .OrderBy(a => Vector3.Distance(ctx.Owner.transform.position, a.transform.position))
                    .FirstOrDefault();

                if (warden == null) return NodeState.Failure;

                // Position behind warden relative to nearest enemy
                var nearestEnemy = ctx.Enemies
                    .Where(e => e != null && e.IsAlive)
                    .OrderBy(e => Vector3.Distance(warden.transform.position, e.transform.position))
                    .FirstOrDefault();

                if (nearestEnemy == null) return NodeState.Failure;

                Vector3 awayFromEnemy = (warden.transform.position - nearestEnemy.transform.position).normalized;
                Vector3 targetPos = warden.transform.position + awayFromEnemy * 1.5f;

                float dist = Vector3.Distance(ctx.Owner.transform.position, targetPos);
                if (dist > 0.5f)
                {
                    Vector3 dir = (targetPos - ctx.Owner.transform.position).normalized;
                    ctx.Owner.transform.position += dir * ctx.Owner.MoveSpeed * ctx.DeltaTime;
                }
                return NodeState.Running;
            });
        }

        /// <summary>Desperate Plea — when near death, generate an extra Mercy Token signal.</summary>
        static BTNode Vessel_DesperatePlea()
        {
            return new Sequence("DesperatePlea",
                new IsHealthBelow(0.20f),
                new ActionNode("SignalMercy", ctx =>
                {
                    // Flag for DirectiveSystem to pick up — generates bonus token on death
                    ctx.Set("DesperatePleaActive", true);
                    return NodeState.Success;
                }),
                // After signalling — flee
                new HasEnemies(),
                new Retreat()
            );
        }
    }
}
