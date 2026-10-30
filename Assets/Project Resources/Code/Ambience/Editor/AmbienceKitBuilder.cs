using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CampfireFriends.Ambience.Editor
{
    /// <summary>
    /// Sets up the camp soundscape: the ambience profile (created once, then left to tune), its
    /// recordings gathered by name from <see cref="ClipsFolder"/> on every run, and an ambience
    /// controller on the player's head in the scene. Run as part of Rebuild Build Kit.
    ///
    /// Recordings are named amb_&lt;layer&gt;_&lt;nn&gt;: wind, rustle, the bed ids (dawn, day, night) and the
    /// scatter ids (crickets, daycalls, nightcalls, creaks). A layer without recordings keeps what it
    /// had; wind, leaves and crickets fall back to generated sound.
    /// </summary>
    public static class AmbienceKitBuilder
    {
        private const string ProfilePath = "Assets/Project Resources/Ambience/AmbienceProfile.asset";
        public const string ClipsFolder = "Assets/Audio/Ambience";
        private static readonly Regex ClipName = new Regex(@"^amb_(?<layer>[a-z]+)_(?<n>\d+)$");

        [MenuItem("Campfire Friends/Ambience/Add Ambience To Open Scene")]
        public static void WireOpenScene()
        {
            BuildAssets();
            var scene = SceneManager.GetActiveScene();
            WireScene(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void BuildAssets()
        {
            EnsureFolder(Path.GetDirectoryName(ProfilePath).Replace('\\', '/'));
            EnsureFolder(ClipsFolder);

            var profile = AssetDatabase.LoadAssetAtPath<AmbienceProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<AmbienceProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            if (profile.Beds.All(b => b.Id != "music")) profile.Beds = profile.Beds.Append(AmbienceProfile.MusicBed()).ToArray();

            var clips = GatherClips();
            if (!clips.ContainsKey("music"))
            {
                // Music dropped in under its own name, e.g. "Abandoned Summer Camp".
                var music = AssetDatabase.FindAssets("t:AudioClip summer camp", new[] { ClipsFolder })
                    .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).Where(c => c != null).ToArray();
                if (music.Length > 0) clips["music"] = music;
                foreach (var clip in music) Stream(clip);
            }
            if (clips.TryGetValue("wind", out var wind)) profile.Wind.Clips = wind;
            if (clips.TryGetValue("rustle", out var rustle)) profile.Rustle.Clips = rustle;
            foreach (var bed in profile.Beds)
                if (clips.TryGetValue(bed.Id, out var found)) bed.Clips = found;
            foreach (var scatter in profile.Scatters)
                if (clips.TryGetValue(scatter.Id, out var found)) scatter.Clips = found;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            string summary = clips.Count == 0
                ? $"no recordings in {ClipsFolder} yet; wind, leaves and crickets are generated"
                : string.Join(", ", clips.Select(c => $"{c.Key} ×{c.Value.Length}"));
            Debug.Log($"[Ambience] Profile ready: {summary}.");
        }

        /// <summary>Adds (or re-links) the ambience controller, listening from the player's eyes.</summary>
        public static void RunBatch()
        {
            try
            {
                BuildAssets();
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/CampAshwood.unity", OpenSceneMode.Single);
                WireScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void WireScene(Scene scene)
        {
            var profile = AssetDatabase.LoadAssetAtPath<AmbienceProfile>(ProfilePath)
                          ?? throw new InvalidOperationException($"{ProfilePath} is missing. Run the build kit first.");

            var roots = scene.GetRootGameObjects();
            var controller = roots.SelectMany(r => r.GetComponentsInChildren<AmbienceController>(true)).FirstOrDefault();
            if (controller == null)
            {
                var go = new GameObject("Ambience", typeof(AmbienceController));
                SceneManager.MoveGameObjectToScene(go, scene);
                controller = go.GetComponent<AmbienceController>();
            }

            AbsorbOldAudio(scene, profile);

            var eyes = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "CenterEyeAnchor");
            var so = new SerializedObject(controller);
            so.FindProperty("_profile").objectReferenceValue = profile;
            so.FindProperty("_listener").objectReferenceValue = eyes;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Ambience] {scene.name}: ambience controller wired{(eyes == null ? " (no CenterEyeAnchor; it will use the main camera)" : "")}.");
        }

        /// <summary>
        /// The scene's older always-on audio: its ambient clip becomes the dawn and day bed (so it
        /// fades out at dusk), and both old players are switched off. Their objects and clips stay.
        /// </summary>
        private static void AbsorbOldAudio(Scene scene, AmbienceProfile profile)
        {
            var sources = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<AudioSource>(true)).ToList();
            var ambient = sources.FirstOrDefault(s => s.name == "Local Audio Controller (Ambient)");
            var music = sources.FirstOrDefault(s => s.name == "Local Audio Controller (BGM)");

            if (ambient != null && ambient.clip != null)
            {
                foreach (var bed in profile.Beds.Where(b => (b.Id == "dawn" || b.Id == "day") && b.Clips.Length == 0))
                    bed.Clips = new[] { ambient.clip };
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }

            foreach (var old in new[] { ambient, music })
            {
                if (old == null || !old.gameObject.activeSelf) continue;
                old.gameObject.SetActive(false);
                Debug.Log($"[Ambience] {scene.name}: switched off '{old.name}' ({(old.clip != null ? old.clip.name : "no clip")}); the ambience controller replaces it.");
            }
        }

        /// <summary>A long track streams from disk instead of sitting decoded in memory.</summary>
        private static void Stream(AudioClip clip)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            var settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.Streaming) return;
            settings.loadType = AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }

        private static Dictionary<string, AudioClip[]> GatherClips()
        {
            return AssetDatabase.FindAssets("t:AudioClip", new[] { ClipsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => (path, match: ClipName.Match(Path.GetFileNameWithoutExtension(path).ToLowerInvariant())))
                .Where(x => x.match.Success)
                .OrderBy(x => x.path, StringComparer.Ordinal)
                .GroupBy(x => x.match.Groups["layer"].Value)
                .ToDictionary(g => g.Key, g => g.Select(x => AssetDatabase.LoadAssetAtPath<AudioClip>(x.path)).Where(c => c != null).ToArray());
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
