using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FireworkStudio;

namespace FireworkStudio.EditorTools
{
    public static class FireworkTrioValidation
    {
        [MenuItem("Tools/Firework Studio/Diagnostics/Validate Three Types (No Playback)")]
        public static void Run()
        {
            int checks=0;
            try
            {
                Check((int)FireworkShowMode.Combined==0,"Backward-compatible mode value",ref checks);
                Check(FireworkShowRules.UsesMain(FireworkShowMode.TrailsOnly),"Trails main ON",ref checks);
                Check(!FireworkShowRules.UsesLegacyAfterburst(FireworkShowMode.TrailsOnly),"Trails afterburst OFF",ref checks);
                Check(!FireworkShowRules.UsesIndependentCrackle(FireworkShowMode.TrailsOnly),"Trails crackle-only OFF",ref checks);
                Check(!FireworkShowRules.UsesMain(FireworkShowMode.CrackleOnly),"Crackle main OFF",ref checks);
                Check(!FireworkShowRules.UsesLegacyAfterburst(FireworkShowMode.CrackleOnly),"Crackle old sequence OFF",ref checks);
                Check(FireworkShowRules.UsesIndependentCrackle(FireworkShowMode.CrackleOnly),"Independent sequence ON",ref checks);
                Check(FireworkShowRules.UsesMain(FireworkShowMode.Combined)&&FireworkShowRules.UsesLegacyAfterburst(FireworkShowMode.Combined),"Combined unchanged routing",ref checks);
                Check(Enum.GetValues(typeof(FireworkPattern)).Length==19,"19 patterns",ref checks);
                Check(Enum.GetValues(typeof(FireworkPalette)).Length==18,"18 palettes",ref checks);
                foreach(FireworkPalette p in Enum.GetValues(typeof(FireworkPalette)))
                    Check(FireworkPaletteUtil.GetColors(p).Length>=3,"Palette "+p,ref checks);
                foreach(FireworkPattern p in Enum.GetValues(typeof(FireworkPattern)))
                {
                    foreach(int n in new[]{24,64,120})
                    for(int i=0;i<n;i++)
                    {
                        Vector3 v=CrackleOnlyLayout.Sample(p,i,n);
                        Check(IsFinite(v)&&v.magnitude<=1.7f,"Finite bounded shape "+p,ref checks);
                    }
                    foreach(int budget in new[]{256,1400,4096})
                    foreach(int seed in new[]{1,42,937})
                    {
                        var options=new CrackleOnlyOptions { maxParticlesPerShot=budget };
                        var a=CrackleOnlyLayout.BuildPlan(p,options,seed);
                        var b=CrackleOnlyLayout.BuildPlan(p,options,seed);
                        Check(a.Count==b.Count&&a.Count>0,"Plan count "+p,ref checks);
                        for(int i=0;i<a.Count;i++)
                        {
                            Check(a[i].time==b[i].time&&a[i].localPosition==b[i].localPosition,"Deterministic plan",ref checks);
                            Check(i==0||a[i].time>=a[i-1].time,"Sorted irregular timing",ref checks);
                            Check(a[i].time>=options.startDelay&&a[i].time<options.startDelay+options.spreadDuration+.30f,"Timing bound",ref checks);
                        }
                        var effective=options.CopyClamped();
                        Check(a.Count*(effective.sparksPerPop+1)<=budget,"Total particle budget",ref checks);
                        // Absolute origin check for a rotated shape far from (0,0,0).
                        Vector3 origin=new Vector3(120,48,-75);
                        Quaternion rotation=Quaternion.Euler(-32,51,9);
                        Vector3 world=origin+rotation*a[0].localPosition;
                        Check((world-origin).magnitude<=effective.radius*1.9f,"World-space sky anchor",ref checks);
                    }
                }
                Vector3 outer=CrackleOnlyLayout.Sample(FireworkPattern.DoubleRing,0,64);
                Vector3 inner=CrackleOnlyLayout.Sample(FireworkPattern.DoubleRing,1,64);
                Check(Mathf.Abs(outer.magnitude-1f)<.001f&&Mathf.Abs(inner.magnitude-.6f)<.001f,"Inner ring keeps its radius",ref checks);
                FireworkTrioInstaller.EnsureShaderReferences();
                AssetDatabase.SaveAssets();
                Debug.Log("[Firework] Passed "+checks+" Unity-side logic/shape checks. This is not a rendered VR image-quality test.");
            }
            catch(Exception e) { Debug.LogError("[Firework] Validation failed after "+checks+" checks: "+e.Message); throw; }
        }
        static bool IsFinite(Vector3 p)
        { return !float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z); }
        static void Check(bool ok,string message,ref int n)
        { n++; if(!ok) throw new InvalidOperationException(message); }
    }
}
