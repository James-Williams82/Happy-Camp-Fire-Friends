using System;
using System.Linq;
using CampfireFriends.Construction;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CampfireFriends.Story.Editor
{
    /// <summary>
    /// Night 1 fixes for the built camp: the dock gate's chain opens once the ferry has docked
    /// (not on a guess at who stepped where), the office map hangs on the wall instead of inside
    /// it, every job's label sits over its object, and the build menu shows the inventory.
    /// Re-runnable; Night 1's setup and the blockout build these in too.
    /// </summary>
    public static class Night1Fixes
    {
        private const string ScenePath = "Assets/Scenes/CampAshwood.unity";
        private const string BuildMenuPrefab = "Assets/UI/PRefabs/build-menu Variant.prefab";

        [MenuItem("Campfire Friends/Camp/Fix Night 1 Gate, Labels And Menus")]
        public static void Run()
        {
            AddInventory(BuildMenuPrefab);
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

            // The map board, out of the north wall.
            var office = Find("Security Office");
            var wall = office != null ? office.Find("Wall North 1") : null;
            var board = office != null ? office.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Map Board") : null;
            if (wall != null && board != null)
            {
                float face = wall.position.z - wall.lossyScale.z * 0.5f;
                var p = board.position;
                board.position = new Vector3(p.x, p.y, face - board.lossyScale.z * 0.5f - 0.01f);
            }
            Physics.SyncTransforms();

            int labels = 0;
            foreach (var story in roots.SelectMany(r => r.GetComponentsInChildren<StoryInteractable>(true)))
            {
                var so = new SerializedObject(story);

                // The chain: once the ferry is in, whoever reaches the gate has stepped off.
                if (so.FindProperty("_raises").longValue == (long)Night01Flag.GateOpened)
                {
                    so.FindProperty("_requires").longValue = (long)Night01Flag.ArrivedAtCamp;
                    so.FindProperty("_whenLocked").stringValue = "Wait for the ferry to dock";
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // Labels over their objects (some were placed before their colliders had moved).
                var label = so.FindProperty("_label").objectReferenceValue as TMP_Text;
                var shown = story.GetComponentInChildren<Renderer>();
                if (label == null || shown == null) continue;
                var b = shown.bounds;
                label.transform.position = new Vector3(b.center.x, b.max.y + 0.18f, b.center.z);
                EditorUtility.SetDirty(label.transform);
                labels++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Night1] {scene.name}: gate chain waits for the ferry; map board {(board != null ? "off the wall" : "missing")}; {labels} labels placed.");
        }

        /// <summary>The inventory line across the top of the build menu.</summary>
        public static void AddInventory(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var menu = root.GetComponentInChildren<BuildPaletteMenu>(true);
                var panel = menu != null ? new SerializedObject(menu).FindProperty("_panel").objectReferenceValue as GameObject : null;
                var canvas = (panel != null ? panel : root).GetComponentInChildren<Canvas>(true);
                if (canvas == null || menu == null) { Debug.LogWarning($"[Night1] {prefabPath}: no build menu canvas for the inventory."); return; }

                var old = canvas.transform.Find("Inventory");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var go = new GameObject("Inventory", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                go.layer = canvas.gameObject.layer;
                go.transform.SetParent(canvas.transform, false);
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 12f);
                rect.sizeDelta = new Vector2(0f, 90f);

                var text = go.GetComponent<TextMeshProUGUI>();
                var like = canvas.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t != text);
                if (like != null) { text.font = like.font; text.fontSharedMaterial = like.fontSharedMaterial; }
                text.fontSize = 30f;
                text.color = new Color(0.95f, 0.92f, 0.85f);
                text.alignment = TextAlignmentOptions.Bottom;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.raycastTarget = false;
                text.text = "<b>INVENTORY</b>";

                var readout = menu.GetComponent<InventoryReadout>();
                if (readout == null) readout = menu.gameObject.AddComponent<InventoryReadout>();
                var so = new SerializedObject(readout);
                so.FindProperty("_text").objectReferenceValue = text;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[Night1] {prefabPath}: inventory added to the build menu.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
