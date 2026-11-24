using System;
using UnityEngine;

namespace FireworkStudio
{
    public enum SparklerTint { WarmGold, Silver, Rose, Aurora, Custom }

    [Serializable]
    public sealed class SparklerOptions
    {
        [Tooltip("Only affects the sparkler. Rockets and terminal crackle are unchanged.")]
        public SparklerTint tint = SparklerTint.WarmGold;
        [ColorUsage(false, false)] public Color customTint = new Color(1f, .55f, .22f);
        [Range(.35f, 2f)] public float brightness = 1f;
        [Range(.35f, 1.8f)] public float density = 1f;
        [Range(.65f, 1.7f)] public float sparkSize = 1f;
        public bool lightWriting = true;
        [Range(.15f, .85f)] public float afterglowSeconds = .48f;
        [Range(.002f, .012f)] public float writingWidth = .0045f;
        [Range(.0f, .65f)] public float smokeAmount = .18f;
        public bool burnDown = true;
        public bool localLight = true;
        public bool audio = true;
        [Range(0f, .4f)] public float audioVolume = .10f;
        [Tooltip("Clear the ribbon after teleports/tracking jumps, in metres per frame.")]
        [Range(.2f, 2f)] public float teleportDistance = .65f;

        public SparklerOptions CopyClamped()
        {
            return new SparklerOptions {
                tint = Enum.IsDefined(typeof(SparklerTint), tint) ? tint : SparklerTint.WarmGold,
                customTint = new Color(Safe(customTint.r,1f,0,1),Safe(customTint.g,.55f,0,1),Safe(customTint.b,.22f,0,1),1),
                brightness = Safe(brightness,1f,.35f,2f), density = Safe(density,1f,.35f,1.8f),
                sparkSize = Safe(sparkSize,1f,.65f,1.7f), lightWriting = lightWriting,
                afterglowSeconds = Safe(afterglowSeconds,.48f,.15f,.85f),
                writingWidth = Safe(writingWidth,.0045f,.002f,.012f),
                smokeAmount = Safe(smokeAmount,.18f,0f,.65f), burnDown = burnDown,
                localLight = localLight, audio = audio, audioVolume = Safe(audioVolume,.1f,0f,.4f),
                teleportDistance = Safe(teleportDistance,.65f,.2f,2f)
            };
        }
        public Color BaseTint(float phase = 0f)
        {
            switch(tint)
            {
                case SparklerTint.Silver: return new Color(.72f,.85f,1f);
                case SparklerTint.Rose: return new Color(1f,.25f,.48f);
                case SparklerTint.Aurora: return Color.HSVToRGB(Mathf.Repeat(.44f+phase*.11f,1f),.70f,1f);
                case SparklerTint.Custom: return new Color(customTint.r,customTint.g,customTint.b,1f);
                default: return new Color(1f,.67f,.19f);
            }
        }
        public static float Safe(float value,float fallback,float min,float max)
        { return float.IsNaN(value)||float.IsInfinity(value) ? fallback : Mathf.Clamp(value,min,max); }
    }
}
