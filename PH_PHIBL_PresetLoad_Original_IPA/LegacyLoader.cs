using System;
using System.Collections;
using System.IO;
using Harmony;
using PHIBL;
using MessagePack;
using UnityEngine;
// ReSharper disable UnassignedField.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace PH_PHIBL_PresetLoad
{
    [MessagePackObject(true)]
    public class ProfileData {
        public Profile profile;

        public float nipples;

        public float shadowDistance;

        public int reflectionBounces;
        public int probeResolution;
        public float probeIntensity;

        public bool enabledLUT;
        public int selectedLUT;
        public float contributionLUT;

        public bool enableDithering;
    }

    public static class LegacyLoader
    {
        public static void LoadPreset()
        {
            FileInfo file = new FileInfo(@"UserData/PHIBL_MainGame.extdata");
            if (!file.Exists) 
                return;

            byte[] bytes = File.ReadAllBytes(file.FullName);

            ProfileData data = LZ4MessagePackSerializer.Deserialize<ProfileData>(bytes);
            IPA_PHIBL_PresetLoad.phIBL.StartCoroutine(LoadLegacyPreset(data, IPA_PHIBL_PresetLoad.phIBL));
        }
        
        private static IEnumerator LoadLegacyPreset(ProfileData preset, PHIBL.PHIBL phIBL)
        {
            yield return new WaitForSeconds(.1f);

            Profile profile = preset.profile;
            float nipples = preset.nipples;
            Traverse traverse = Traverse.Create(phIBL);
            traverse.Method("LoadPostProcessingProfile", profile).GetValue();
            DeferredShadingUtils deferredShading = traverse.Field("deferredShading").GetValue<DeferredShadingUtils>();
            PHIBL.AlloyDeferredRendererPlus SSSSS = deferredShading.SSSSS;
            Traverse trav = Traverse.Create(SSSSS);
            trav.Field("TransmissionSettings").SetValue(profile.TransmissionSettings);
            trav.Field("SkinSettings").SetValue(profile.SkinSettings);
            trav.Method("Reset").GetValue();
            traverse.Field("phong").SetValue(profile.phong);
            traverse.Field("edgelength").SetValue(profile.edgeLength);
            DeferredShadingUtils.SetTessellation(profile.phong, profile.edgeLength);
            traverse.Field("nippleSSS").SetValue(nipples);
            Shader.SetGlobalFloat(Shader.PropertyToID("_AlphaSSS"), nipples);
            QualitySettings.shadowDistance = preset.shadowDistance;
            RenderSettings.reflectionBounces = preset.reflectionBounces;
            ReflectionProbe probe = traverse.Field("probeComponent").GetValue<ReflectionProbe>();
            probe.resolution = preset.probeResolution;
            probe.intensity = preset.probeIntensity;

            var PPCtrl_obj = traverse.Field("PPCtrl").GetValue<PHIBL.PostProcessing.Utilities.PostProcessingController>();
            PPCtrl_obj.enableDither = preset.enableDithering;
            
            Console.WriteLine("[PHIBL_PresetLoad] Loaded LEGACY preset");
        }
    }
}