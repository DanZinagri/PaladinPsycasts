using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Ability = VEF.Abilities.Ability;

namespace PaladinPsycasts
{
    // A buff whose stat modifiers scale continuously instead of stepping through severity stages.
    // The def declares one stage holding the values at scale 1; CurStage returns a rebuilt copy
    // with everything multiplied through. The copy is cached and only rebuilt when scale changes.
    public class Hediff_ScaledBuff : Hediff_Ability
    {
        private static readonly FieldInfo[] StageFields =
            typeof(HediffStage).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public float scale = 1f;

        private HediffStage cachedStage;
        private float cachedScale = float.NaN;
        private string cachedLabelSuffix;

        public float Scale => Mathf.Max(0f, scale);

        public override HediffStage CurStage
        {
            get
            {
                HediffStage source = base.CurStage;
                if (source == null) return null;

                float s = Scale;
                if (cachedStage == null || cachedScale != s)
                {
                    cachedScale = s;
                    cachedStage = BuildScaledStage(source, s);
                }

                return cachedStage;
            }
        }

        // notifyHealth is false during load: the health tracker is still being assembled at
        // PostLoadInit, and dropping the cache is enough there.
        public void Notify_ScaleChanged(bool notifyHealth = true)
        {
            cachedStage = null;
            cachedScale = float.NaN;
            cachedLabelSuffix = null;
            if (notifyHealth && pawn?.health?.hediffSet != null) pawn.health.Notify_HediffChanged(this);
        }

        private static HediffStage BuildScaledStage(HediffStage source, float s)
        {
            HediffStage stage = new HediffStage();
            for (int i = 0; i < StageFields.Length; i++)
            {
                FieldInfo field = StageFields[i];
                if (field.IsLiteral || field.IsInitOnly) continue;
                field.SetValue(stage, field.GetValue(source));
            }

            stage.statOffsets = ScaleOffsets(source.statOffsets, s);
            stage.statFactors = ScaleFactors(source.statFactors, s);
            stage.capMods = ScaleCapMods(source.capMods, s);
            return stage;
        }

        private static List<StatModifier> ScaleOffsets(List<StatModifier> source, float s)
        {
            if (source == null) return null;
            List<StatModifier> result = new List<StatModifier>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(new StatModifier { stat = source[i].stat, value = source[i].value * s });
            return result;
        }

        // A factor of 1 means no change, so it is the distance from 1 that scales.
        private static List<StatModifier> ScaleFactors(List<StatModifier> source, float s)
        {
            if (source == null) return null;
            List<StatModifier> result = new List<StatModifier>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(new StatModifier { stat = source[i].stat, value = 1f + (source[i].value - 1f) * s });
            return result;
        }

        private static List<PawnCapacityModifier> ScaleCapMods(List<PawnCapacityModifier> source, float s)
        {
            if (source == null) return null;
            List<PawnCapacityModifier> result = new List<PawnCapacityModifier>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                result.Add(new PawnCapacityModifier
                {
                    capacity = source[i].capacity,
                    offset = source[i].offset * s,
                    postFactor = 1f + (source[i].postFactor - 1f) * s,
                    setMax = source[i].setMax
                });
            }

            return result;
        }

        // Cached: the health tab asks for this every frame it is open.
        public override string LabelInBrackets
        {
            get
            {
                cachedLabelSuffix ??= "DZ_Paladin_Strength".Translate(Scale.ToStringPercent());
                string label = base.LabelInBrackets;
                return label.NullOrEmpty() ? cachedLabelSuffix : label + ", " + cachedLabelSuffix;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref scale, "scale", 1f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) Notify_ScaleChanged(false);
        }
    }

    // Applies its hediff at a strength taken from the caster's psychic sensitivity, captured once
    // at cast. Duration still comes from the def's durationTime and stat factors, via VEF.
    public class Ability_ScaledBuff : Ability
    {
        public override Hediff ApplyHediff(Pawn targetPawn, HediffDef hediffDef, BodyPartRecord bodyPart, int duration,
            float severity)
        {
            Hediff hediff = base.ApplyHediff(targetPawn, hediffDef, bodyPart, duration, severity);
            if (hediff is Hediff_ScaledBuff scaled)
            {
                scaled.scale = PaladinUtility.Sensitivity(pawn);
                scaled.Notify_ScaleChanged();
            }

            return hediff;
        }
    }
}
