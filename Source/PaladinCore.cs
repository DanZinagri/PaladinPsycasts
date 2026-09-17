using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Ability = VEF.Abilities.Ability;

namespace PaladinPsycasts
{
    [DefOf]
    public static class PaladinDefOf
    {
        public static ThingDef DZ_Paladin_SmiteHammer;
        public static ThingDef DZ_Paladin_ConsecratedGround;
        public static ThingDef DZ_Paladin_JudgementBeam;

        public static DamageDef DZ_Paladin_SmiteDamage;
        public static DamageDef DZ_Paladin_HolyFire;

        public static VEF.Abilities.AbilityDef DZ_Paladin_AngelicLeap;

        static PaladinDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(PaladinDefOf));
    }

    public static class PaladinUtility
    {
        // Anomaly entities plus ghouls and shamblers. Empty without Anomaly.
        public static bool IsUnholy(Thing thing)
        {
            return thing is Pawn pawn && !pawn.Dead && (pawn.RaceProps.IsAnomalyEntity || pawn.IsSubhuman);
        }

        public static float Sensitivity(Pawn pawn)
        {
            return pawn == null ? 1f : Mathf.Max(0.01f, pawn.GetStatValue(StatDefOf.PsychicSensitivity));
        }

        // Per-hit damage of the best available melee verb, weapon and skill included.
        public static float BestMeleeDamage(Pawn pawn)
        {
            float best = 0f;
            List<VerbEntry> verbs = pawn?.meleeVerbs?.GetUpdatedAvailableVerbsList(false);
            if (verbs != null)
            {
                for (int i = 0; i < verbs.Count; i++)
                {
                    Verb verb = verbs[i].verb;
                    if (verb?.verbProps == null) continue;
                    float dmg = verb.verbProps.AdjustedMeleeDamageAmount(verb, pawn);
                    if (dmg > best) best = dmg;
                }
            }

            return best > 0f ? best : 8f;
        }

        public static float HolyDamageAgainst(float baseDamage, Thing target, float unholyMultiplier = 2f)
        {
            return IsUnholy(target) ? baseDamage * unholyMultiplier : baseDamage;
        }
    }

    // For abilities whose spawned thing delivers the effect: skips VEF's own target-side
    // fleck and hediff pass.
    public abstract class Ability_Deferred : Ability
    {
        public override void CheckCastEffects(GlobalTargetInfo[] targetsInfos, out bool cast, out bool target,
            out bool hediffApply)
        {
            base.CheckCastEffects(targetsInfos, out cast, out _, out _);
            target = false;
            hediffApply = false;
        }
    }
}
