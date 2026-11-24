using System;
using System.Collections.Generic;
using UnityEngine;

namespace FireworkStudio
{
    // Positions, not normalized launch directions. This preserves stars, hearts and inner rings.
    public static class CrackleOnlyLayout
    {
        public struct Pop
        {
            public Vector3 localPosition;
            public float time;
            public int cluster;
            public bool repeat;
        }

        public static Vector3 Sample(FireworkPattern pattern, int index, int count)
        {
            count = Mathf.Max(1, count);
            int i = Mathf.Clamp(index, 0, count - 1);
            float u = (i + .5f) / count;
            float a = 2f * Mathf.PI * u;
            switch (pattern)
            {
                case FireworkPattern.Ring: return Ring(a, 1f);
                case FireworkPattern.DoubleRing:
                {
                    int ring = i % 2;
                    int n = (count + 1 - ring) / 2;
                    return Ring(2f * Mathf.PI * ((i / 2f - ring * .5f) + .5f) / Mathf.Max(1, n), ring == 0 ? 1f : .60f);
                }
                case FireworkPattern.Flower:
                    return Ring(a, .68f + .32f * Mathf.Cos(6f * a));
                case FireworkPattern.Peony: return Sphere(i, count);
                case FireworkPattern.Willow:
                {
                    float angle = (i % 9) * 2f * Mathf.PI / 9f;
                    float t = (.5f + i / 9) / Mathf.Ceil(count / 9f);
                    return new Vector3(Mathf.Cos(angle) * t, .62f - 1.15f * t * t, Mathf.Sin(angle) * t);
                }
                case FireworkPattern.CometBurst:
                {
                    int spoke = i % 8;
                    float t = (.5f + i / 8) / Mathf.Ceil(count / 8f);
                    return Sphere(spoke, 8) * (.25f + .75f * t);
                }
                case FireworkPattern.Star: return Polygon(u, 10, true);
                case FireworkPattern.Heart:
                {
                    float x = 16f * Mathf.Pow(Mathf.Sin(a), 3f);
                    float y = 13f * Mathf.Cos(a) - 5f * Mathf.Cos(2f*a) - 2f * Mathf.Cos(3f*a) - Mathf.Cos(4f*a);
                    return new Vector3(x / 17f, (y + 2f) / 17f, 0f);
                }
                case FireworkPattern.Spiral:
                    return Ring(6f * Mathf.PI * u, .14f + .86f * u);
                case FireworkPattern.Crown:
                    return new Vector3(Mathf.Cos(a) * .85f, -.2f + .72f * Mathf.Pow(Mathf.Abs(Mathf.Sin(5f*a)), 3f), Mathf.Sin(a) * .85f);
                case FireworkPattern.Snowflake:
                {
                    int arm = i % 6, k = i / 6;
                    int group = k % 3;
                    float t = (k / 3f + .5f) / Mathf.Max(1f, Mathf.Ceil(count / 18f));
                    float angle = arm * Mathf.PI / 3f;
                    Vector3 ray = Ring(angle, 1f);
                    if (group == 0) return ray * Mathf.Lerp(.15f, 1f, Mathf.Clamp01(t));
                    return ray * .58f + Ring(angle + (group == 1 ? 1f : -1f) * Mathf.PI / 3f, .36f * Mathf.Clamp01(t));
                }
                case FireworkPattern.Butterfly:
                {
                    float r = (Mathf.Exp(Mathf.Cos(a)) - 2f * Mathf.Cos(4f*a) - Mathf.Pow(Mathf.Sin(a/12f), 5f)) / 3.9f;
                    return new Vector3(Mathf.Sin(a) * r, Mathf.Cos(a) * r, 0f);
                }
                case FireworkPattern.Palm:
                {
                    float angle = i % 8 * Mathf.PI * .25f;
                    float t = (.5f + i / 8) / Mathf.Ceil(count / 8f);
                    return new Vector3(Mathf.Cos(angle) * t, .2f + .65f * Mathf.Sin(Mathf.PI*t) - .3f*t, Mathf.Sin(angle) * t);
                }
                case FireworkPattern.Galaxy:
                {
                    int arm = i % 3;
                    float t = (.5f + i / 3) / Mathf.Ceil(count / 3f);
                    float turn = 1.6f * Mathf.PI * t + arm * 2f * Mathf.PI / 3f;
                    Vector3 v = Ring(turn, .18f + .82f * t);
                    v.z = Mathf.Sin(turn) * .08f;
                    return v;
                }
                case FireworkPattern.Diamond: return Polygon(u, 4, false);
                case FireworkPattern.FourLeaf: return Ring(a, .24f + .76f * Mathf.Abs(Mathf.Sin(a*2f)));
                case FireworkPattern.Cross:
                {
                    float t = (.5f + i / 4) / Mathf.Ceil(count / 4f);
                    return Ring((i % 4) * Mathf.PI / 2f, .12f + .88f * t);
                }
                case FireworkPattern.Fan:
                {
                    float angle = Mathf.Lerp(-1.3f, 1.3f, u);
                    return new Vector3(Mathf.Sin(angle), Mathf.Cos(angle) * .85f - .2f, 0f);
                }
                case FireworkPattern.DoubleFlower:
                {
                    int ring = i % 2, n = (count + 1 - ring) / 2;
                    float angle = 2f * Mathf.PI * ((i / 2f - ring*.5f) + .5f) / Mathf.Max(1,n);
                    return Ring(angle, (.64f + .36f * Mathf.Cos(angle*7f)) * (ring == 0 ? 1f : .57f));
                }
                default: return Sphere(i, count);
            }
        }

