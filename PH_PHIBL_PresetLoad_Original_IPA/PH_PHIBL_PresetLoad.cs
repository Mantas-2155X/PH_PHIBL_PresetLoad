using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Harmony;

using IllusionPlugin;
using MessagePack;
using PHIBL;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace PH_PHIBL_PresetLoad
{
    [MessagePackObject(true)]
    public class PresetInfo
    {
        public string name = "preset name";
        public bool[] scenes = new bool[6];
        
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
    
    // ReSharper disable once ClassNeverInstantiated.Global
    public class IPA_PHIBL_PresetLoad : IPlugin
    {
        public string Name => "PHIBL Preset Load (IPA, for original PHIBL)";
        public string Version => "2.0.1";

        public static bool drawUI;
        private static GameObject uiObj;
        public static PHIBL.PHIBL phIBL;

        private const KeyCode uiKey = KeyCode.M;
        
        public static readonly List<PresetInfo> conflicts = new List<PresetInfo>();
        public static readonly List<PresetInfo> presets = new List<PresetInfo>();
        public static readonly Dictionary<int, string> scenes = new Dictionary<int, string>
        {
            {0, "H"},
            {1, "ADVScene"},
            {2, "EditMode"},
            {3, "EditScene"},
            {4, "SelectScene"},
            {5, "Studio"}
        };
        
        public void OnApplicationStart()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("PHIBL_PresetLoad");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            uiObj = new GameObject("PHIBL_PresetLoad_UI");
            uiObj.AddComponent<PHIBL_PresetLoad_UI>();

            UnityEngine.Object.DontDestroyOnLoad(uiObj);
            
            CreateDirectories();
        }

        private static void CreateDirectories()
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Plugins\\PHIBL_PresetLoad");
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Plugins\\PHIBL_PresetLoad\\presets");
        }
        
        private static void SetupPresets()
        {
            presets.Clear();
            conflicts.Clear();

            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\Plugins\\PHIBL_PresetLoad\\presets\\", "*.preset");
            if (files.Length > 0)
            {
                foreach (var filename in files)
                {
                    var file = new FileInfo(filename);
                    if (!file.Exists)
                        continue;

                    PresetInfo preset = LZ4MessagePackSerializer.Deserialize<PresetInfo>(File.ReadAllBytes(file.FullName));
                    if (preset != null)
                        presets.Add(preset);
                }
            }

            foreach (var preset in presets)
            {
                foreach (var _preset in presets.Where(_preset => preset != _preset))
                {
                    for (int i = 0; i < scenes.Count; i++)
                    {
                        if (!preset.scenes[i] || !_preset.scenes[i]) 
                            continue;
                            
                        conflicts.Add(preset);
                        conflicts.Add(_preset);

                        break;
                    }
                }
            }
            
            PHIBL_PresetLoad_UI.selectedPreset = -1;
            PHIBL_PresetLoad_UI.presetName = "preset name";
            PHIBL_PresetLoad_UI.scenes = new bool[6];
        }

        private static IEnumerator LoadPresetDelayed(int presetID)
        {
            yield return new WaitForSeconds(1.5f);

            LoadPreset(presetID);
        }
        
        public static void LoadPreset(int presetID = -1)
        {
            PresetInfo preset = presets.ElementAtOrDefault(presetID);
            if (preset == null)
                return;

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

            Console.WriteLine("[PHIBL_PresetLoad] Loaded preset: " + presets[presetID].name);
        }

        public static void SavePreset(bool[] pScenes = null, string name = "preset name")
        {
            Traverse trav = Traverse.Create(phIBL);

            ReflectionProbe probe = trav.Field("probeComponent").GetValue<ReflectionProbe>();
            int selectedUserLut = 0;

            PHIBL.PostProcessing.Utilities.PostProcessingController PPCtrl_obj = trav.Field("PPCtrl").GetValue<PHIBL.PostProcessing.Utilities.PostProcessingController>();

            PresetInfo preset = new PresetInfo
            {
                name = name,
                scenes = pScenes,
                profile = phIBL.Snapshot(),
                nipples = trav.Field("nippleSSS").GetValue<float>(),
                shadowDistance = QualitySettings.shadowDistance,
                reflectionBounces = RenderSettings.reflectionBounces,
                probeResolution = probe.resolution,
                probeIntensity = probe.intensity,
                enabledLUT = false,
                selectedLUT = selectedUserLut,
                contributionLUT = 0
            };

            File.WriteAllBytes(Directory.GetCurrentDirectory() + "\\Plugins\\PHIBL_PresetLoad\\presets\\" + name + ".preset", LZ4MessagePackSerializer.Serialize(preset));
            
            SetupPresets();
            
            Console.WriteLine("[PHIBL_PresetLoad] Saved preset: " + name);
        }

        public static void DeletePreset(string name = "preset name")
        {
            string path = Directory.GetCurrentDirectory() + "\\Plugins\\PHIBL_PresetLoad\\presets\\" + name + ".preset";
            
            if(File.Exists(path))
                File.Delete(path);
            
            SetupPresets();
        }
        
        public void OnUpdate()
        {
            if (uiObj == null || !Input.GetKey(KeyCode.RightControl) || !Input.GetKeyDown(uiKey)) 
                return;
            
            if(!drawUI)
                SetupPresets();
            
            drawUI = !drawUI;
        }

        public void OnLevelWasLoaded(int level)
        {
            drawUI = false;
            
            string name = SceneManager.GetActiveScene().name;
            if (!scenes.ContainsValue(name))
                return;

            phIBL = phIBL == null ? UnityEngine.Object.FindObjectOfType<PHIBL.PHIBL>() : phIBL;
            if (phIBL == null) 
                return;

            SetupPresets();
            
            int pID = 0;
            foreach (var preset in presets)
            {
                for (int i = 0; i < scenes.Count; i++)
                {
                    if (!preset.scenes[i]) 
                        continue;

                    if (!scenes.TryGetValue(i, out var pName)) 
                        continue;

                    if (pName != name) 
                        continue;

                    phIBL.StartCoroutine(LoadPresetDelayed(pID));
                    break;
                }

                pID++;
            }
        }
        
        public void OnFixedUpdate() {}
        public void OnLevelWasInitialized(int level) {}
        public void OnApplicationQuit() {}
    }
}