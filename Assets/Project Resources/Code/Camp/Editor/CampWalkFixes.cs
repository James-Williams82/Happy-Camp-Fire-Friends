using System;
using System.Linq;
using Oculus.Interaction.Surfaces;
using Oculus.Interaction.Locomotion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CampfireFriends.Camp.Editor
{
    /// <summary>
    /// Fixes for walking (rather than teleporting) round the built camp, plus the night kit:
    /// both docks reach the ferry's deck (there was a strip of lake between), the beach sits above
    /// the water line, a sandbar runs out to the lifeguard tower, the flashlight reaches further,
    /// and the generator runs on its own loop. Re-runnable. The blockout builds these in too.
    /// </summary>
    public static class CampWalkFixes
    {
        private const string ScenePath = "Assets/Scenes/CampAshwood.unity";
        private const string GeneratorLoop = "Assets/Audio/Ambience/generator.mp3";

        // The ferry's deck reaches 3.0 m either side of its mooring; the docks are run to 0.2 m under it.
        private const float DeckHalfLength = 3f;
        private const float Overlap = 0.2f;

        [MenuItem("Campfire Friends/Camp/Fix Walking Round The Camp")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Apply(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void RunBatch()
        {
            try { Run(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void Apply(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            Transform Find(string name) => roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == name);

            // The docks, out to the deck.
            var campStop = Find("Ferry Stop (camp dock)");
            var farStop = Find("Ferry Stop (far shore)");
            if (campStop != null) Reach(Find("Boat Dock (ferry landing)"), campStop.position.z - DeckHalfLength + Overlap);
            if (farStop != null) Reach(Find("Ferry Landing (far shore)"), farStop.position.z + DeckHalfLength - Overlap);

            // The beach, just above the lake where they meet.
            var beach = Find("Beach");
            if (beach != null)
            {
                var p = beach.position;
                beach.position = new Vector3(p.x, 0.04f, p.z);
                beach.localScale = new Vector3(beach.localScale.x, 0.08f, beach.localScale.z);
            }

            // A sandbar from the beach to the lifeguard tower, which stands in the shallows.
            var tower = Find("Lifeguard Tower (out of service)");
            var platform = tower != null ? tower.Find("Platform") : null;
            if (platform != null && beach != null)
            {
                var old = tower.parent.Find("Sandbar (to the tower)");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var top = platform.position;
                float south = beach.position.z + beach.localScale.z * 0.5f - 1f;
                float north = top.z + 2.2f;
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = "Sandbar (to the tower)";
                bar.transform.SetParent(tower.parent, true);
                bar.transform.position = new Vector3(top.x, 0.04f, (south + north) * 0.5f);
                bar.transform.localScale = new Vector3(4.5f, 0.08f, north - south);
                bar.GetComponent<MeshRenderer>().sharedMaterial = beach.GetComponent<MeshRenderer>().sharedMaterial;
                Walkable(bar);
                GameObjectUtility.SetStaticEditorFlags(bar, StaticEditorFlags.BatchingStatic);
            }

            // The flashlight: further and brighter.
            var flashlight = Find("Flashlight");
            var beam = flashlight != null ? flashlight.GetComponentsInChildren<Light>(true).FirstOrDefault() : null;
            if (beam != null) Flashlight(beam);

            // The generator's own running loop.
            var hum = Find("Generator Hum");
            var loop = AssetDatabase.LoadAssetAtPath<AudioClip>(GeneratorLoop);
            if (hum != null && loop != null)
            {
                var source = hum.GetComponent<AudioSource>();
                source.clip = loop;
                source.loop = true;
                source.playOnAwake = true;
                source.volume = 0.6f;
                EditorUtility.SetDirty(source);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Camp] Walking fixes on {scene.name}: docks to the deck, beach and tower sandbar, flashlight {(beam != null ? "boosted" : "missing")}, generator loop {(loop != null && hum != null ? "set" : "missing")}.");
        }

        /// <summary>The flashlight beam's settings (shared with Night 1's setup).</summary>
        public static void Flashlight(Light beam)
        {
            beam.range = 45f;
            beam.intensity = 9f;
            beam.spotAngle = 50f;
            beam.innerSpotAngle = 24f;
            EditorUtility.SetDirty(beam);
        }

        /// <summary>Runs a dock box (along z) on to <paramref name="toZ"/>, keeping its far end.</summary>
        private static void Reach(Transform dock, float toZ)
        {
            if (dock == null) return;
            float half = dock.localScale.z * 0.5f;
            float min = dock.position.z - half, max = dock.position.z + half;
            if (toZ > max) max = toZ;
            else if (toZ < min) min = toZ;
            else return;
            dock.position = new Vector3(dock.position.x, dock.position.y, (min + max) * 0.5f);
            dock.localScale = new Vector3(dock.localScale.x, dock.localScale.y, max - min);
        }

        private static void Walkable(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            var surface = target.AddComponent<ColliderSurface>();
            var surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("_collider").objectReferenceValue = collider;
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();
            var interactable = target.AddComponent<TeleportInteractable>();
            var so = new SerializedObject(interactable);
            so.FindProperty("_surface").objectReferenceValue = surface;
            so.FindProperty("_allowTeleport").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
