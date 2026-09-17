using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Ability = VEF.Abilities.Ability;

namespace PaladinPsycasts
{
    // Touch heal. Melee reach comes from the def's short range with no goto job.
    // The unholy are seared instead of healed.
    public class Ability_LayOnHands : Ability
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);

            float pool = GetPowerForPawn();
            foreach (GlobalTargetInfo target in targets)
            {
                if (!(target.Thing is Pawn patient) || patient.Dead) continue;

                if (PaladinUtility.IsUnholy(patient)) SearUnholy(patient, pool);
                else Heal(patient, pool);
            }
        }

        private void SearUnholy(Pawn patient, float pool)
        {
            patient.TakeDamage(new DamageInfo(PaladinDefOf.DZ_Paladin_HolyFire, pool, 0f, -1f, pawn, null, null,
                DamageInfo.SourceCategory.ThingOrUnknown, patient));
            if (patient.Map != null)
                MoteMaker.ThrowText(patient.DrawPos, patient.Map, "DZ_Paladin_Unhallowed".Translate(), 2f);
        }

        // Spends the pool on the worst injuries first.
        private void Heal(Pawn patient, float pool)
        {
            List<Hediff_Injury> injuries = new List<Hediff_Injury>();
            List<Hediff> hediffs = patient.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Injury injury && injury.Severity > 0f && !injury.IsPermanent())
                    injuries.Add(injury);
            }

            if (injuries.Count == 0)
            {
                if (patient.Map != null)
                    MoteMaker.ThrowText(patient.DrawPos, patient.Map, "DZ_Paladin_Unwounded".Translate(), 2f);
                return;
            }

            injuries.Sort((a, b) => b.Severity.CompareTo(a.Severity));

            float remaining = pool;
            for (int i = 0; i < injuries.Count && remaining > 0f; i++)
            {
                float healed = Mathf.Min(injuries[i].Severity, remaining);
                injuries[i].Heal(healed);
                remaining -= healed;
            }

            if (patient.Map != null)
            {
                MoteMaker.ThrowText(patient.DrawPos, patient.Map,
                    "DZ_Paladin_Healed".Translate((pool - remaining).ToString("F0")), 2f);
                FleckMaker.AttachedOverlay(patient, FleckDefOf.HealingCross, Vector3.zero);
            }
        }

        public override void CheckCastEffects(GlobalTargetInfo[] targetsInfos, out bool cast, out bool target,
            out bool hediffApply)
        {
            base.CheckCastEffects(targetsInfos, out cast, out target, out _);
            hediffApply = false;
        }
    }
}
