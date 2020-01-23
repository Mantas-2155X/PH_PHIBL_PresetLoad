using UnityEngine;
using System.Text.RegularExpressions;

namespace PH_PHIBL_PresetLoad
{
    public class PHIBL_PresetLoad_UI : MonoBehaviour
    {
        private const int uiWidth = 600;
        private const int uiHeight = 300;

        private static Rect window = new Rect(5, 5, uiWidth, uiHeight);
        private static Vector2 presetsScrollPos;

        public static int selectedPreset = -1;
        public static string presetName = "preset name";
        public static bool[] scenes = new bool[6];
        
        private void OnGUI()
        {
            if(IPA_PHIBL_PresetLoad.drawUI)
                window = GUILayout.Window(789456124, window, DrawWindow, "PHIBL Preset Load UI", GUILayout.Width(uiWidth), GUILayout.Height(uiHeight));
        }

        private static void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            
                GUILayout.BeginArea(new Rect(5, 20, uiWidth / 2f - 7, uiHeight - 25), GUI.skin.box);
                
                    GUILayout.BeginVertical();
                
                        presetsScrollPos = GUILayout.BeginScrollView(presetsScrollPos);

                        for(int i = 0; i < IPA_PHIBL_PresetLoad.presets.Count; i++)
                        {
                            GUILayout.BeginHorizontal();
                            
                                string name = IPA_PHIBL_PresetLoad.presets[i].name;
                                if (selectedPreset == i)
                                    name += "<";
                                
                                if (IPA_PHIBL_PresetLoad.conflicts.Contains(IPA_PHIBL_PresetLoad.presets[i]))
                                    name += " (conflict!)";
                                
                                GUILayout.Label(name);

                                GUILayout.FlexibleSpace();

                                if (GUILayout.Button("Select"))
                                {
                                    PresetInfo preset = IPA_PHIBL_PresetLoad.presets[i];
                                    presetName = preset.name;
                                    scenes = preset.scenes;
                                    
                                    selectedPreset = i;
                                }

                            GUILayout.EndHorizontal();
                        }
                        
                        GUILayout.EndScrollView();

                        if (GUILayout.Button("Load legacy preset"))
                            LegacyLoader.LoadPreset();
                        
                    GUILayout.EndVertical();

                GUILayout.EndArea();
                
                GUILayout.BeginArea(new Rect(2 + uiWidth / 2f, 20, uiWidth / 2f - 7, uiHeight - 25), GUI.skin.box);
                
                    GUILayout.BeginVertical();

                        presetName = GUILayout.TextField(presetName, 24);
                        presetName = Regex.Replace(presetName, @"[^a-zA-Z0-9 _-]", "");

                        for (int i = 0; i < scenes.Length; i++)
                            if(IPA_PHIBL_PresetLoad.scenes.TryGetValue(i, out var name))
                                scenes[i] = GUILayout.Toggle(scenes[i], name);
                        
                        GUILayout.FlexibleSpace();

                        GUILayout.BeginHorizontal();

                            if (GUILayout.Button("Load preset"))
                                IPA_PHIBL_PresetLoad.LoadPreset(selectedPreset);

                            if (GUILayout.Button("Delete preset"))
                                IPA_PHIBL_PresetLoad.DeletePreset(presetName);
                            
                            if (GUILayout.Button("Save preset"))
                                IPA_PHIBL_PresetLoad.SavePreset(scenes, presetName);

                        GUILayout.EndHorizontal();
                    
                    GUILayout.EndVertical();
                
                GUILayout.EndArea();
                
            GUILayout.EndHorizontal();
            
            GUI.DragWindow();
        }
    }
}