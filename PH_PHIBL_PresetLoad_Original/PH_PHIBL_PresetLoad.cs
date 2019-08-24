using System;
using System.IO;
using System.Reflection;
using IllusionPlugin;
using PHIBL;
using UnityEngine;
using Harmony;
using System.Collections;
using UnityEngine.SceneManagement;
using MessagePack;

namespace PH_PHIBL_PresetLoad {
    [MessagePackObject(true)]
    public class PH_PHIBL_Data {
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

    public class PH_PHIBL_PresetLoad : IPlugin {
        public string Name {
            get {
                return "PlayHome PHIBL Preset Load";
            }
        }

        public string Version {
            get {
                return "1.1.1";
            }
        }

        public void OnFixedUpdate() {
        }

        public void OnLevelWasInitialized(int level) {
        }

        public void OnUpdate() {
        }

        public void OnApplicationQuit() {
        }

        [HarmonyPatch(typeof(Profile), "Save")]
        class Patch_PHIBL_Profile_Save {
            static void Postfix(string path) {
                PHIBL.PHIBL pHIBL = UnityEngine.Object.FindObjectOfType<PHIBL.PHIBL>();
                Traverse trave = Traverse.Create(pHIBL);

                ReflectionProbe probe = trave.Field("probeComponent").GetValue<ReflectionProbe>();
                int selectedUserLut = trave.Field("selectedUserLut").GetValue<int>();

                PHIBL.PostProcessing.Utilities.PostProcessingController PPCtrl_obj = trave.Field("PPCtrl").GetValue<PHIBL.PostProcessing.Utilities.PostProcessingController>();

                PH_PHIBL_Data data = new PH_PHIBL_Data {
                    profile = pHIBL.Snapshot(),
                    nipples = trave.Field("nippleSSS").GetValue<float>(),
                    shadowDistance = QualitySettings.shadowDistance,
                    reflectionBounces = RenderSettings.reflectionBounces,
                    probeResolution = probe.resolution,
                    probeIntensity = probe.intensity,
                    enabledLUT = PPCtrl_obj.enableUserLut,
                    selectedLUT = selectedUserLut,
                    contributionLUT = PPCtrl_obj.userLut.contribution
                };

                byte[] bytes = LZ4MessagePackSerializer.Serialize(data);
                File.WriteAllBytes(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\UserData\temp_PHIBL_MainGame.extdata", bytes);

                Console.WriteLine("==============Saved temp_PHIBL_MainGame.extdata==============");
            }
        }

        public IEnumerator SetPHIBLSettings(PH_PHIBL_Data data, PHIBL.PHIBL pHIBL) {
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

                Texture2D texture2D = new Texture2D(1024, 32, TextureFormat.ARGB32, mipmap: false, linear: true);
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

        public void OnLevelWasLoaded(int level) {
            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName == "H" || sceneName == "EditMode" || sceneName == "EditScene" || sceneName == "SelectScene") {
                FileInfo file = new FileInfo(@"UserData/PHIBL_MainGame.extdata");

                if (file.Exists && file != null) {
                    PHIBL.PHIBL pHIBL = UnityEngine.Object.FindObjectOfType<PHIBL.PHIBL>();

                    if (pHIBL != null) {
                        byte[] bytes = File.ReadAllBytes(file.FullName);

                        PH_PHIBL_Data data = LZ4MessagePackSerializer.Deserialize<PH_PHIBL_Data>(bytes);
                        pHIBL.StartCoroutine(SetPHIBLSettings(data, pHIBL));
                    }
                }
            }
        }

        public void OnApplicationStart() {
            HarmonyInstance harmony = HarmonyInstance.Create("PH_PHIBL_PresetLoad");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
