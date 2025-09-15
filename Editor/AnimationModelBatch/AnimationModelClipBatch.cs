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

        private bool loopTime = true;
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

            loopTime = EditorGUILayout.Toggle("Loop Time", loopTime);

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

                // Show clips
                for (int j = 0; j < model.Clips.Count; j++)
                {
                    var clip = model.Clips[j];
                    string clipPath = AssetDatabase.GUIDToAssetPath(clip.GUID);
                    AnimationClip clipAsset = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    bool clipMissing = clipAsset == null;

                    EditorGUILayout.BeginHorizontal();

                    // Toggle for status
                    clip.Status = EditorGUILayout.Toggle(clip.Status, GUILayout.Width(20));

                    // Object field for clip
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(clipAsset, typeof(AnimationClip), false);
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();

                    if (clipMissing)
                    {
                        EditorGUILayout.HelpBox($"Clip missing: {clip.ClipName} (GUID {clip.GUID})", MessageType.Warning);
                    }
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
            string name = System.IO.Path.GetFileNameWithoutExtension(fbxPath);

            var model = new AnimationModelClipBatch(guid, name);

            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    string clipGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip));
                    model.Clips.Add(new AnimationClipEntry(clipGUID, clip.name));
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
                bool modified = false;

                foreach (var clip in model.Clips)
                {
                    if (!clip.Status) continue;

                    for (int i = 0; i < clipAnimations.Length; i++)
                    {
                        if (clipAnimations[i].name == clip.ClipName)
                        {
                            clipAnimations[i].loopTime = loopTime;
                            modified = true;
                            Debug.Log($"Applied LoopTime={loopTime} to {clip.ClipName} in {model.ModelName}");
                        }
                    }
                }

                if (modified)
                {
                    importer.clipAnimations = clipAnimations;
                    importer.SaveAndReimport();
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

            string filePath = Path.Combine(PresetFilePath);
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
                foreach (var model in wrapper.Models)
                {
                    string modelPath = AssetDatabase.GUIDToAssetPath(model.ModelGUID);
                    bool modelMissing = string.IsNullOrEmpty(modelPath);

                    var validModel = new AnimationModelClipBatch(model.ModelGUID, model.ModelName);

                    foreach (var clip in model.Clips)
                    {
                        string clipPath = AssetDatabase.GUIDToAssetPath(clip.GUID);
                        bool clipMissing = string.IsNullOrEmpty(clipPath);

                        if (clipMissing)
                        {
                            Debug.LogWarning($"Clip missing: {clip.ClipName} (GUID {clip.GUID}) in model {model.ModelName}");
                        }

                        validModel.Clips.Add(new AnimationClipEntry(clip.GUID, clip.ClipName, clip.Status)
                        {
                            // You could add a flag if you want (e.g., `IsMissing`)
                        });
                    }

                    if (modelMissing)
                    {
                        Debug.LogWarning($"Model missing: {model.ModelName} (GUID {model.ModelGUID})");
                    }

                    models.Add(validModel);
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
