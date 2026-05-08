using System;
using System.Collections.Generic;
using System.Text;
using CoreClasses.Models;

namespace CoreClasses.Models
{
    public class AnxietyBar
    {
        [Newtonsoft.Json.JsonProperty]
        public float Value { get; private set; } = 50f;
        public float Max { get; } = 100f;
        public float DangerThreshold { get; } = 75f;

        public bool IsCritical => Value >= DangerThreshold;
        public bool IsMaxed => Value >= Max;

        public void Increase(float amount)
        {
            Value = Math.Clamp(Value + amount, 0f, Max);
        }

        public void Decrease(float amount)
        {
            Value = Math.Clamp(Value - amount, 0f, Max);
        }
        public AnxietyState GetState()
        {
            if (Value <= 25f) return AnxietyState.Freeze;
            if (Value <= 40f) return AnxietyState.Low;
            if (Value <= 60f) return AnxietyState.Balanced;
            if (Value <= 75f) return AnxietyState.High;

            return AnxietyState.Panic;
        }
        public void MoveTowardBalance(float amount)
        {
            float target = 50f;
            if (Math.Abs(Value - target) < amount)
                Value = target;
            else if (Value > target)
                Value -= amount;
            else
                Value += amount;
        }

        // לתצוגת UI
        public float GetPercentage()
        {
            return Value / Max;
        }
        // לשמירת מצב
        public void SetValue(float newValue)
        {
            Value = Math.Clamp(newValue, 0f, Max);
        }
        // איפוס המצב לאמצע
        public void Reset()
        {
            Value = 50f;
        }
    }
}
