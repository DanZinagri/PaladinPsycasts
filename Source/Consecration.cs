using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PaladinPsycasts
{
    public class ConsecrationExtension : DefModExtension
    {
        public float damagePerPulse = 3f;
        public int pulseIntervalTicks = 60;
        public float unholyMultiplier = 2f;
        public HediffDef allyBlessing;
    }

    // Radius, duration and damage scale are all read from the ability def at cast time.
    public class Ability_Consecration : Ability_Deferred
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            if (pawn.Map == null) return;

            Thing_ConsecratedGround ground =
                (Thing_ConsecratedGround)ThingMaker.MakeThing(PaladinDefOf.DZ_Paladin_ConsecratedGround);
            ground.instigator = pawn;
            ground.radius = GetRadiusForPawn();
            ground.damageScale = GetPowerForPawn();
            ground.ticksLeft = GetDurationForPawn();
            GenSpawn.Spawn(ground, pawn.Position, pawn.Map);
        }
    }

    public class Thing_ConsecratedGround : ThingWithComps
    {
        public Pawn instigator;
        public float radius = 4.9f;
        public float damageScale = 1f;
        public int ticksLeft = 1200;

        private int pulseTimer;
        private ConsecrationExtension ext;

        private static readonly List<Thing> ScratchTargets = new List<Thing>();

        private ConsecrationExtension Ext => ext ??= def.GetModExtension<ConsecrationExtension>();

        // TickInterval, not Tick: 1.6 batches it down to every 15 ticks when off-screen.
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            ticksLeft -= delta;
            if (ticksLeft <= 0)
            {
                Destroy();
                return;
            }

            if (Ext == null || Map == null) return;

            pulseTimer -= delta;
            if (pulseTimer > 0) return;
            pulseTimer = Mathf.Max(1, Ext.pulseIntervalTicks);
            Pulse();
        }

        private void Pulse()
        {
            List<Thing> found = ScratchTargets;
            found.Clear();
            found.AddRange(GenRadial.RadialDistinctThingsAround(Position, Map, radius, true));

            for (int i = 0; i < found.Count; i++)
            {
                if (!(found[i] is Pawn target) || target.Dead) continue;

                if (instigator != null && target.HostileTo(instigator))
                {
                    float amount = damageScale * Ext.damagePerPulse;
                    if (PaladinUtility.IsUnholy(target)) amount *= Ext.unholyMultiplier;

                    target.TakeDamage(new DamageInfo(PaladinDefOf.DZ_Paladin_HolyFire, amount, 0f, -1f,
                        instigator, null, null, DamageInfo.SourceCategory.ThingOrUnknown, target));
                }
                else if (Ext.allyBlessing != null && !PaladinUtility.IsUnholy(target))
                {
                    RefreshBlessing(target);
                }
            }

            found.Clear();
            FleckMaker.Static(Position.ToVector3Shifted(), Map, FleckDefOf.PsycastAreaEffect, radius * 0.5f);
        }

        // Lifetime slightly longer than the pulse gap: the buff holds while standing on the
        // ground and lapses shortly after stepping off.
        private void RefreshBlessing(Pawn target)
        {
            Hediff blessing = target.health.hediffSet.GetFirstHediffOfDef(Ext.allyBlessing);
            if (blessing == null)
            {
                blessing = HediffMaker.MakeHediff(Ext.allyBlessing, target);
                target.health.AddHediff(blessing);
            }

            HediffComp_Disappears disappears = blessing.TryGetComp<HediffComp_Disappears>();
            if (disappears != null) disappears.ticksToDisappear = Mathf.Max(2, Ext.pulseIntervalTicks * 3);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            drawLoc.y = AltitudeLayer.Filth.AltitudeFor();
            Matrix4x4 matrix = default;
            matrix.SetTRS(drawLoc, Quaternion.identity, new Vector3(radius * 2f, 1f, radius * 2f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref instigator, "instigator");
            Scribe_Values.Look(ref radius, "radius", 4.9f);
            Scribe_Values.Look(ref damageScale, "damageScale", 1f);
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 1200);
            Scribe_Values.Look(ref pulseTimer, "pulseTimer", 0);
        }
    }
}
