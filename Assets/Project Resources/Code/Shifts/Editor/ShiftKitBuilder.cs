using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampfireFriends.Network;
using Fusion;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CampfireFriends.Shifts.Editor
{
    /// <summary>
    /// Generates the day/night kit and adds it to a scene: the shift settings and look assets
    /// (created once, then left to tune), the wrist watch prefab (generated once from the Interaction
    /// SDK UI set, then re-linked), an offline and a networked shift clock, the lighting that shows
    /// them, and a watch on the player's left hand. Run as part of Rebuild Build Kit.
    /// </summary>
    public static class ShiftKitBuilder
    {
        private const string Root = "Assets/Project Resources/Shift";
        private const string SettingsPath = Root + "/ShiftSettings.asset";
        private const string LookPath = Root + "/DayNightLook.asset";
        private const string WatchPrefabPath = "Assets/UI/PRefabs/shift-watch.prefab";

        // Interaction SDK UI set: Backplate/EmptyUIBackplateWithCanvas and Button/PrimaryButton_IconAndLabel_UnityUIButton.
        private const string UiBackplateGuid = "6b196de96e7d16b4297c1980179ae439";
        private const string UiButtonGuid = "708bb04189b8488418aa725e7f94bbe8";

        // On the left controller, just behind it towards the wrist, tilted to face the eyes when looked down at.
        private static readonly Vector3 WatchPosition = new Vector3(0f, 0.03f, -0.09f);
        private static readonly Vector3 WatchRotation = new Vector3(60f, 0f, 0f);

        [MenuItem("Campfire Friends/Shift/Add Day-Night To Open Scene")]
        public static void WireOpenScene()
        {
            BuildAssets();
            var scene = SceneManager.GetActiveScene();
            WireScene(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void BuildAssets()
        {
            EnsureFolder(Root);
            if (AssetDatabase.LoadAssetAtPath<ShiftSettings>(SettingsPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ShiftSettings>(), SettingsPath);
            if (AssetDatabase.LoadAssetAtPath<DayNightLook>(LookPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DayNightLook>(), LookPath);

            BuildWatchPrefab();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Adds (or re-links) the clocks, lighting and watch. Assets are loaded here, after the scene is open.</summary>
        public static void WireScene(Scene scene)
        {
            var settings = AssetDatabase.LoadAssetAtPath<ShiftSettings>(SettingsPath);
            var look = AssetDatabase.LoadAssetAtPath<DayNightLook>(LookPath);
            if (settings == null || look == null) throw new InvalidOperationException("Shift assets are missing. Run the build kit first.");

            var offline = FindInScene<LocalShiftClock>(scene).FirstOrDefault() ?? Create<LocalShiftClock>(scene, "Shift Clock (offline)");
            Assign(offline, ("_settings", settings));

            var network = FindInScene<NetworkShiftClock>(scene).FirstOrDefault()
                          ?? Create<NetworkShiftClock>(scene, "Shift Clock (network)", typeof(NetworkObject));
            Assign(network, ("_settings", settings));

            var lighting = FindInScene<DayNightLighting>(scene).FirstOrDefault() ?? Create<DayNightLighting>(scene, "Day Night Lighting");
            var lights = FindInScene<Light>(scene).Where(l => l.type == LightType.Directional).ToList();
            var moon = lights.FirstOrDefault(l => l.name == "Moonlight") ?? CreateMoon(scene);
            var sun = lights.FirstOrDefault(l => l != moon);
            if (sun == null) Debug.LogWarning($"[ShiftKit] {scene.name}: no directional light to use as the sun.");
            Assign(lighting, ("_look", look), ("_sun", sun), ("_moon", moon));

            WireWatch(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[ShiftKit] {scene.name}: shift clock, day/night lighting and watch wired.");
        }

        private static Light CreateMoon(Scene scene)
        {
            var go = new GameObject("Moonlight", typeof(Light));
            SceneManager.MoveGameObjectToScene(go, scene);
            var light = go.GetComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            light.intensity = 0f;
            return light;
        }

        private static void WireWatch(Scene scene)
        {
            var transforms = FindInScene<Transform>(scene);
            var wrist = transforms.FirstOrDefault(t => t.name == "LeftHandAnchor") ?? transforms.FirstOrDefault(t => t.name == "LeftControllerAnchor");
            if (wrist == null)
            {
                Debug.LogWarning($"[ShiftKit] {scene.name}: no LeftHandAnchor, so no watch.");
                return;
            }

            if (FindInScene<ShiftWatch>(scene).Any()) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WatchPrefabPath);
            var watch = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wrist);
            watch.transform.localPosition = WatchPosition;
            watch.transform.localRotation = Quaternion.Euler(WatchRotation);
        }

        // ---------------------------------------------------------------- watch prefab

        private static void BuildWatchPrefab()
        {
            if (!File.Exists(WatchPrefabPath)) CreateWatchPrefab();

            var root = PrefabUtility.LoadPrefabContents(WatchPrefabPath);
            try
            {
                var watch = root.GetComponent<ShiftWatch>();
                if (watch == null) watch = root.AddComponent<ShiftWatch>();
                var texts = root.GetComponentsInChildren<TMP_Text>(true);
                TMP_Text Named(string name) => texts.FirstOrDefault(t => t.name == name);

                var button = root.GetComponentInChildren<UnityEngine.UI.Button>(true);
                Assign(watch,
                    ("_night", Named("Night")),
                    ("_time", Named("Time")),
                    ("_phase", Named("Phase")),
                    ("_skip", button),
                    ("_skipLabel", button != null ? button.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Label") : null));
                PrefabUtility.SaveAsPrefabAsset(root, WatchPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>A small backplate: night, a large time, the phase, and a skip button.</summary>
        private static void CreateWatchPrefab()
        {
            var backplate = LoadByGuid<GameObject>(UiBackplateGuid);
            var buttonPrefab = LoadByGuid<GameObject>(UiButtonGuid);

            const float width = 220f, padding = 16f, buttonHeight = 44f;
            var lines = new (string name, float size, float height)[] { ("Night", 18f, 26f), ("Time", 44f, 54f), ("Phase", 20f, 28f) };
            var size = new Vector2(width + padding * 2f, lines.Sum(l => l.height) + buttonHeight + padding * 2f + 24f);

            var root = new GameObject(Path.GetFileNameWithoutExtension(WatchPrefabPath));
            try
            {
                var panel = (GameObject)PrefabUtility.InstantiatePrefab(backplate, root.transform);
                panel.name = "Panel";

                var canvas = (RectTransform)FindDeep(panel.transform, "CanvasRoot");
                var plate = (RectTransform)FindDeep(panel.transform, "UIBackplate");
                canvas.sizeDelta = size;
                canvas.localScale = Vector3.one * 0.00045f;
                plate.sizeDelta = size;

                var button = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, plate);
                button.name = "Button Skip";
                ((RectTransform)button.transform).sizeDelta = new Vector2(width, buttonHeight);
                var label = button.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Label");
                if (label != null) label.text = "Skip ahead";
                var icon = FindDeep(button.transform, "Icon");
                if (icon != null) icon.gameObject.SetActive(false);

                // The text lines sit above the button, in the button's font.
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    var (name, fontSize, height) = lines[i];
                    var line = new GameObject(name, typeof(RectTransform));
                    line.transform.SetParent(plate, false);
                    line.transform.SetAsFirstSibling();
                    ((RectTransform)line.transform).sizeDelta = new Vector2(width, height);

                    var text = line.AddComponent<TextMeshProUGUI>();
                    if (label != null) text.font = label.font;
                    text.fontSize = fontSize;
                    text.color = Color.white;
                    text.alignment = TextAlignmentOptions.Center;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.text = name == "Time" ? "3:00 PM" : name == "Night" ? "Night 1" : "Prep";

                    var layout = line.AddComponent<UnityEngine.UI.LayoutElement>();
                    layout.preferredHeight = height;
                    layout.preferredWidth = width;
                }

                PrefabUtility.SaveAsPrefabAsset(root, WatchPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------------------------------------------------------------- helpers

        private static T Create<T>(Scene scene, string name, params Type[] extras) where T : Component
        {
            var go = new GameObject(name, extras.Concat(new[] { typeof(T) }).ToArray());
            SceneManager.MoveGameObjectToScene(go, scene);
            return go.GetComponent<T>();
        }

        private static List<T> FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToList();
        }

        private static void Assign(Object target, params (string field, Object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var property = so.FindProperty(field) ?? throw new InvalidOperationException($"{target.GetType().Name} has no field '{field}'.");
                property.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadByGuid<T>(string guid) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            return asset != null ? asset : throw new InvalidOperationException($"Interaction SDK asset {guid} not found. Is com.meta.xr.sdk.interaction installed?");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
