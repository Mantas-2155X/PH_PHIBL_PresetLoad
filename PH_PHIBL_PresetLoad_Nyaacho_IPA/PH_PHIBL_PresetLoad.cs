using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Linq;
using Harmony;
using IllusionPlugin;
using MessagePack;
using PHIBL;

using UnityEngine;
using UnityEngine.SceneManagement;

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
    
    public class IPA_PHIBL_PresetLoad : IPlugin
    {
        public string Name => "PHIBL Preset Load (IPA, for Nyaacho custom PHIBL)";
        public string Version => "1.2.1";
        
        private const KeyCode saveKey = KeyCode.S;
        private const KeyCode loadKey = KeyCode.L;

        private static readonly string[] activeScenes =
        {
            "ADVScene",
            "H",
            "EditMode",
            "EditScene",
            "SelectScene"
        };
        
        private static PHIBL.PHIBL phIBL;
        
        public void OnApplicationStart()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("PHIBL_PresetLoad");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void OnUpdate()
        {
            if (phIBL == null)
                return;

            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(saveKey))
                SaveSettings();
            
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(loadKey))
                LoadSettings();
        }
        
        public void OnLevelWasLoaded(int level) {
            phIBL = phIBL != null ? phIBL : UnityEngine.Object.FindObjectOfType<PHIBL.PHIBL>();
            if (phIBL == null) 
                return;
            
            string sceneName = SceneManager.GetActiveScene().name;
            
            if (!activeScenes.Contains(sceneName)) 
                return;

            LoadSettings();
        }

        private static void LoadSettings()
        {
            if (phIBL == null)
                return;

            FileInfo file = new FileInfo(@"UserData/PHIBL_MainGame.extdata");
            if (!file.Exists) 
                return;

            byte[] bytes = File.ReadAllBytes(file.FullName);

            ProfileData data = LZ4MessagePackSerializer.Deserialize<ProfileData>(bytes, CustomCompositeResolver.Instance);
            phIBL.StartCoroutine(ApplySettings(data, phIBL));
        }
        
        private static void SaveSettings()
        {
            if (phIBL == null)
                return;

            Traverse trav = Traverse.Create(phIBL);

            ReflectionProbe probe = trav.Field("probeComponent").GetValue<ReflectionProbe>();
            int selectedUserLut = trav.Field("selectedUserLut").GetValue<int>();

            PHIBL.PostProcessing.Utilities.PostProcessingController PPCtrl_obj = trav.Field("PPCtrl").GetValue<PHIBL.PostProcessing.Utilities.PostProcessingController>();

            ProfileData data = new ProfileData {
                profile = phIBL.Snapshot(),
                nipples = trav.Field("nippleSSS").GetValue<float>(),
                shadowDistance = QualitySettings.shadowDistance,
                reflectionBounces = RenderSettings.reflectionBounces,
                probeResolution = probe.resolution,
                probeIntensity = probe.intensity,
                enabledLUT = PPCtrl_obj.enableUserLut,
                selectedLUT = selectedUserLut,
                contributionLUT = PPCtrl_obj.userLut.contribution
            };

            byte[] bytes = LZ4MessagePackSerializer.Serialize(data, CustomCompositeResolver.Instance);
            File.WriteAllBytes(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\UserData\temp_PHIBL_MainGame.extdata", bytes);
            
            Console.WriteLine("==============Saved temp_PHIBL_MainGame.extdata==============");
        }
        
        private static IEnumerator ApplySettings(ProfileData data, PHIBL.PHIBL pHIBL) {
            yield return new WaitForSeconds(1.5f);

            Profile profile = data.profile;
            float nipples = data.nipples;

            Traverse traverse = Traverse.Create(pHIBL);
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

            QualitySettings.shadowDistance = data.shadowDistance;
            RenderSettings.reflectionBounces = data.reflectionBounces;

            ReflectionProbe probe = traverse.Field("probeComponent").GetValue<ReflectionProbe>();
            probe.resolution = data.probeResolution;
            probe.intensity = data.probeIntensity;

            if (data.enabledLUT) {
                PHIBL.PostProcessing.Utilities.PostProcessingController PPCtrl_obj = traverse.Field("PPCtrl").GetValue<PHIBL.PostProcessing.Utilities.PostProcessingController>();
                traverse.Field("selectedUserLut").SetValue(data.selectedLUT);
                
                Texture2D texture2D = new Texture2D(1024, 32, TextureFormat.ARGB32, false, true);
                byte[] Ldata = File.ReadAllBytes(PHIBL.UserData.Path + "PHIBL/Settings/" + traverse.Field("LutFileNames").GetValue<string[]>()[data.selectedLUT] + ".png");
                texture2D.LoadImage(Ldata);
                texture2D.filterMode = FilterMode.Trilinear;
                texture2D.anisoLevel = 0;
                texture2D.wrapMode = TextureWrapMode.Repeat;

                PPCtrl_obj.userLut.lut = texture2D;
                PPCtrl_obj.userLut.contribution = data.contributionLUT;
                PPCtrl_obj.controlUserLut = true;
                PPCtrl_obj.enableUserLut = true;
            }

            Console.WriteLine("==============Loaded PHIBL_MainGame.extdata==============");
        }

        public void OnFixedUpdate() { }
        public void OnLevelWasInitialized(int level) { }
        public void OnApplicationQuit() { }
    }
}