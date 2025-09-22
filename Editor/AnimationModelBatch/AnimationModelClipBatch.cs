#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Herghys.AnimationBatchClipHelper.Saves;

using UnityEditor;

using UnityEngine;

namespace Herghys.AnimationBatchClipHelper.FBXImporter
{
    public class AnimationModelBatchSettings : EditorWindow
    {
        [SerializeField] private HashSet<AnimationModelClipBatch> models = new();
        private Vector2 scroll;

        private Dictionary<string, bool> modelFoldouts = new();

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

        private void OnGUI()
        {
            DrawTitle();
            DrawPresetSection();
            DrawDragDropArea();
            DrawLoopProcessToggle();
            DrawModelAnimationList();
            DrawFooter();
        }

        private void DrawTitle()
        {
            EditorGUILayout.LabelField("Batch Animation Model Settings", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("By: Herghys", EditorStyles.miniLabel);
            EditorGUILayout.Space();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Apply Settings"))
            {
                ApplyModelSettings();
            }
        }

        private void DrawPresetSection()
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

        private void DrawDragDropArea()
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

        /// <summary>
        /// Global Loop and Process Toggle
        /// </summary>
        private void DrawLoopProcessToggle()
        {
            if (models is null || models.Count == 0)
                return;

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            ShowProcessToggle();
            ShowLoopToggle();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

        }

        /// <summary>
        /// Global Process Toggle
        /// </summary>
        private void ShowProcessToggle()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Proccess Toggle", EditorStyles.boldLabel);


            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set All Processed", GUILayout.ExpandWidth(true)))
            {
                foreach (var model in models)
                {
                    foreach (var clip in model.Clips)
                    {
                        clip.Processed = true;
                    }
                }
            }

            if (GUILayout.Button("Unprocessed Everything", GUILayout.ExpandWidth(true)))
            {
                foreach (var model in models)
                {
                    foreach (var clip in model.Clips)
                    {
                        clip.Processed = false;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Global Loop Toggle
        /// </summary>
        private void ShowLoopToggle()
        {
            EditorGUILayout.BeginVertical();

            GUIStyle rightBold = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };

            EditorGUILayout.LabelField("Loop Toggle", rightBold);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Loop Everything", GUILayout.ExpandWidth(true)))
            {
                foreach (var model in models)
                {
                    foreach (var clip in model.Clips)
                    {
                        clip.Status = true;
                    }
                }
            }

            if (GUILayout.Button("Unloop Everything", GUILayout.ExpandWidth(true)))
            {
                foreach (var model in models)
                {
                    foreach (var clip in model.Clips)
                    {
                        clip.Status = false;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw Animations list
        /// </summary>
        private void DrawModelAnimationList()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (int i = 0; i < models.Count; i++)
            {
                var model = models.ElementAtOrDefault(i);
                string modelPath = AssetDatabase.GUIDToAssetPath(model.ModelGUID);

                if (!modelFoldouts.ContainsKey(model.ModelGUID))
                    modelFoldouts[model.ModelGUID] = true;

                // Always draw the header
                modelFoldouts[model.ModelGUID] = EditorGUILayout.Foldout(
                    modelFoldouts[model.ModelGUID],
                    modelPath,
                    true);

                if (modelFoldouts[model.ModelGUID])
                {
                    DrawAnimationsFromModel(ref i, model, modelPath);
                }

                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAnimationsFromModel(ref int i, AnimationModelClipBatch model, string modelPath)
        {
            EditorGUILayout.BeginVertical("box");

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            bool modelMissing = modelAsset == null;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Model", modelAsset, typeof(GameObject), false);
            EditorGUI.EndDisabledGroup();

            if (modelMissing)
            {
                EditorGUILayout.HelpBox(
                    $"Model missing: {model.ModelName} (GUID {model.ModelGUID})",
                    MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply to Model", GUILayout.Height(40)))
            {
                ApplyAnimationSettings(model, modelPath);
            }

            if (GUILayout.Button("Remove Model", GUILayout.Height(40)))
            {
                models.Remove(model);
                modelFoldouts.Remove(model.ModelGUID);
                i--;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Process All\nAnimations From This Model", GUILayout.Height(50)))
                model.Clips.ForEach(c => c.Processed = true);
            if (GUILayout.Button("Unprocess All\nAnimations From This Model", GUILayout.Height(50)))
                model.Clips.ForEach(c => c.Processed = false);
            if (GUILayout.Button("Loop All\nAnimations From This Model", GUILayout.Height(50)))
                model.Clips.ForEach(c => c.Status = true);
            if (GUILayout.Button("Unloop All\nAnimations From This Model", GUILayout.Height(50)))
                model.Clips.ForEach(c => c.Status = false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            foreach (var clip in model.Clips)
            {
                EditorGUILayout.BeginHorizontal();
                clip.Processed = EditorGUILayout.Toggle(clip.Processed, GUILayout.Width(20));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(clip.ClipName, GUILayout.MinWidth(150));
                EditorGUI.EndDisabledGroup();
                clip.Status = EditorGUILayout.ToggleLeft("Loop", clip.Status, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Add Model from fbx
        /// </summary>
        /// <param name="fbxPath"></param>
        private void AddModelFromFBX(string fbxPath)
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
                        model.Clips.Add(new AnimationClipEntry(clip.name, processed: true));
                }
            }

            models.Add(model);

            if (!modelFoldouts.ContainsKey(guid))
                modelFoldouts.Add(guid, true);
        }

        /// <summary>
        /// Apply Everything
        /// </summary>
        private void ApplyModelSettings()
        {
            foreach (var model in models)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(model.ModelGUID);
                ApplyAnimationSettings(model, modelPath);
            }
        }

        /// <summary>
        /// Apply Target Animations
        /// </summary>
        /// <param name="model"></param>
        /// <param name="modelPath"></param>
        private void ApplyAnimationSettings(AnimationModelClipBatch model, string modelPath)
        {
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Skipping {model.ModelName}, not a ModelImporter");
                return;
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

        /// <summary>
        /// Save current settings to preset file (JSON)
        /// </summary>
        private void SavePreset()
        {
            if (string.IsNullOrEmpty(PresetFileName))
                PresetFileName = "ModelPreset.json";

            if (!Directory.Exists(GlobalSavesData.AnimationModelClipBatchLocation))
                Directory.CreateDirectory(GlobalSavesData.AnimationModelClipBatchLocation);

            File.WriteAllText(PresetFilePath, JsonUtility.ToJson(new Wrapper { Models = models.ToList() }, true));
            AssetDatabase.Refresh();
            Debug.Log($"Preset saved: {PresetFilePath}");
        }

        private void LoadPreset()
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

            models = new();

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
