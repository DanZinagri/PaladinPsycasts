using System.Collections.Generic;
using Verse;
using Ability = VEF.Abilities.Ability;

namespace PaladinPsycasts
{
    public class AbilityExtension_PaladinPassive : DefModExtension
    {
        public List<HediffDef> hediffs = new List<HediffDef>();
    }

    // A tree node that is never cast: unlocking it applies its hediffs.
    // Init() only runs from CompAbilities.GiveAbility, never on save load, so the hediffs are
    // reapplied at PostLoadInit - without that, passives vanish on the first reload.
    public class Ability_PaladinPassive : Ability
    {
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

        public override bool ShowGizmoOnPawn() => false;

        public override bool AICanUseOn(Thing target) => false;
    }
}
