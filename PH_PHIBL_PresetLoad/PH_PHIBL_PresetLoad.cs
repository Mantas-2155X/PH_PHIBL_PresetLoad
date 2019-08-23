using System;
using System.IO;
using IllusionPlugin;
using PHIBL;
using UnityEngine;
using Harmony;
using System.Collections;
using UnityEngine.SceneManagement;
using MessagePack;

namespace PH_PHIBL_PresetLoad {
    public class PH_PHIBL_PresetLoad : IPlugin {
        public string Name {
            get {
                return "PlayHome PHIBL Preset Load";
            }
        }

        public string Version {
            get {
                return "1.0";
            }
        }

        public void OnFixedUpdate() {
        }

        public void OnLevelWasInitialized(int level) {
        }

        public IEnumerator SetPHIBLSettings(Profile profile, PHIBL.PHIBL pHIBL) {
            yield return new WaitForSeconds(1.5f);

            Traverse traverse = Traverse.Create(pHIBL);
            traverse.Method("LoadPostProcessingProfile", profile).GetValue();

            DeferredShadingUtils deferredShading = traverse.Field("deferredShading").GetValue<DeferredShadingUtils>();

            PHIBL.AlloyDeferredRendererPlus SSSSS = deferredShading.SSSSS;

            Traverse trav = Traverse.Create(SSSSS);
            trav.Field("TransmissionSettings").SetValue(profile.TransmissionSettings);
            trav.Field("SkinSettings").SetValue(profile.SkinSettings);
            trav.Method("Reset").GetValue();

            DeferredShadingUtils.SetTessellation(profile.phong, profile.edgeLength);

            Console.WriteLine("==============Loaded default PHIBL default_PHIBL_maingame.extdata==============");
        }

        public void OnLevelWasLoaded(int level) {
            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName == "H" || sceneName == "EditMode" || sceneName == "EditScene" || sceneName == "SelectScene") {
                FileInfo file = new FileInfo(@"UserData/default_PHIBL_maingame.extdata");

                if (file.Exists && file != null) {
                    PHIBL.PHIBL pHIBL = UnityEngine.Object.FindObjectOfType<PHIBL.PHIBL>();

                    if (pHIBL != null) {
                        byte[] bytes = File.ReadAllBytes(file.FullName);
                        Profile profile = LZ4MessagePackSerializer.Deserialize<Profile>(bytes, CustomCompositeResolver.Instance);
                        pHIBL.StartCoroutine(SetPHIBLSettings(profile, pHIBL));
                    }
                }
            }
        }

        public void OnUpdate() {
        }

        public void OnApplicationQuit() {
        }

        public void OnApplicationStart() {
        }
    }
}
