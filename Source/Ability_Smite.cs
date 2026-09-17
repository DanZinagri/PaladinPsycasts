using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PaladinPsycasts
{
    // Strikes for a share (def power) of the caster's own best melee hit.
    public class Ability_Smite : Ability_Deferred
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);

            float damage = PaladinUtility.BestMeleeDamage(pawn) * GetPowerForPawn();
            foreach (GlobalTargetInfo target in targets)
            {
                Map map = target.Map ?? pawn.Map;
                if (map == null) continue;

                Skyfaller_SmiteHammer hammer =
                    (Skyfaller_SmiteHammer)SkyfallerMaker.MakeSkyfaller(PaladinDefOf.DZ_Paladin_SmiteHammer);
                hammer.instigator = pawn;
                hammer.damage = damage;
                hammer.target = target.Thing;
                GenSpawn.Spawn(hammer, target.Cell, map);
            }
        }
    }

    // Guided: hits the thing it was aimed at, or whoever stands in the impact cell if that
    // target is gone by the time it lands.
    public class Skyfaller_SmiteHammer : Skyfaller
    {
        public Pawn instigator;
        public Thing target;
        public float damage = 10f;

        protected override void Impact()
        {
            Map map = Map;
            if (map != null)
            {
                if (target != null && target.Spawned && target.Map == map)
                {
                    ApplyTo(target);
                }
                else
                {
                    // Copied: TakeDamage can mutate the cell's thing list.
                    List<Thing> inCell = new List<Thing>(Position.GetThingList(map));
                    for (int i = 0; i < inCell.Count; i++)
                    {
                        if (inCell[i] is Pawn && inCell[i] != instigator) ApplyTo(inCell[i]);
                    }
                }

                FleckMaker.Static(Position.ToVector3Shifted(), map, FleckDefOf.PsycastAreaEffect, 1.4f);
            }

            base.Impact();
        }

        private void ApplyTo(Thing thing)
        {
            thing.TakeDamage(new DamageInfo(PaladinDefOf.DZ_Paladin_SmiteDamage,
                PaladinUtility.HolyDamageAgainst(damage, thing), 0f, -1f, instigator, null, null,
                DamageInfo.SourceCategory.ThingOrUnknown, thing));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref instigator, "instigator");
            Scribe_References.Look(ref target, "target");
            Scribe_Values.Look(ref damage, "damage", 10f);
        }
    }
}
