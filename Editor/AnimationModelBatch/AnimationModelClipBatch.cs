#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

using Herghys.AnimationBatchClipHelper.Saves;

using UnityEditor;

using UnityEngine;

namespace Herghys.AnimationBatchClipHelper.FBXImporter
{
    public class AnimationModelBatchSettings : EditorWindow
    {
        [SerializeField] private List<AnimationModelClipBatch> models = new();
        private Vector2 scroll;

        private string PresetFileName;
        private string PresetFilePath => Path.Combine(GlobalSavesData.AnimationModelClipBatchLocation, PresetFileName);

        [MenuItem("Tools/Herghys/Animation Model Batch Settings")]
        public static void ShowWindow()
        {
            GetWindow<AnimationModelBatchSettings>("Model Clip Settings");
        }

        private void OnEnable()
        {
            PresetFileName = GlobalSavesData.DefaultAnimationModelBatchFileName;
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Batch Animation Model Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            DrawPresetSection();

            EditorGUILayout.Space();

            EditorGUILayout.Space();
            DrawDragDropArea();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                EditorGUILayout.BeginVertical("box");

                string modelPath = AssetDatabase.GUIDToAssetPath(model.ModelGUID);
                GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                bool modelMissing = modelAsset == null;

                // Show model object field (read-only)
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Model", modelAsset, typeof(GameObject), false);
                EditorGUI.EndDisabledGroup();

                if (modelMissing)
                {
                    EditorGUILayout.HelpBox($"Model missing: {model.ModelName} (GUID {model.ModelGUID})", MessageType.Warning);
                }

                EditorGUILayout.Space();

                // Show clips (now by name only)
                for (int j = 0; j < model.Clips.Count; j++)
                {
                    AnimationClipEntry clip = model.Clips[j];

                    EditorGUILayout.BeginHorizontal();

                    // Toggle for Processed status
                    clip.Processed = EditorGUILayout.Toggle(clip.Processed, GUILayout.Width(20));

                    // Show clip name (read-only)
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(clip.ClipName, GUILayout.MinWidth(150));
                    EditorGUI.EndDisabledGroup();

                    // Loop toggle (per clip)
                    clip.Status = EditorGUILayout.ToggleLeft("Loop", clip.Status, GUILayout.Width(60));

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Remove Model"))
                {
                    models.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Apply Settings"))
            {
                ApplySettings();
            }
        }

        void DrawDragDropArea()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag FBX Models Here", EditorStyles.helpBox);

            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (string path in DragAndDrop.paths)
                        {
                            if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                            {
                                AddModelFromFBX(path);
                            }
                        }
                    }
                    evt.Use();
                }
            }
        }

        void AddModelFromFBX(string fbxPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(fbxPath);
            string name = Path.GetFileNameWithoutExtension(fbxPath);

            var model = new AnimationModelClipBatch(guid, name);

            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                    clips = importer.defaultClipAnimations;

                foreach (var clip in clips)
                {
                    if (!clip.name.Contains("__preview__"))
                        model.Clips.Add(new AnimationClipEntry(clip.name));
                }
            }

            models.Add(model);
        }

        void ApplySettings()
        {
            foreach (var model in models)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(model.ModelGUID);
                var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"Skipping {model.ModelName}, not a ModelImporter");
                    continue;
                }

                var clipAnimations = importer.clipAnimations;
                if (clipAnimations == null || clipAnimations.Length == 0)
                    clipAnimations = importer.defaultClipAnimations;

                bool modified = false;

                foreach (var clipEntry in model.Clips)
                {
                    if (!clipEntry.Processed) continue;

                    for (int i = 0; i < clipAnimations.Length; i++)
                    {
                        if (clipAnimations[i].name == clipEntry.ClipName)
                        {
                            clipAnimations[i].loopTime = clipEntry.Status;
                            modified = true;
                            Debug.Log($"Applied LoopTime={clipEntry.Status} to {clipEntry.ClipName} in {model.ModelName}");
                        }
                    }
                }

                if (modified)
                {
                    importer.clipAnimations = clipAnimations;
                    importer.SaveAndReimport();
                    AssetDatabase.Refresh();
                }
            }
        }

        void DrawPresetSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            PresetFileName = EditorGUILayout.TextField("Preset File", PresetFileName);

            if (GUILayout.Button("Select", GUILayout.MinWidth(60)))
            {
                string selected = EditorUtility.OpenFilePanel("Select Preset File",
                    Directory.Exists(PresetFilePath) ? PresetFilePath : Application.dataPath, "json");

                if (!string.IsNullOrEmpty(selected))
                    PresetFileName = Path.GetFileName(selected);
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

        void SavePreset()
        {
            if (string.IsNullOrEmpty(PresetFileName))
                PresetFileName = "ModelPreset.json";

            if (!Directory.Exists(GlobalSavesData.AnimationModelClipBatchLocation))
                Directory.CreateDirectory(GlobalSavesData.AnimationModelClipBatchLocation);

            File.WriteAllText(PresetFilePath, JsonUtility.ToJson(new Wrapper { Models = models }, true));
            AssetDatabase.Refresh();
            Debug.Log($"Preset saved: {PresetFilePath}");
        }

        void LoadPreset()
        {
            if (string.IsNullOrEmpty(PresetFileName))
                return;

            string filePath = PresetFilePath;
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Preset not found: {filePath}");
                return;
            }

            string json = File.ReadAllText(filePath);
            var wrapper = JsonUtility.FromJson<Wrapper>(json);

            models = new List<AnimationModelClipBatch>();

            if (wrapper.Models != null)
            {
                foreach (var savedModel in wrapper.Models)
                {
                    string modelPath = AssetDatabase.GUIDToAssetPath(savedModel.ModelGUID);
                    if (string.IsNullOrEmpty(modelPath))
                    {
                        Debug.LogWarning($"Model missing: {savedModel.ModelName} (GUID {savedModel.ModelGUID})");
                        models.Add(savedModel); // keep placeholder
                        continue;
                    }

                    var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                    if (importer == null)
                    {
                        Debug.LogWarning($"Not a valid ModelImporter: {savedModel.ModelName}");
                        continue;
                    }

                    var clipAnimations = importer.clipAnimations;
                    if (clipAnimations == null || clipAnimations.Length == 0)
                        clipAnimations = importer.defaultClipAnimations;

                    var restoredModel = new AnimationModelClipBatch(savedModel.ModelGUID, savedModel.ModelName);

                    foreach (var clip in clipAnimations)
                    {
                        // Try to find if it was in the preset
                        var savedClip = savedModel.Clips.Find(c => c.ClipName == clip.name);
                        if (savedClip != null)
                        {
                            restoredModel.Clips.Add(new AnimationClipEntry(clip.name, savedClip.Status, savedClip.Processed));
                        }
                        else
                        {
                            // New clip not in preset yet
                            restoredModel.Clips.Add(new AnimationClipEntry(clip.name, true, true));
                        }
                    }

                    models.Add(restoredModel);
                }
            }

            Debug.Log($"Preset loaded: {filePath}");
        }


        [Serializable]
        private class Wrapper
        {
            public List<AnimationModelClipBatch> Models;
        }
    }
}
#endif
