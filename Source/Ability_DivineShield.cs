using RimWorld.Planet;
using VEF.Abilities;
using Ability = VEF.Abilities.Ability;

namespace PaladinPsycasts
{
    // The immunity is the applied hediff (IncomingDamageFactor x0). The bubble is visual only:
    // VEF's CompAbilities is a shield-belt-style comp already on every humanlike, so it draws in
    // the pawn's own render pass and adds no per-tick cost. power is the bubble's energy pool
    // and only has to outlast the buff.
    public class Ability_DivineShield : Ability
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);

            string texPath = def.GetModExtension<AbilityExtension_Shield>()?.shieldTexPath;
            pawn?.GetComp<CompAbilities>()?.ReinitShield(GetPowerForPawn(), texPath, GetDurationForPawn());
        }
    }
}
