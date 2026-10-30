using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampfireFriends.Story.Editor
{
    /// <summary>
    /// Night 1 waits for the walkies: the office's radio spot hands them out, and the shift's
    /// tasks (flashlights, work orders, map, fuel) stay locked until someone has one. Bixby says
    /// his piece in person on the ferry, so before that the radio is silent. Re-runnable; also
    /// applied when Night 1 is first set up.
    /// </summary>
    public static class WalkieGate
    {
        private const string ScenePath = "Assets/Scenes/CampAshwood.unity";
        private const string TakeWalkie = "Take a walkie (channel 2)";
        private const string Locked = "Grab a walkie first";

        private static readonly Night01Flag[] Gated =
        {
            Night01Flag.FlashlightCollected, Night01Flag.WorkOrderViewed, Night01Flag.MapInspected, Night01Flag.FuelCollected,
        };

        [MenuItem("Campfire Friends/Camp/Lock Night 1 Tasks Behind The Walkies")]
        public static void ApplyToCamp()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Apply(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void RunBatch()
        {
            try { ApplyToCamp(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void Apply(Scene scene)
        {
            int gated = 0;
            foreach (var story in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryInteractable>(true)))
            {
                var so = new SerializedObject(story);
                var raises = (Night01Flag)so.FindProperty("_raises").longValue;
                var requires = so.FindProperty("_requires");

                if (raises == Night01Flag.RadioCheckIn)
                {
                    so.FindProperty("_verb").stringValue = TakeWalkie;
                    var label = so.FindProperty("_label").objectReferenceValue as TMP_Text;
                    if (label != null) { label.text = TakeWalkie; EditorUtility.SetDirty(label); }
                }
                else if (Gated.Contains(raises) && ((Night01Flag)requires.longValue & Night01Flag.RadioCheckIn) == 0)
                {
                    requires.longValue |= (long)Night01Flag.RadioCheckIn;
                    so.FindProperty("_whenLocked").stringValue = Locked;
                    gated++;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Night1] {scene.name}: {gated} task(s) now wait for the walkies.");
        }
    }
}