        // Use real world-space depth for effect geometry; Sample remains the flat design outline.
        public static Vector3 SampleVolume(FireworkPattern pattern, int index, int count)
        {
            count = Mathf.Max(1, count);
            int i = Mathf.Clamp(index, 0, count - 1);
            Vector3 outline = Sample(pattern, i, count);
            switch (pattern)
            {
                // These layouts already distribute particles in three dimensions.
                case FireworkPattern.Peony:
                case FireworkPattern.Willow:
                case FireworkPattern.CometBurst:
                case FireworkPattern.Crown:
                case FireworkPattern.Palm:
                    return outline;
            }

            float phase = i * 2.39996323f;
            if (pattern == FireworkPattern.Ring || pattern == FireworkPattern.DoubleRing)
            {
                // A torus has a circular silhouette and visible thickness from the side.
                float radius = outline.magnitude;
                float tube = pattern == FireworkPattern.Ring ? 0.20f : 0.13f;
                Vector3 radial = outline.normalized;
                return radial * (radius + tube * Mathf.Cos(phase))
                    + Vector3.forward * (tube * Mathf.Sin(phase));
            }

            // A rounded shell: one third preserves the silhouette; the rest fills the
            // front and back caps. Interleaved depth avoids stacked parallel sheets.
            float latitude = i % 3 == 0 ? 0.12f * Mathf.Sin(phase)
                : (Mathf.Repeat((i + 0.5f) * 0.618033989f, 1f) * 1.8f - 0.9f);
            float section = Mathf.Sqrt(Mathf.Max(0f, 1f - latitude * latitude));
            return new Vector3(outline.x * section, outline.y * section,
                outline.z + latitude * 0.72f);
        }

        static Vector3 Ring(float angle, float radius)
        { return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f); }
        static Vector3 Polygon(float u, int corners, bool alternate)
        {
            float t = u * corners;
            int k = Mathf.FloorToInt(t);
            Vector3 a = Ring(Mathf.PI * .5f + k * 2f*Mathf.PI/corners, alternate && k % 2 == 1 ? .43f : 1f);
            Vector3 b = Ring(Mathf.PI * .5f + (k+1) * 2f*Mathf.PI/corners, alternate && (k+1) % 2 == 1 ? .43f : 1f);
            return Vector3.Lerp(a, b, t-k);
        }
        static Vector3 Sphere(int i, int n)
        {
            float y = 1f - (i + .5f) * 2f / Mathf.Max(1,n);
            float r = Mathf.Sqrt(Mathf.Max(0f,1f-y*y));
            return new Vector3(Mathf.Cos(i*2.39996323f)*r, y, Mathf.Sin(i*2.39996323f)*r);
        }
        static float Next(System.Random rng) { return (float)rng.NextDouble(); }

        public static List<Pop> BuildPlan(FireworkPattern pattern, CrackleOnlyOptions rawOptions, int seed)
        {
            CrackleOnlyOptions o = (rawOptions ?? new CrackleOnlyOptions()).CopyClamped();
            // Allow one repeat for EVERY cluster without exceeding the per-shot budget.
            int n = Mathf.Min(o.clusters, o.maxParticlesPerShot / (2 * (o.sparksPerPop + 1)));
            n = Mathf.Max(1,n);
            var rng = new System.Random(seed);
            var order = new int[n];
            for (int i=0;i<n;i++) order[i]=i;
            for (int i=n-1;i>0;i--) { int j=rng.Next(i+1); int temp=order[i]; order[i]=order[j]; order[j]=temp; }
            var result = new List<Pop>(n*2);
            for (int step=0;step<n;step++)
            {
                int i=order[step];
                Vector3 p=SampleVolume(pattern,i,n)*o.radius;
                Vector3 jitter=new Vector3(Next(rng)*2f-1f,Next(rng)*2f-1f,Next(rng)*2f-1f)*o.positionJitter*o.radius;
                p+=jitter;
                float time=o.startDelay + o.spreadDuration*(step + Next(rng)*.8f)/n;
                result.Add(new Pop { localPosition=p, time=time, cluster=i, repeat=false });
                if (Next(rng)<o.repeatProbability)
                    result.Add(new Pop { localPosition=p+jitter*.35f, time=time+.09f+Next(rng)*.20f, cluster=i, repeat=true });
            }
            result.Sort((a,b) => { int c=a.time.CompareTo(b.time); return c!=0 ? c : a.cluster.CompareTo(b.cluster); });
            return result;
        }
    }
}
