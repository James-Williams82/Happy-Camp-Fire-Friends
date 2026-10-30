using System;
using System.IO;
using System.Linq;
using CampfireFriends.Network;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CampfireFriends.Story.Editor
{
    /// <summary>
    /// Gathers Bixby's recordings from <see cref="Folder"/> into the Night 1 voice asset and points
    /// the camp's walkie and stories at it. Files are named vo_night01_&lt;doc label&gt;, e.g.
    /// vo_night01_01.wav … vo_night01_29.wav, vo_night01_30a/_30b/_30c and vo_night01_31. Run it
    /// whenever lines are added or replaced; missing lines stay text-only.
    /// </summary>
    public static class RadioVoiceBuilder
    {
        public const string Folder = "Assets/Audio/VO/Night01";
        private const string AssetPath = "Assets/Project Resources/Story/Night01Voice.asset";
        private const string ScenePath = "Assets/Scenes/CampAshwood.unity";

        [MenuItem("Campfire Friends/Camp/Update Bixby Voice Lines")]
        public static void Build()
        {
            EnsureFolder(Folder);
            EnsureFolder(Path.GetDirectoryName(AssetPath).Replace('\\', '/'));

            var voice = AssetDatabase.LoadAssetAtPath<RadioVoice>(AssetPath);
            if (voice == null)
            {
                voice = ScriptableObject.CreateInstance<RadioVoice>();
                AssetDatabase.CreateAsset(voice, AssetPath);
            }

            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath)
                .GroupBy(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant())
                .ToDictionary(g => g.Key, g => AssetDatabase.LoadAssetAtPath<AudioClip>(g.First()));

            voice.Lines = new AudioClip[34];
            int found = 0;
            var missing = new System.Collections.Generic.List<string>();
            foreach (var id in Night01Lines.Text.Keys)
            {
                var label = Night01Lines.DocLabel(id);
                if (clips.TryGetValue($"vo_night01_{label}", out var clip)) { voice.Lines[id] = clip; found++; }
                else missing.Add(label);
            }
            EditorUtility.SetDirty(voice);
            AssetDatabase.SaveAssets();

            if (System.IO.File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                voice = AssetDatabase.LoadAssetAtPath<RadioVoice>(AssetPath);
                foreach (var target in scene.GetRootGameObjects().SelectMany(r =>
                             r.GetComponentsInChildren<RadioPanel>(true).Cast<Component>()
                                 .Concat(r.GetComponentsInChildren<LocalStory>(true))
                                 .Concat(r.GetComponentsInChildren<NetworkStory>(true))))
                {
                    var so = new SerializedObject(target);
                    so.FindProperty("_voice").objectReferenceValue = voice;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[Night1] Bixby: {found}/{Night01Lines.Text.Count} lines recorded" +
                      (missing.Count > 0 ? $"; text only for {string.Join(", ", missing)}." : "."));
        }

        public static void BuildBatch()
        {
            try { Build(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
