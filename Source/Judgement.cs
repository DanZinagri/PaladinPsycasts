using System.Collections.Generic;
using System.Reflection;
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

        // Vanilla starts the pillar a quarter of its width above the strike point. This fraction
        // of the width is pulled back down so the beam lands on the target; tune in XML.
        public float beamDropFraction = 0.25f;
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
                beam.MatchBeamToRadius();
            }
        }
    }

    // Vanilla orbital beam for the visual. Damage lands once and hits every pawn in the radius
    // except the caster.
    public class OrbitalStrike_Judgement : OrbitalStrike
    {
        private static readonly FieldInfo AngleField =
            typeof(OrbitalStrike).GetField("angle", BindingFlags.Instance | BindingFlags.NonPublic);

        public float radius = 5.9f;
        public float damageScale = 1f;

        private bool struck;
        private JudgementExtension ext;
        private Vector3 drawShift;

        private JudgementExtension Ext => ext ??= def.GetModExtension<JudgementExtension>();

        // CompOrbitalBeam positions the whole beam from DrawPos; damage and flecks use Position.
        public override Vector3 DrawPos => base.DrawPos + drawShift;

        // CompOrbitalBeam draws at Props.width every frame, and props is a public field, so each
        // beam gets its own copy sized to its radius instead of sharing the def's fixed width.
        // Props aren't saved, hence the reapply on load. The beam texture's bright core fills only
        // about half its width, hence the 1.8 multiplier to make the visible beam fill the area.
        public void MatchBeamToRadius()
        {
            CompOrbitalBeam comp = GetComp<CompOrbitalBeam>();
            if (!(comp?.props is CompProperties_OrbitalBeam shared)) return;

            float width = (radius * 2f + 1f) * 1.8f;
            comp.props = new CompProperties_OrbitalBeam
            {
                width = width,
                color = shared.color,
                sound = shared.sound
            };

            // Same direction vanilla draws the beam along, so the shift follows its tilt.
            float angle = (float)AngleField.GetValue(this);
            drawShift = -Vector3Utility.FromAngleFlat(angle - 90f) * width * (Ext?.beamDropFraction ?? 0.25f);
        }

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

            // PsycastAreaEffect is 2.3 cells at scale 1 and grows ~1.1 scale over its life, so start
            // it small enough that it finishes at the edge of the damaged area.
            float diameter = radius * 2f + 1f;
            FleckMaker.Static(Position.ToVector3Shifted(), Map, FleckDefOf.PsycastAreaEffect,
                Mathf.Max(0.3f, diameter / 2.3f - 1.1f));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref radius, "radius", 5.9f);
            Scribe_Values.Look(ref damageScale, "damageScale", 1f);
            Scribe_Values.Look(ref struck, "struck", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) MatchBeamToRadius();
        }
    }
}
