using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.AI;

namespace PaladinPsycasts
{
    public class AngelicAspectExtension : DefModExtension
    {
        public float igniteChance = 0.2f;
        public float igniteSize = 0.15f;
    }

    // Grants the Angelic Leap ability for exactly as long as the hediff lives (the granted
    // ability is scribed with CompAbilities, so no load-time reapply is needed), and ignites
    // what the pawn damages. Thing.TakeDamage notifies every hediff on the instigator, so the
    // ignite covers melee, ranged and ability damage without patching.
    public class Hediff_AngelicAspect : Hediff_ScaledBuff
    {
        private AngelicAspectExtension ext;

        private AngelicAspectExtension Ext => ext ??= def.GetModExtension<AngelicAspectExtension>();

        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);

            if (Ext == null || thing == null || !thing.Spawned || dinfo.Def == DamageDefOf.Flame) return;
            if (Rand.Chance(Ext.igniteChance)) thing.TryAttachFire(Ext.igniteSize, pawn);
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            pawn?.GetComp<CompAbilities>()?.GiveAbility(PaladinDefOf.DZ_Paladin_AngelicLeap);
        }

        // The leap def has no ticking flags, so the learned list is the only place it lives.
        public override void PostRemoved()
        {
            base.PostRemoved();
            pawn?.GetComp<CompAbilities>()?.LearnedAbilities
                .RemoveAll(a => a.def == PaladinDefOf.DZ_Paladin_AngelicLeap);
        }
    }

    public class HediffCompProperties_ShortenMentalStates : HediffCompProperties
    {
        // Mental breaks are cut to this share of their maximum length.
        public float durationFactor = 0.5f;
        public int checkInterval = 60;

        public HediffCompProperties_ShortenMentalStates() => compClass = typeof(HediffComp_ShortenMentalStates);
    }

    // MentalState.forceRecoverAfterTicks is public and checked against the state's own age, so
    // lowering it shortens any mental state - enemy-inflicted ones included - without patching.
    // Only ever lowered, never raised.
    public class HediffComp_ShortenMentalStates : HediffComp
    {
        private int timer;

        private HediffCompProperties_ShortenMentalStates Props => (HediffCompProperties_ShortenMentalStates)props;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            timer -= delta;
            if (timer > 0) return;
            timer = Mathf.Max(1, Props.checkInterval);

            MentalState state = Pawn?.MentalState;
            if (state?.def == null) return;

            int full = state.def.maxTicksBeforeRecovery;
            if (full <= 0 || full == int.MaxValue) full = state.def.minTicksBeforeRecovery;
            if (full <= 0) return;

            int shortened = Mathf.Max(1, Mathf.RoundToInt(full * Props.durationFactor));
            if (state.forceRecoverAfterTicks < 0 || shortened < state.forceRecoverAfterTicks)
                state.forceRecoverAfterTicks = shortened;
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref timer, "shortenTimer", 0);
        }
    }

    public class HediffCompProperties_PurgeHediffs : HediffCompProperties
    {
        public List<HediffDef> purge = new List<HediffDef>();
        public int checkInterval = 2500;

        public HediffCompProperties_PurgeHediffs() => compClass = typeof(HediffComp_PurgeHediffs);
    }

    // Clears the listed hediffs shortly after they appear, rather than patching the birthday
    // HediffGivers that hand them out. Also cleans up whatever the pawn already carried.
    public class HediffComp_PurgeHediffs : HediffComp
    {
        private int timer;

        private HediffCompProperties_PurgeHediffs Props => (HediffCompProperties_PurgeHediffs)props;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            timer -= delta;
            if (timer > 0) return;
            timer = Mathf.Max(1, Props.checkInterval);

            if (Props.purge.NullOrEmpty() || Pawn?.health == null) return;

            // Backwards so removal during iteration is safe without a temp list.
            List<Hediff> hediffs = Pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (i < hediffs.Count && Props.purge.Contains(hediffs[i].def))
                    Pawn.health.RemoveHediff(hediffs[i]);
            }
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref timer, "purgeTimer", 0);
        }
    }
}
