#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Herghys.AnimationBatchClipHelper.Saves;

using UnityEditor;

using UnityEngine;

namespace Herghys.AnimationBatchClipHelper.AnimationClipBatch
{
    public class AnimationClipBatchSettings : EditorWindow
    {
        [SerializeField] private List<AnimationClip> clips = new();
        [SerializeField] private bool loopTime = true;
        [SerializeField] private bool autoClearList = true;

        private string PresetFileName;
        private string PresetFilePath => Path.Combine(GlobalSavesData.AnimationClipBatchLocation, PresetFileName);

        SerializedObject serializedObject;
        SerializedProperty clipsProperty;

        [MenuItem("Tools/Herghys/Animation Clip Batch Settings")]
        public static void ShowWindow()
        {
            GetWindow<AnimationClipBatchSettings>("Animation Clip Settings");
        }

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            clipsProperty = serializedObject.FindProperty("clips");

            PresetFileName = GlobalSavesData.DefaultAnimationClipBatchFileName;
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Batch Animation Clip Settings", EditorStyles.boldLabel);

            DrawPresetSection();

            EditorGUILayout.PropertyField(clipsProperty, true);
            serializedObject.ApplyModifiedProperties();

            loopTime = EditorGUILayout.Toggle("Loop Time", loopTime);
            autoClearList = EditorGUILayout.Toggle("Auto Clear List", autoClearList);

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Settings"))
            {
                ApplySettings();
            }

            if (GUILayout.Button("Clear List"))
            {
                clips.Clear();
            }
        }

        void DrawPresetSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // Editable text field instead of LabelField
            PresetFileName = EditorGUILayout.TextField("Preset File", PresetFileName);

            if (GUILayout.Button("Select", GUILayout.MinWidth(60)))
            {
                string selected = EditorUtility.OpenFilePanel("Select Preset File",
                    Path.GetDirectoryName(PresetFilePath), "json");

                if (!string.IsNullOrEmpty(selected))
                {
                    PresetFileName = Path.GetFileName(selected); // only store filename
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Preset"))
            {
                SavePreset();
            }
            if (GUILayout.Button("Load Preset"))
            {
                LoadPreset();
            }
            EditorGUILayout.EndHorizontal();
        }

        void ApplySettings()
        {
            if (clips == null || clips.Count == 0)
            {
                Debug.LogWarning("No animation clips assigned.");
                return;
            }

            foreach (var clip in clips)
            {
                if (clip == null) continue;

                SerializedObject so = new SerializedObject(clip);
                SerializedProperty settings = so.FindProperty("m_AnimationClipSettings");

                if (settings != null)
                {
                    settings.FindPropertyRelative("m_LoopTime").boolValue = loopTime;
                    so.ApplyModifiedProperties();
                    Debug.Log($"LoopTime = {loopTime} on {clip.name}");
                }
                else
                {
                    Debug.LogWarning($"Could not access settings for {clip.name}");
                }
            }

            if (autoClearList)
            {
                clips.Clear();
            }
        }

        void SavePreset()
        {
            if (!Directory.Exists(GlobalSavesData.AnimationClipBatchLocation))
                Directory.CreateDirectory(GlobalSavesData.AnimationClipBatchLocation);

            var saves = clips
                .Where(c => c != null)
                .Select(c => new AnimationClipEntry
                {
                    GUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c)),
                    ClipName = c.name,
                })
                .ToList();

            File.WriteAllText(PresetFilePath, JsonUtility.ToJson(new Wrapper { Clips = saves }, true));
            Debug.Log($"Preset saved to {PresetFilePath}");
            AssetDatabase.Refresh();
        }

        void LoadPreset()
        {
            if (!File.Exists(PresetFilePath))
            {
                Debug.LogWarning($"No preset found at {PresetFilePath}");
                return;
            }

            var wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(PresetFilePath));
            clips.Clear();

            foreach (var save in wrapper.Clips)
            {
                string path = AssetDatabase.GUIDToAssetPath(save.GUID);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            serializedObject.Update();
            Repaint();
            EditorUtility.SetDirty(this);
            Debug.Log("Preset loaded.");
        }

        [System.Serializable]
        public class Wrapper
        {
            public List<AnimationClipEntry> Clips;
        }
    }
}
#endif
