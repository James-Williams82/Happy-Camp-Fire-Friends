using System;
using System.Linq;
using CampfireFriends.Climbing;
using CampfireFriends.Construction;
using Oculus.Interaction.Locomotion;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CampfireFriends.Locomotion.Editor
{
    /// <summary>
    /// Swaps teleporting for walking: a <see cref="SlideMover"/> on the left stick, the
    /// controllers' teleport arcs switched off, and the left stick's snap turn with them so it
    /// can strafe. The right stick still turns. Hand-tracking teleport is left as it was. Also the
    /// quiet footsteps, the sprint meter on the left wrist, and the camp map on the left stick's click.
    /// </summary>
    public static class SlideSetup
    {
        private const string CampScene = "Assets/Scenes/CampAshwood.unity";
        private const string SandboxScene = "Assets/Scenes/BuildSandbox.unity";
        private const string StepClip = "Assets/Audio/single_muffled_foot__grass_walk.mp3";
        private const string RunClip = "Assets/Audio/running.mp3";
        private const string MapImage = "Assets/Design/camp_overview_map.png";
        private const string MaterialFolder = "Assets/Project Resources/Story";
        private const string MapMaterialPath = MaterialFolder + "/FieldMap.mat";

        [MenuItem("Campfire Friends/Locomotion/Walk Instead Of Teleport (Open Scene)")]
        public static void ApplyToOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (Apply(scene)) EditorSceneManager.SaveScene(scene);
        }

        public static void RunBatch()
        {
            try
            {
                foreach (var path in new[] { CampScene, SandboxScene })
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    if (Apply(scene)) EditorSceneManager.SaveScene(scene);
                }
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static bool Apply(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            T[] All<T>() where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

            var locomotor = All<PlayerLocomotor>().FirstOrDefault();
            if (locomotor == null)
            {
                Debug.LogWarning($"[Slide] {scene.name}: no PlayerLocomotor, so walking isn't added.");
                return false;
            }

            var locomotorSo = new SerializedObject(locomotor);
            var origin = (Transform)locomotorSo.FindProperty("_playerOrigin").objectReferenceValue;
            var head = (Transform)locomotorSo.FindProperty("_playerHead").objectReferenceValue;

            var mover = All<SlideMover>().FirstOrDefault();
            if (mover == null)
            {
                var go = new GameObject("Walking", typeof(SlideMover));
                SceneManager.MoveGameObjectToScene(go, scene);
                mover = go.GetComponent<SlideMover>();
            }

            var so = new SerializedObject(mover);
            so.FindProperty("_locomotor").objectReferenceValue = locomotor;
            so.FindProperty("_playerOrigin").objectReferenceValue = origin;
            so.FindProperty("_head").objectReferenceValue = head;
            so.FindProperty("_climbing").objectReferenceValue = All<ClimbController>().FirstOrDefault();
            so.FindProperty("_building").objectReferenceValue = All<BuildController>().FirstOrDefault();
            so.ApplyModifiedPropertiesWithoutUndo();

            AddFootsteps(mover);
            var wrist = All<Transform>().FirstOrDefault(t => t.name == "LeftHandAnchor");
            if (wrist != null) AddMeter(mover, wrist);
            AddMap(mover.gameObject, head);

            // Teleport arcs off on both controllers; the left stick's turning too, as it walks now.
            int teleports = 0, turners = 0;
            foreach (var t in All<Transform>().Where(t => t.name == "TeleportControllerInteractor"))
            {
                t.gameObject.SetActive(false);
                teleports++;
            }
            foreach (var t in All<Transform>().Where(t => t.name == "ControllerTurnerInteractor" && IsLeft(t)))
            {
                t.gameObject.SetActive(false);
                turners++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Slide] {scene.name}: walking on the left stick; {teleports} controller teleport(s) and {turners} left-stick turner(s) switched off.");
            return true;
        }

        /// <summary>Footfalls at the feet, kept quiet: the walk step and run clips in Assets/Audio.</summary>
        private static void AddFootsteps(SlideMover mover)
        {
            var steps = mover.GetComponent<Footsteps>();
            if (steps == null) steps = mover.gameObject.AddComponent<Footsteps>();
            var source = mover.GetComponent<AudioSource>();
            if (source == null) source = mover.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;

            var so = new SerializedObject(steps);
            so.FindProperty("_mover").objectReferenceValue = mover;
            so.FindProperty("_source").objectReferenceValue = source;
            so.FindProperty("_step").objectReferenceValue = Clip(StepClip);
            so.FindProperty("_run").objectReferenceValue = Clip(RunClip);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioClip Clip(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) { Debug.LogWarning($"[Slide] No sound at {path}."); return null; }
            if (!importer.forceToMono)
            {
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>The sprint meter: a thin bar on top of the left controller, facing up at the eyes.</summary>
        private static void AddMeter(SlideMover mover, Transform wrist)
        {
            var existing = wrist.Find("Sprint Meter");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var meter = new GameObject("Sprint Meter").transform;
            meter.SetParent(wrist, false);
            meter.localPosition = new Vector3(0f, 0.035f, -0.075f);
            meter.localRotation = Quaternion.Euler(60f, 0f, 0f);

            var bar = new GameObject("Bar").transform;
            bar.SetParent(meter, false);
            Bit(bar, "Back", new Vector3(0f, 0f, 0.002f), new Vector3(0.085f, 0.014f, 0.003f), new Color(0.08f, 0.07f, 0.06f));
            var fill = Bit(bar, "Fill", Vector3.zero, new Vector3(0.08f, 0.009f, 0.003f), new Color(0.55f, 0.85f, 0.45f));

            var view = meter.gameObject.AddComponent<StaminaMeter>();
            var so = new SerializedObject(view);
            so.FindProperty("_mover").objectReferenceValue = mover;
            so.FindProperty("_root").objectReferenceValue = bar.gameObject;
            so.FindProperty("_fill").objectReferenceValue = fill.transform;
            so.FindProperty("_fillRenderer").objectReferenceValue = fill.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            bar.gameObject.SetActive(false);
        }

        /// <summary>
        /// The camp map on the left stick's click: the overview image with a dot where the guard
        /// stands, and tonight's checklist on a paper card beside it.
        /// </summary>
        private static void AddMap(GameObject host, Transform head)
        {
            var map = host.GetComponent<FieldMap>();
            if (map == null) map = host.AddComponent<FieldMap>();
            foreach (var old in new[] { "Map Board", "Field Map" })
            {
                var existing = host.transform.Find(old);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
            }

            var root = new GameObject("Field Map").transform;
            root.SetParent(host.transform, false);

            var image = GameObject.CreatePrimitive(PrimitiveType.Quad);
            image.name = "Map Image";
            Object.DestroyImmediate(image.GetComponent<Collider>());
            image.transform.SetParent(root, false);
            image.transform.localPosition = new Vector3(-0.15f, 0f, 0f);
            image.transform.localScale = new Vector3(0.5f, 0.333f, 1f);
            image.GetComponent<MeshRenderer>().sharedMaterial = MapMaterial();

            var dot = Bit(image.transform, "You Are Here", new Vector3(0f, 0f, -0.004f), Vector3.one, new Color(0.9f, 0.15f, 0.1f));
            dot.transform.localScale = new Vector3(0.016f / 0.5f, 0.016f / 0.333f, 0.004f);

            var card = Bit(root, "Task Card", new Vector3(0.27f, 0f, 0.002f), new Vector3(0.3f, 0.333f, 0.003f), new Color(0.86f, 0.82f, 0.7f));
            var tasks = new GameObject("Tasks", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            tasks.transform.SetParent(root, false);
            tasks.transform.localPosition = new Vector3(0.27f, 0f, -0.002f);
            tasks.rectTransform.sizeDelta = new Vector2(0.27f, 0.31f);
            tasks.fontSize = 0.17f;
            tasks.color = new Color(0.12f, 0.1f, 0.08f);
            tasks.alignment = TextAlignmentOptions.TopLeft;
            tasks.textWrappingMode = TextWrappingModes.Normal;
            tasks.text = "<b>TONIGHT</b>";
            var list = card.AddComponent<CampfireFriends.Story.TaskListView>();
            var listSo = new SerializedObject(list);
            listSo.FindProperty("_text").objectReferenceValue = tasks;
            listSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(map);
            so.FindProperty("_head").objectReferenceValue = head;
            so.FindProperty("_board").objectReferenceValue = root;
            so.FindProperty("_youAreHere").objectReferenceValue = dot.transform;
            so.FindProperty("_ahead").floatValue = 0.55f;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
        }

        private static Material MapMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MapMaterialPath);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture"));
            var image = AssetDatabase.LoadAssetAtPath<Texture2D>(MapImage);
            material.SetTexture("_BaseMap", image);
            material.mainTexture = image;
            AssetDatabase.CreateAsset(material, MapMaterialPath);
            return material;
        }

        private static GameObject Bit(Transform parent, string name, Vector3 at, Vector3 size, Color colour)
        {
            var bit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bit.name = name;
            Object.DestroyImmediate(bit.GetComponent<Collider>());
            bit.transform.SetParent(parent, false);
            bit.transform.localPosition = at;
            bit.transform.localScale = size;
            bit.GetComponent<MeshRenderer>().sharedMaterial = Flat(name, colour);
            return bit;
        }

        private static Material Flat(string name, Color colour)
        {
            var path = $"{MaterialFolder}/Walk_{name.Replace(" ", "")}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            material.SetColor("_BaseColor", colour);
            material.color = colour;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static bool IsLeft(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
            {
                var name = p.name.ToLowerInvariant();
                if (name.Contains("left")) return true;
                if (name.Contains("right")) return false;
            }
            return false;
        }
    }
}
