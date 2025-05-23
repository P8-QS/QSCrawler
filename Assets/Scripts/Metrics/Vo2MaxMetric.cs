using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Data.Models;
using Effects;
using UnityEngine;

namespace Metrics
{
    public class Vo2MaxMetric : IMetric
    {
        public IReadOnlyCollection<Vo2MaxRecord> Data { get; }
        public string Name => "VO2 Max";
        public List<IEffect> Effects { get; } = new();
        public Sprite Icon { get; }

        private readonly int _level;
        private readonly decimal _vo2MaxAverage;

        public Vo2MaxMetric()
        {
            if (UserMetricsHandler.Instance.Vo2MaxRecords is null) return;
            Data = UserMetricsHandler.Instance.Vo2MaxRecords;
            Icon = SpriteManager.Instance.GetSprite("metric_vo2_max");
            
            _vo2MaxAverage = Data.Average(r => r.Vo2MillilitersPerMinuteKilogram);

            if (Data is null) return;
            _level = _vo2MaxAverage switch
            {
                > 45 => 3,
                > 35 => 2,
                _ => 1
            };
            

            Effects.Add(new ToxicPuddleEffect(SpriteManager.Instance.GetSprite(LevelToEffectIconName()),
                _level));
        }

        public string Text()
        {
            return
                $"Your VO2 max is <b>{_vo2MaxAverage} (mL/kg/min)</b>. This gives you {(this as IMetric).EffectsToString()}.";
        }

        private string LevelToString()
        {
            return _level switch
            {
                1 => "below average",
                2 => "average",
                3 => "above average",
                _ => "unknown"
            };
        }

        private string LevelToEffectIconName()
        {
            return _level switch
            {
                1 => "effect_slime_puddle_negative",
                2 => "effect_slime_puddle_negative",
                3 => "effect_slime_puddle_positive",
                _ => "effect_slime_puddle_neutral"
            };
        }

        public string Description()
        {
            return
                $"Your VO2 max is <b>{_vo2MaxAverage} (mL/kg/min)</b>. This is considered {LevelToString()}. VO2 max is a measure of your body's ability to utilize oxygen during exercise. ";
        }
    }
}