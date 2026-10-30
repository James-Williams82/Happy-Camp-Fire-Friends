using System.Linq;
using Oculus.Interaction.Locomotion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampfireFriends.Climbing.Editor
{
    /// <summary>
    /// Adds climbing to a scene's player: a <see cref="ClimbController"/> moving the player through
    /// the scene's <see cref="PlayerLocomotor"/>, gripping with the controllers' grip buttons or a
    /// pinch. Run as part of Rebuild Build Kit, which also adds a ladder and a climbing wall to
    /// the sandbox to try it on.
    /// </summary>
    public static class ClimbingSetup
    {
        [MenuItem("Campfire Friends/Climbing/Add Climbing To Open Scene")]
        public static void ApplyToOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            Apply(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static bool Apply(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            T[] All<T>() where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
            Transform Named(string name) => All<Transform>().FirstOrDefault(t => t.name == name);

            var locomotor = All<PlayerLocomotor>().FirstOrDefault();
            if (locomotor == null)
            {
                Debug.LogWarning($"[Climbing] {scene.name}: no PlayerLocomotor (locomotion building block), so climbing isn't added.");
                return false;
            }

            var locomotorSo = new SerializedObject(locomotor);
            var origin = (Transform)locomotorSo.FindProperty("_playerOrigin").objectReferenceValue;
            var head = (Transform)locomotorSo.FindProperty("_playerHead").objectReferenceValue ?? Named("CenterEyeAnchor");

            var controller = All<ClimbController>().FirstOrDefault();
            if (controller == null)
            {
                var go = new GameObject("Climbing", typeof(OvrClimbInput), typeof(ClimbController));
                SceneManager.MoveGameObjectToScene(go, scene);
                controller = go.GetComponent<ClimbController>();
            }

            var hands = All<OVRHand>();
            OVRHand Hand(string side) => hands.FirstOrDefault(h => h.name.ToLowerInvariant().Contains(side));
            Assign(controller.GetComponent<OvrClimbInput>(), ("_leftHand", Hand("left")), ("_rightHand", Hand("right")));
            Assign(controller,
                ("_input", controller.GetComponent<OvrClimbInput>()),
                ("_locomotor", locomotor),
                ("_playerOrigin", origin),
                ("_head", head),
                ("_leftHand", Named("LeftHandAnchor")),
                ("_rightHand", Named("RightHandAnchor")));

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Climbing] {scene.name}: climbing wired to {locomotor.name} (origin {(origin != null ? origin.name : "none")}).");
            return true;
        }

        /// <summary>
        /// Something to climb in the sandbox, made once: a ladder up a 2.5 m tower, and a climbing
        /// wall against the raised deck. Built from the kit's own piece prefabs.
        /// </summary>
        public static void AddTestClimbs(Scene scene, GameObject ladderPrefab, GameObject wallPrefab, Material towerMaterial)
        {
            if (scene.GetRootGameObjects().Any(r => r.name == "Climb Test")) return;

            var root = new GameObject("Climb Test");
            SceneManager.MoveGameObjectToScene(root, scene);

            var tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Tower (2.5 m)";
            tower.transform.SetParent(root.transform, false);
            tower.transform.position = new Vector3(4f, 1.25f, 6f);
            tower.transform.localScale = new Vector3(2f, 2.5f, 2f);
            if (towerMaterial != null) tower.GetComponent<MeshRenderer>().sharedMaterial = towerMaterial;

            Place(ladderPrefab, root.transform, new Vector3(4f, 0f, 4.75f), "Ladder");

            // The raised deck from the sandbox spans z 2.5–5.5 at x −4; its top is 1 m up.
            Place(wallPrefab, root.transform, new Vector3(-4f, 0f, 2.25f), "Climbing Wall");
        }

        private static void Place(GameObject prefab, Transform parent, Vector3 position, string name)
        {
            if (prefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = position;

            // Scenery, not a placed piece: the build tool must not try to select or move it.
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var view in instance.GetComponentsInChildren<CampfireFriends.Construction.PieceView>(true)) Object.DestroyImmediate(view);
        }

        private static void Assign(Object target, params (string field, Object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values) so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
