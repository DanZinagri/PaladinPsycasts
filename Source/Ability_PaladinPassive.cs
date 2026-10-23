using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using Ability = VEF.Abilities.Ability;

namespace PaladinPsycasts
{
    public class AbilityExtension_PaladinPassive : DefModExtension
    {
        public List<HediffDef> hediffs = new List<HediffDef>();

        // Optional active half: if set, the node gets a button that removes these from the caster.
        public List<HediffDef> purgeOnCast;
    }

    // A tree node whose hediffs are applied on unlock. With purgeOnCast set it is also castable,
    // removing those hediffs from the caster; otherwise it has no button at all.
    // Init() only runs from CompAbilities.GiveAbility, never on save load, so the hediffs are
    // reapplied at PostLoadInit - without that, passives vanish on the first reload.
    public class Ability_PaladinPassive : Ability
    {
        private List<HediffDef> PurgeOnCast => def.GetModExtension<AbilityExtension_PaladinPassive>()?.purgeOnCast;

        public override void Init()
        {
            base.Init();
            ApplyPassives();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit) ApplyPassives();
        }

        private void ApplyPassives()
        {
            List<HediffDef> hediffs = def.GetModExtension<AbilityExtension_PaladinPassive>()?.hediffs;
            if (hediffs == null || pawn?.health == null) return;

            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] != null && pawn.health.hediffSet.GetFirstHediffOfDef(hediffs[i]) == null)
                    pawn.health.AddHediff(hediffs[i]);
            }
        }

        public override bool ShowGizmoOnPawn() => !PurgeOnCast.NullOrEmpty() && base.ShowGizmoOnPawn();

        public override bool IsEnabledForPawn(out string reason)
        {
            if (!base.IsEnabledForPawn(out reason)) return false;
            if (FirstPurgeable() != null) return true;

            reason = "DZ_Paladin_NothingToPurge".Translate();
            return false;
        }

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);

            Hediff hediff;
            while ((hediff = FirstPurgeable()) != null) pawn.health.RemoveHediff(hediff);
        }

        private Hediff FirstPurgeable()
        {
            List<HediffDef> purge = PurgeOnCast;
            if (purge == null || pawn?.health == null) return null;

            for (int i = 0; i < purge.Count; i++)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(purge[i]);
                if (hediff != null) return hediff;
            }

            return null;
        }

        public override bool AICanUseOn(Thing target) => false;
    }
}
