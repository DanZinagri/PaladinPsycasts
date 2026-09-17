using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PaladinPsycasts
{
    public class JudgementExtension : DefModExtension
    {
        public float flameDamage = 18f;
        public float physicalDamage = 18f;

        // Delay between the beam appearing and the damage landing, so the visual reads first.
        public int strikeDelayTicks = 35;
    }

    public class Ability_Judgement : Ability_Deferred
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);

            foreach (GlobalTargetInfo target in targets)
            {
                Map map = target.Map ?? pawn.Map;
                if (map == null) continue;

                OrbitalStrike_Judgement beam =
                    (OrbitalStrike_Judgement)GenSpawn.Spawn(PaladinDefOf.DZ_Paladin_JudgementBeam, target.Cell, map);
                beam.instigator = pawn;
                beam.duration = Mathf.Max(60, GetDurationForPawn());
                beam.radius = GetRadiusForPawn();
                beam.damageScale = GetPowerForPawn();
                beam.StartStrike();
            }
        }
    }

    // Vanilla orbital beam for the visual. Damage lands once and hits every pawn in the radius
    // except the caster.
    public class OrbitalStrike_Judgement : OrbitalStrike
    {
        public float radius = 5.9f;
        public float damageScale = 1f;

        private bool struck;
        private JudgementExtension ext;

        private JudgementExtension Ext => ext ??= def.GetModExtension<JudgementExtension>();

        protected override void Tick()
        {
            base.Tick();
            if (struck || Destroyed || Ext == null || TicksPassed < Ext.strikeDelayTicks) return;

            struck = true;
            Strike();
        }

        private void Strike()
        {
            if (Map == null) return;

            List<Thing> found = new List<Thing>(GenRadial.RadialDistinctThingsAround(Position, Map, radius, true));
            for (int i = 0; i < found.Count; i++)
            {
                if (!(found[i] is Pawn target) || target.Dead || target == instigator) continue;

                target.TakeDamage(new DamageInfo(PaladinDefOf.DZ_Paladin_HolyFire,
                    Ext.flameDamage * damageScale, 0f, -1f, instigator, null, null,
                    DamageInfo.SourceCategory.ThingOrUnknown, target));

                if (target.Dead) continue;

                target.TakeDamage(new DamageInfo(PaladinDefOf.DZ_Paladin_SmiteDamage,
                    Ext.physicalDamage * damageScale, 0f, -1f, instigator, null, null,
                    DamageInfo.SourceCategory.ThingOrUnknown, target));
            }

            FleckMaker.Static(Position.ToVector3Shifted(), Map, FleckDefOf.PsycastAreaEffect, radius);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref radius, "radius", 5.9f);
            Scribe_Values.Look(ref damageScale, "damageScale", 1f);
            Scribe_Values.Look(ref struck, "struck", false);
        }
    }
}
