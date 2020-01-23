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
            
            if (preset.enabledLUT)
            {
                string[] names = traverse.Field("LutFileNames").GetValue<string[]>();
                if (names.Length >= preset.selectedLUT)
                {
                    string path = PHIBL.UserData.Path + "PHIBL/Settings/" + names[preset.selectedLUT] + ".png";
                    if (File.Exists(path))
                    {
                        PHIBL.PostProcessing.Utilities.PostProcessingController PPCtrl_obj = traverse.Field("PPCtrl").GetValue<PHIBL.PostProcessing.Utilities.PostProcessingController>();
                        traverse.Field("selectedUserLut").SetValue(preset.selectedLUT);

                        Texture2D texture2D = new Texture2D(1024, 32, TextureFormat.ARGB32, false, true);

                        byte[] Ldata = File.ReadAllBytes(path);
                        texture2D.LoadImage(Ldata);
                        texture2D.filterMode = FilterMode.Trilinear;
                        texture2D.anisoLevel = 0;
                        texture2D.wrapMode = TextureWrapMode.Repeat;

                        PPCtrl_obj.userLut.lut = texture2D;
                        PPCtrl_obj.userLut.contribution = preset.contributionLUT;
                        PPCtrl_obj.controlUserLut = true;
                        PPCtrl_obj.enableUserLut = true;
                    }
                }
            }
            Console.WriteLine("[PHIBL_PresetLoad] Loaded LEGACY preset");
        }
    }
}