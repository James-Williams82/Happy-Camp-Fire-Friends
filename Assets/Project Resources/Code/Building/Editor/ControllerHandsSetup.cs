using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.OVR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampfireFriends.Construction.Editor
{
    /// <summary>
    /// Shows the player's hands while they hold controllers: the headset conforms a hand pose to
    /// each controller, the scene's existing hand visuals draw it, and the controller models only
    /// appear once a controller is put down. Hand interactions (hand grab, hand teleport) are
    /// gated to empty hands, so a held controller never drives both its own and a hand's
    /// interactors at once. Applied to the scene's rig, not the shared rig prefab.
    /// </summary>
    public static class ControllerHandsSetup
    {
        private const string GateName = "Only When Hand Is Empty";

        [MenuItem("Campfire Friends/Rig/Show Hands On Controllers In Open Scene")]
        public static void ApplyToOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            Apply(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void Apply(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            T[] All<T>() where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

            var manager = All<OVRManager>().FirstOrDefault();
            if (manager == null)
            {
                Debug.LogWarning($"[Rig] {scene.name}: no OVRManager, so hands on controllers aren't set up.");
                return;
            }

            manager.controllerDrivenHandPosesType = OVRManager.ControllerDrivenHandPosesType.ConformingToController;
            Record(manager);

            foreach (var hand in All<OVRHand>())
            {
                hand.m_showState = OVRInput.InputDeviceShowState.Always;
                Record(hand);
            }

            foreach (var controller in All<OVRControllerHelper>())
            {
                controller.m_showState = OVRInput.InputDeviceShowState.ControllerNotInHand;
                Record(controller);
            }

            int gated = 0;
            foreach (var tracker in All<ActiveStateTracker>())
                if (GateToEmptyHand(tracker)) gated++;

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Rig] {scene.name}: hands shown on controllers (conforming), controller models only when put down, {gated} hand interactor(s) limited to empty hands.");
        }

        /// <summary>
        /// A tracker switched by a hand being present now needs that hand to be empty too:
        /// its state becomes (hand present AND no controller in that hand).
        /// </summary>
        private static bool GateToEmptyHand(ActiveStateTracker tracker)
        {
            var so = new SerializedObject(tracker);
            var state = so.FindProperty("_activeState");

            // Hand interactors switch on "a hand is present": a HandRef, or a HandActiveState.
            var handState = state.objectReferenceValue as Component;
            if (!(handState is HandRef) && !(handState is HandActiveState)) return false;

            // Handedness only exists at runtime, so read it from the hierarchy (LeftHand, HandInteractorsRight…).
            var handedness = IsRight(handState.transform) ? Handedness.Right : Handedness.Left;

            var gate = new GameObject($"{GateName} ({tracker.name})");
            gate.transform.SetParent(tracker.transform, false);

            var empty = gate.AddComponent<OVRControllerInHandActiveState>();
            empty.HandType = handedness == Handedness.Right ? OVRInput.Hand.HandRight : OVRInput.Hand.HandLeft;
            empty.ShowState = OVRInput.InputDeviceShowState.ControllerNotInHand;

            var group = gate.AddComponent<ActiveStateGroup>();
            var groupSo = new SerializedObject(group);
            var states = groupSo.FindProperty("_activeStates");
            states.arraySize = 2;
            states.GetArrayElementAtIndex(0).objectReferenceValue = handState;
            states.GetArrayElementAtIndex(1).objectReferenceValue = empty;
            groupSo.FindProperty("_logicOperator").enumValueIndex = (int)ActiveStateGroup.ActiveStateGroupLogicOperator.AND;
            groupSo.ApplyModifiedPropertiesWithoutUndo();

            state.objectReferenceValue = group;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static bool IsRight(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                if (t.name.Contains("Right")) return true;
                if (t.name.Contains("Left")) return false;
            }
            return false;
        }

        /// <summary>Marks a changed component dirty, keeping the change as a prefab-instance override.</summary>
        private static void Record(Object target)
        {
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
