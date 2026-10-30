using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CampfireFriends.Arrival.Editor
{
    /// <summary>
    /// Puts Bixby (a stand-in block for now) on the ferry's deck with his recorded welcome from
    /// <see cref="Folder"/>: vo_ferry_aNN and _bNN while crossing, _cNN at the camp dock. The
    /// crossing is lengthened to fit what he says, and the approach horn sounds in the pause
    /// before the b lines ("One more thing"). Re-runnable: re-recorded lines are picked up again.
    /// Lines in <see cref="Cut"/> stay on disk but aren't played, which keeps the ride short.
    /// </summary>
    public static class FerryHostSetup
    {
        public const string Folder = "Assets/Audio/VO/Ferry";
        private const string ScenePath = "Assets/Scenes/CampAshwood.unity";
        private const string MaterialPath = "Assets/Project Resources/Story/Bixby.mat";
        private const string HostName = "Bixby (aboard)";

        private const float StartDelay = 4f;   // the bell and the cast-off horn go first
        private const float DefaultPause = 0.5f;
        private const float ArrivalMargin = 2f;

        // Playtest cut (about 1:20 instead of 2:40): the jokes, the toilet story, the directions
        // the map now gives, and the "one more thing" about the woods. Keeps the intro, the
        // premise, the history hook, the job, the walkies and the generator.
        private static readonly HashSet<string> Cut = new HashSet<string>
        {
            "a02", "a03", "a04", "a06", "a07", "a08", "a11", "b01", "b02", "b03", "c03",
        };

        // The script's stage directions: a brief pause, a beat, the horn as the dock nears.
        private static readonly Dictionary<string, float> Pauses = new Dictionary<string, float>
        {
            ["a03"] = 1.5f, ["a04"] = 0.8f, ["a08"] = 0.9f, ["a13"] = 3f, ["b01"] = 0.8f, ["c02"] = 0.7f,
        };

        [MenuItem("Campfire Friends/Camp/Put Bixby On The Ferry")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var ferry = Object.FindFirstObjectByType<Ferry>();
            if (ferry == null) { Debug.LogWarning("[Ferry] No ferry in the camp scene."); return; }

            var lines = AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => (key: Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Replace("vo_ferry_", ""), path: p))
                .OrderBy(x => x.key, StringComparer.Ordinal)
                .Select(x => (x.key, line: new SpokenLine
                {
                    Clip = AssetDatabase.LoadAssetAtPath<AudioClip>(x.path),
                    PauseAfter = Pauses.TryGetValue(x.key, out var p) ? p : DefaultPause,
                }))
                .Where(x => x.line.Clip != null && !Cut.Contains(x.key)).ToList();
            var crossing = lines.Where(x => x.key[0] == 'a' || x.key[0] == 'b').ToList();
            var docked = lines.Where(x => x.key[0] == 'c').Select(x => x.line).ToArray();
            if (crossing.Count == 0) { Debug.LogWarning($"[Ferry] No vo_ferry_ lines in {Folder}."); return; }

            var host = Host(ferry.transform);
            var so = new SerializedObject(host);
            so.FindProperty("_ferry").objectReferenceValue = ferry;
            so.FindProperty("_voice").objectReferenceValue = host.GetComponent<AudioSource>();
            so.FindProperty("_body").objectReferenceValue = host.transform.Find("Body");
            so.FindProperty("_startDelay").floatValue = StartDelay;
            Lines(so.FindProperty("_crossing"), crossing.Select(x => x.line).ToArray());
            Lines(so.FindProperty("_docked"), docked);
            so.ApplyModifiedPropertiesWithoutUndo();

            // The crossing lasts as long as he talks; the horn sounds just after the a lines.
            float length = StartDelay + crossing.Sum(x => x.line.Seconds) + ArrivalMargin;
            var hornAt = StartDelay + crossing.TakeWhile(x => x.key[0] == 'a').Sum(x => x.line.Seconds) - 2f;
            var ferrySo = new SerializedObject(ferry);
            ferrySo.FindProperty("_crossingSeconds").floatValue = length;
            ferrySo.ApplyModifiedPropertiesWithoutUndo();

            var sounds = ferry.GetComponentInChildren<FerrySounds>(true);
            if (sounds != null)
            {
                var soundsSo = new SerializedObject(sounds);
                soundsSo.FindProperty("_approachAt").floatValue = Mathf.Clamp(hornAt / length, 0.5f, 0.98f);
                soundsSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Ferry] Bixby aboard: {crossing.Count} lines crossing, {docked.Length} at the dock; the crossing now takes {length:0} s, horn at {hornAt:0} s.");
        }

        public static void RunBatch()
        {
            try { Run(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Bixby's block, standing at the front of the deck, port side, opposite the bell.</summary>
        private static FerryHost Host(Transform ferry)
        {
            var existing = ferry.Find(HostName);
            var root = existing != null ? existing.gameObject : new GameObject(HostName, typeof(AudioSource), typeof(FerryHost));
            root.transform.SetParent(ferry, false);

            var deck = ferry.Find("Deck");
            if (deck != null)
            {
                float top = deck.localPosition.y + deck.localScale.y * 0.5f;
                root.transform.localPosition = new Vector3(-deck.localScale.x * 0.3f, top, deck.localPosition.z + deck.localScale.z * 0.5f - 0.6f);
            }
            root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var body = root.transform.Find("Body");
            if (body == null)
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                body.name = "Body";
                body.SetParent(root.transform, false);
            }
            body.localPosition = new Vector3(0f, 0.875f, 0f);
            body.localScale = new Vector3(0.5f, 1.75f, 0.32f);
            body.GetComponent<MeshRenderer>().sharedMaterial = Material();

            var voice = root.GetComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Logarithmic;
            voice.minDistance = 2f;
            voice.maxDistance = 30f;
            voice.dopplerLevel = 0f;
            voice.volume = 1f;

            return root.GetComponent<FerryHost>();
        }

        private static Material Material()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;

            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            var mustard = new Color(0.72f, 0.55f, 0.22f); // a salesman's sport coat
            material.SetColor("_BaseColor", mustard);
            material.color = mustard;
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void Lines(SerializedProperty array, SpokenLine[] lines)
        {
            array.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Clip").objectReferenceValue = lines[i].Clip;
                element.FindPropertyRelative("PauseAfter").floatValue = lines[i].PauseAfter;
            }
        }
    }
}
