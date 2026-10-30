using System;
using System.Linq;
using CampfireFriends.Arrival;
using CampfireFriends.Menu;
using CampfireFriends.Network;
using Fusion;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CampfireFriends.Story.Editor
{
    /// <summary>
    /// Turns the Camp Ashwood blockout into Night 1 (Assets/Design/Camp_Ashwood_Night_01_First_Day_on_the_Job_v01.md):
    /// the story (offline and networked), the zones and interactables along the route, the power
    /// circuits, the scares, the walkie on the wrist, the flashlight, the work orders, the shift log
    /// and the end card. Props are found by their blockout names. Added once; rerunning leaves it.
    /// </summary>
    public static class Night01Setup
    {
        private const string ScenePath = "Assets/Scenes/CampAshwood.unity";
        private const string RootName = "Night 1";

        private static Scene _scene;
        private static Transform _root;
        private static Transform _head;

        [MenuItem("Campfire Friends/Camp/Add Night 1 To Camp Ashwood")]
        public static void Build()
        {
            _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (_scene.GetRootGameObjects().Any(r => r.name == RootName))
            {
                Debug.Log($"[Night1] {_scene.name} already has Night 1. Delete '{RootName}' to set it up again.");
                return;
            }

            _root = new GameObject(RootName).transform;
            SceneManager.MoveGameObjectToScene(_root.gameObject, _scene);
            _head = Find("CenterEyeAnchor").transform;

            Authorities();
            Arrival();
            Office();
            Shed();
            Cabin6();
            Perimeter();
            Finale();
            OnThePlayer();
            WalkieGate.Apply(_scene);

            EditorSceneManager.SaveScene(_scene);
            Debug.Log($"[Night1] {_scene.name}: Night 1 set up (Story mode).");
        }

        public static void BuildBatch()
        {
            try { Build(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        // ---------------------------------------------------------------- sequences

        private static void Authorities()
        {
            new GameObject("Story (offline)", typeof(LocalStory)).transform.SetParent(_root, false);
            new GameObject("Story (network)", typeof(NetworkObject), typeof(NetworkStory)).transform.SetParent(_root, false);
            new GameObject("Cabin 6 Watcher", typeof(Cabin6Watcher)).transform.SetParent(_root, false);

            var link = new GameObject("Ferry Story Link", typeof(FerryStoryLink));
            link.transform.SetParent(_root, false);
            Set(link.GetComponent<FerryStoryLink>(), ("_ferry", Object.FindFirstObjectByType<Ferry>()), ("_head", _head));
        }

        /// <summary>Sequence 1, at the camp dock: step off, read the notice, lift the chain, open the gate.</summary>
        private static void Arrival()
        {
            var arrival = Find("Dock Arrival (Night 1 opening)").transform;
            var dock = Find("Boat Dock (ferry landing)").transform;
            Zone("Dock", dock.position + Vector3.up * 1.5f, new Vector3(4f, 3f, 9f), Night01Flag.DockReached, Night01Flag.ArrivedAtCamp);

            Interact(Child(arrival, "Demolition Notice"), "Read the notice", Night01Flag.NoticeInspected);

            var gate = Child(arrival, "Dock Gate").transform;
            var chain = Child(gate, "Chain");
            Interact(chain, "Lift the chain", Night01Flag.GateOpened, Night01Flag.ArrivedAtCamp, "Wait for the ferry to dock", hide: chain);

            // Each leaf swings on a hinge at its post.
            foreach (var (leafName, sign) in new[] { ("Gate Leaf West", -1f), ("Gate Leaf East", 1f) })
            {
                var leaf = Child(gate, leafName).transform;
                var hinge = new GameObject($"{leafName} Hinge").transform;
                hinge.SetParent(gate, false);
                hinge.position = leaf.position + Vector3.right * (sign * leaf.localScale.x * 0.5f);
                leaf.SetParent(hinge, true);
                var swing = hinge.gameObject.AddComponent<SwingOnFlag>();
                Set(swing, ("_hinge", hinge));
                SetValue(swing, "_flag", (long)Night01Flag.GateOpened);
                SetVector(swing, "_openEuler", new Vector3(0f, -sign * 105f, 0f));
            }
        }

        /// <summary>Sequence 2: the office, flashlights, map, work orders, radio check-in.</summary>
        private static void Office()
        {
            var office = Find("Security Office").transform;
            var props = Child(office, "Props").transform;
            var centre = Child(office, "Floor").transform.position;

            Zone("Office", centre + Vector3.up * 1.4f, new Vector3(7.2f, 2.8f, 5.2f), Night01Flag.OfficeVisited);
            Zone("Back At Office", centre + Vector3.up * 1.4f, new Vector3(7.2f, 2.8f, 5.2f), Night01Flag.BackAtOffice, Night01Flag.CabinImpactPlayed);

            var locker = Interact(Child(props, "Flashlight Locker"), "Take a flashlight", Night01Flag.FlashlightCollected, repeatable: true);
            Interact(Child(props, "Clipboard Spot (work orders)"), "Read the work orders", Night01Flag.WorkOrderViewed);
            Interact(Child(props, "Map Board"), "Study the map", Night01Flag.MapInspected);
            Interact(Child(props, "Radio Spot"), "Check in on the radio", Night01Flag.RadioCheckIn);

            // The office's own light, on the security circuit.
            var lamp = Light("Office Light", centre + new Vector3(0f, 2.6f, 0f), 7f, 1.6f, new Color(1f, 0.82f, 0.55f), office);

            // TONIGHT'S ASSIGNMENTS on the north wall, read from inside.
            var board = Box("Assignments Board", centre + new Vector3(2.1f, 1.6f, 2.68f), new Vector3(1.4f, 0.9f, 0.03f), Paper(), office);
            var orders = Text("Assignments", "", board.transform.position + new Vector3(0f, 0f, -0.02f), 1.3f, 0.055f, new Color(0.12f, 0.1f, 0.1f), office);
            orders.alignment = TextAlignmentOptions.TopLeft;
            orders.rectTransform.sizeDelta = new Vector2(1.3f, 0.85f);
            orders.textWrappingMode = TextWrappingModes.Normal;
            Set(_root.gameObject.AddComponent<WorkOrderBoard>(), ("_text", orders));

            // The shift log: three report planks over the desk once everyone's back.
            var log = new GameObject("Shift Log").transform;
            log.SetParent(office, false);
            var deskTop = Child(props, "Desk").transform.position + new Vector3(0f, 0.7f, -0.55f);
            var entry = Text("Incident Line", "UNEXPLAINED ACTIVITY: ______", deskTop + new Vector3(0f, 0.5f, 0f), 1.4f, 0.05f, new Color(0.95f, 0.9f, 0.8f), log);
            var shiftLog = log.gameObject.AddComponent<ShiftLog>();
            Set(shiftLog,
                ("_structural", Plank(log, "Report Structural", deskTop + new Vector3(-0.5f, 0.3f, 0f), new Vector3(0.45f, 0.12f, 0.03f))),
                ("_trespass", Plank(log, "Report Trespass", deskTop + new Vector3(0f, 0.3f, 0f), new Vector3(0.45f, 0.12f, 0.03f))),
                ("_unknown", Plank(log, "Report Unknown", deskTop + new Vector3(0.5f, 0.3f, 0f), new Vector3(0.45f, 0.12f, 0.03f))),
                ("_entry", entry));

            // NIGHT 1 COMPLETE, on the wall above the map.
            var card = Text("End Card", "NIGHT 1 COMPLETE\n<size=50%>FIRST DAY ON THE JOB</size>", centre + new Vector3(-0.5f, 2.3f, 2.66f), 3f, 0.16f, new Color(0.9f, 0.85f, 0.7f), office);
            Set(_root.gameObject.AddComponent<EndCard>(), ("_card", card.gameObject));

            Circuit("Security Circuit", Night01Flag.SecurityCircuitOn, Find("Yard Lamps (security circuit)").GetComponentsInChildren<Light>(true).Append(lamp).ToArray());
            Circuit("Recreation Circuit", Night01Flag.RecCircuitOn, Find("Court Lights (recreation circuit)").GetComponentsInChildren<Light>(true));

            // Leave the locker's flashlight hookup for OnThePlayer.
            _locker = locker;
        }

        private static StoryInteractable _locker;

        /// <summary>Sequence 3: fuse, fuel, starter, SECURITY breaker; the bucket falls while someone's in the cabinet.</summary>
        private static void Shed()
        {
            var shed = Find("Maintenance Shed").transform;
            var props = Child(shed, "Props").transform;
            var generator = Find("Generator").transform;

            Zone("Shed", Child(shed, "Floor").transform.position + new Vector3(-2f, 1.5f, -1f), new Vector3(13f, 3f, 10f), Night01Flag.ShedVisited, Night01Flag.WorkOrderViewed);

            var cabinet = Interact(Child(props, "Fuse Cabinet (left side)"), "Take the fuse", Night01Flag.FuseCollected, Night01Flag.WorkOrderViewed, "Read the work orders first");
            var later = cabinet.gameObject.AddComponent<RaiseLater>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(cabinet.UsedLocally, later.Trigger);

            var fuel = Child(props, "Fuel Can");
            Interact(fuel, "Take the fuel can", Night01Flag.FuelCollected, hide: fuel);

            var panel = Child(props, "Breaker Panel");
            Interact(panel, "Insert the fuse", Night01Flag.FuseInserted, Night01Flag.FuseCollected, "Needs a fuse");
            var fuseInPanel = Box("Fuse (in panel)", panel.transform.position + new Vector3(-0.06f, 0.2f, 0f), new Vector3(0.04f, 0.08f, 0.04f), Paper(), panel.transform.parent);
            fuseInPanel.SetActive(false);
            var showFuse = fuseInPanel.transform.parent.gameObject.AddComponent<ShowOnFlag>();
            SetValue(showFuse, "_flag", (long)Night01Flag.FuseInserted);
            SetArray(showFuse, "_show", fuseInPanel);

            var security = Box("Breaker SECURITY", panel.transform.position + new Vector3(-0.07f, -0.15f, -0.12f), new Vector3(0.06f, 0.12f, 0.08f), Paper(), panel.transform.parent);
            Interact(security, "Switch on SECURITY", Night01Flag.SecurityCircuitOn, Night01Flag.GeneratorStarted, "The generator isn't running");
            var rec = Box("Breaker RECREATION", panel.transform.position + new Vector3(-0.07f, -0.15f, 0.12f), new Vector3(0.06f, 0.12f, 0.08f), Paper(), panel.transform.parent);
            Interact(rec, "Switch on RECREATION", Night01Flag.RecCircuitOn, Night01Flag.GeneratorStarted, "The generator isn't running");

            Interact(Child(generator, "Fuel Feed"), "Pour in the fuel", Night01Flag.GeneratorFueled, Night01Flag.FuelCollected | Night01Flag.FuseInserted, "Fuse first, then fuel");
            Interact(Child(generator, "Starter Pull"), "Pull the starter", Night01Flag.GeneratorStarted, Night01Flag.GeneratorFueled, "It needs fuel");

            // The generator runs: a rumble that starts with it.
            var hum = new GameObject("Generator Hum", typeof(AudioSource));
            hum.transform.SetParent(generator, false);
            hum.transform.position = Child(generator, "Generator Body").transform.position;
            Spatial(hum.GetComponent<AudioSource>(), Clip("generator") ?? Clip("Amb_Rumble"), loop: true, volume: 0.6f, maxDistance: 30f);
            hum.SetActive(false);
            var runs = generator.gameObject.AddComponent<ShowOnFlag>();
            SetValue(runs, "_flag", (long)Night01Flag.GeneratorStarted);
            SetArray(runs, "_show", hum);

            // SE-01: the bucket falls from the far shelf. A real bucket where the camping pack has one.
            var placeholder = Child(props, "Bucket");
            var bucket = placeholder;
            var bucketPrefab = Prefab("Metal_Bucket");
            if (bucketPrefab != null)
            {
                bucket = (GameObject)PrefabUtility.InstantiatePrefab(bucketPrefab, props);
                bucket.name = "Bucket";
                bucket.transform.position = placeholder.transform.position - Vector3.up * 0.15f;
                Object.DestroyImmediate(placeholder);
            }
            var clang = new GameObject("Bucket Clang", typeof(AudioSource));
            clang.transform.SetParent(props, false);
            Spatial(clang.GetComponent<AudioSource>(), Clip("Hit_Metal"), volume: 0.9f, maxDistance: 25f);
            var fall = props.gameObject.AddComponent<BucketFall>();
            Set(fall, ("_bucket", bucket.transform), ("_clang", clang.GetComponent<AudioSource>()));

            // The rec-hall sign buzzes once when the power comes on, though it isn't on that circuit.
            var sign = Find("Rec Hall Sign (buzzes)");
            Sound("Rec Hall Buzz", sign.transform.position, Night01Flag.SecurityCircuitOn, Clip("Misc_Noise_1"), delay: 2.5f, maxDistance: 40f);
        }

        /// <summary>Sequence 4: arriving at Cabin 6 (the watcher judges the boards).</summary>
        private static void Cabin6()
        {
            Zone("Cabin 6", new Vector3(-2f, 1.5f, 26.5f), new Vector3(12f, 3f, 10f), Night01Flag.Cabin06Arrived, Night01Flag.SecurityCircuitOn);
        }

        /// <summary>Sequence 5: dusk; the dining-hall latch, an entry sign, the padlock, an optional hoop; the footsteps.</summary>
        private static void Perimeter()
        {
            Interact(Find("Dining Hall Latch"), "Check the latch", Night01Flag.DiningHallInspected, Night01Flag.Cabin06Secured, "Cabin 6 first");

            var doors = Find("Dining Hall Doors (latch to check)").transform.position;
            var print = Box("Wet Boot Print", doors + new Vector3(-1.4f, -1.08f, -0.8f), new Vector3(0.14f, 0.01f, 0.32f), Mat("Camp_Mud"), _root);
            Interact(print, "Report the boot print", Night01Flag.EntrySignReported, Night01Flag.Cabin06Secured);

            // The padlocked service gate by the court: the perimeter check.
            var court = Find("Basketball Court (working lights)").transform.position;
            var gateAt = court + new Vector3(-9.5f, 0f, -3f);
            Box("Service Gate", gateAt + new Vector3(0f, 0.75f, 0f), new Vector3(0.08f, 1.5f, 2.4f), Mat("Camp_Wood"), _root);
            var padlock = Box("Service Gate Padlock", gateAt + new Vector3(0.08f, 1f, 0f), new Vector3(0.06f, 0.08f, 0.05f), Mat("Camp_Metal"), _root);
            Interact(padlock, "Check the padlock", Night01Flag.PerimeterInspected, Night01Flag.Cabin06Secured);

            Interact(Find("Basketball (flat)"), "Bounce the ball", Night01Flag.LeisureNoticed);

            // SE-02, for whoever walks the path between the court and the dining hall alone.
            var path = court + new Vector3(18f, 1.5f, 0f);
            var zone = Zone("Footsteps (private)", path, new Vector3(6f, 3f, 6f), Night01Flag.None, Night01Flag.Cabin06Secured);
            var voice = new GameObject("Footsteps Voice", typeof(AudioSource));
            voice.transform.SetParent(_root, false);
            Spatial(voice.GetComponent<AudioSource>(), null, volume: 0.8f, maxDistance: 20f);
            var scare = voice.AddComponent<FootstepScare>();
            Set(scare, ("_head", _head), ("_voice", voice.GetComponent<AudioSource>()));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(zone.EnteredLocally, scare.Play);
        }

        /// <summary>Sequence 6: on the way back, three knocks from inside Cabin 6, then the impact.</summary>
        private static void Finale()
        {
            Zone("Knock Corridor", new Vector3(0f, 1.5f, 26.5f), new Vector3(9f, 3f, 12f), Night01Flag.CabinKnocksPlayed, Night01Flag.DiningHallInspected | Night01Flag.PerimeterInspected);

            var inside = new Vector3(-4.2f, 1.2f, 26.5f);
            var knocks = Sound("Knocks", inside, Night01Flag.CabinKnocksPlayed, null, delay: 0.6f, maxDistance: 35f);
            SetValue(knocks, "_repeats", 3);
            SetFloat(knocks, "_gap", 0.95f);
            SetBool(knocks, "_knocks", true);

            var opening = new GameObject("Cabin 6 Opening", typeof(BoxCollider));
            opening.transform.SetParent(_root, false);
            opening.transform.position = new Vector3(-2.75f, 1f, 26.5f);
            var box = opening.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.5f, 2f, 1f);

            var hit = new GameObject("Impact", typeof(AudioSource));
            hit.transform.SetParent(opening.transform, false);
            Spatial(hit.GetComponent<AudioSource>(), Clip("Hit_upstair_boom"), volume: 1f, maxDistance: 60f);

            var impact = opening.AddComponent<CabinImpact>();
            Set(impact, ("_opening", box), ("_hit", hit.GetComponent<AudioSource>()), ("_dust", Dust(opening.transform)));
        }

        /// <summary>The walkie on the left wrist (the owner's lines as text), and the flashlight.</summary>
        private static void OnThePlayer()
        {
            var wrist = Find("LeftHandAnchor").transform;

            var walkie = new GameObject("Walkie").transform;
            walkie.SetParent(wrist, false);
            walkie.localPosition = new Vector3(0f, 0.045f, -0.19f);
            walkie.localRotation = Quaternion.Euler(60f, 0f, 0f);

            var screen = Box("Screen", walkie.position, Vector3.one, Mat("Camp_DarkWood"), walkie);
            screen.transform.localPosition = new Vector3(0f, 0f, 0.004f);
            screen.transform.localRotation = Quaternion.identity;
            screen.transform.localScale = new Vector3(0.2f, 0.11f, 0.006f);
            Object.DestroyImmediate(screen.GetComponent<Collider>());

            var text = Text("Radio Text", "", walkie.position, 0.19f, 0.009f, new Color(0.95f, 0.9f, 0.75f), walkie);
            text.transform.localPosition = Vector3.zero;
            text.transform.localRotation = Quaternion.identity;
            text.rectTransform.sizeDelta = new Vector2(0.19f, 0.1f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.TopLeft;

            var logPlank = Plank(walkie, "LOG", walkie.position, new Vector3(0.06f, 0.022f, 0.006f));
            logPlank.transform.localPosition = new Vector3(-0.07f, -0.07f, 0f);
            logPlank.transform.localRotation = Quaternion.identity;
            var report = Plank(walkie, "Report", walkie.position, new Vector3(0.11f, 0.022f, 0.006f));
            report.transform.localPosition = new Vector3(0.04f, -0.07f, 0f);
            report.transform.localRotation = Quaternion.identity;

            var speaker = walkie.gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0.5f;
            speaker.volume = 0.6f;

            var panel = walkie.gameObject.AddComponent<RadioPanel>();
            Set(panel, ("_text", text), ("_log", logPlank), ("_report", report), ("_speaker", speaker), ("_screen", screen));
            logPlank.Text = "LOG";

            // The flashlight: the camping pack's model in the left hand with a spot light, hidden until taken.
            var flashlight = new GameObject("Flashlight").transform;
            flashlight.SetParent(wrist, false);
            flashlight.localPosition = new Vector3(0f, -0.02f, 0.04f);
            var model = Prefab("Flashlight");
            if (model != null)
            {
                var m = (GameObject)PrefabUtility.InstantiatePrefab(model, flashlight);
                m.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                foreach (var c in m.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            }
            var beam = new GameObject("Beam", typeof(Light)).GetComponent<Light>();
            beam.transform.SetParent(flashlight, false);
            beam.transform.localPosition = new Vector3(0f, 0f, 0.12f);
            beam.type = LightType.Spot;
            beam.color = new Color(1f, 0.95f, 0.85f);
            CampfireFriends.Camp.Editor.CampWalkFixes.Flashlight(beam);
            beam.shadows = LightShadows.None;

            var equip = wrist.gameObject.AddComponent<FlashlightEquip>();
            Set(equip, ("_flashlight", flashlight.gameObject));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(_locker.UsedLocally, equip.Equip);
        }

        // ---------------------------------------------------------------- building blocks

        /// <summary>Makes a prop pointable: collider, surface, ray interactable, a label, and the story behaviour.</summary>
        private static StoryInteractable Interact(GameObject target, string verb, Night01Flag raises, Night01Flag requires = Night01Flag.None,
            string locked = "Not yet", GameObject hide = null, bool repeatable = false)
        {
            if (target == null) throw new InvalidOperationException($"Night 1: missing a prop for '{verb}'.");
            var collider = target.GetComponent<Collider>();
            if (collider == null) collider = target.AddComponent<BoxCollider>();

            var surface = target.AddComponent<ColliderSurface>();
            Set(surface, ("_collider", collider));
            var ray = target.AddComponent<RayInteractable>();
            Set(ray, ("_surface", surface));

            Physics.SyncTransforms();
            var shown = target.GetComponentInChildren<Renderer>();
            var bounds = shown != null ? shown.bounds : collider.bounds;
            var label = Text($"{target.name} Label", verb, new Vector3(bounds.center.x, bounds.max.y + 0.18f, bounds.center.z), 0.9f, 0.05f, new Color(0.97f, 0.93f, 0.8f), _root);
            label.gameObject.AddComponent<Billboard>();
            label.gameObject.SetActive(false);

            var story = target.AddComponent<StoryInteractable>();
            Set(story, ("_interactable", ray), ("_label", label));
            SetString(story, "_verb", verb);
            SetString(story, "_whenLocked", locked);
            SetValue(story, "_raises", (long)raises);
            SetValue(story, "_requires", (long)requires);
            SetBool(story, "_repeatable", repeatable);
            if (hide != null) SetArray(story, "_hideWhenDone", hide);
            return story;
        }

        private static StoryZone Zone(string name, Vector3 centre, Vector3 size, Night01Flag raises, Night01Flag requires = Night01Flag.None)
        {
            var go = new GameObject($"Zone: {name}", typeof(BoxCollider));
            go.transform.SetParent(_root, false);
            go.transform.position = centre;
            var box = go.GetComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            var zone = go.AddComponent<StoryZone>();
            Set(zone, ("_head", _head));
            SetValue(zone, "_raises", (long)raises);
            SetValue(zone, "_requires", (long)requires);
            return zone;
        }

        private static void Circuit(string name, Night01Flag flag, Light[] lights)
        {
            var go = new GameObject(name, typeof(PowerCircuit));
            go.transform.SetParent(_root, false);
            var circuit = go.GetComponent<PowerCircuit>();
            SetValue(circuit, "_flag", (long)flag);
            var so = new SerializedObject(circuit);
            var list = so.FindProperty("_lights");
            list.arraySize = lights.Length;
            for (int i = 0; i < lights.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = lights[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SoundOnFlag Sound(string name, Vector3 at, Night01Flag flag, AudioClip clip, float delay = 0f, float maxDistance = 30f)
        {
            var go = new GameObject(name, typeof(AudioSource));
            go.transform.SetParent(_root, false);
            go.transform.position = at;
            Spatial(go.GetComponent<AudioSource>(), clip, volume: 1f, maxDistance: maxDistance);
            var sound = go.AddComponent<SoundOnFlag>();
            Set(sound, ("_source", go.GetComponent<AudioSource>()));
            SetValue(sound, "_flag", (long)flag);
            SetFloat(sound, "_delay", delay);
            return sound;
        }

        private static void Spatial(AudioSource source, AudioClip clip, bool loop = false, float volume = 1f, float maxDistance = 30f)
        {
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = loop;
            source.volume = volume;
            source.spatialBlend = 1f;
            source.spatialize = true;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2f;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;
        }

        private static Light Light(string name, Vector3 at, float range, float intensity, Color colour, Transform parent)
        {
            var light = new GameObject(name, typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.position = at;
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = colour;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        private static ParticleSystem Dust(Transform parent)
        {
            var go = new GameObject("Dust", typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.2f, 0f, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 2.2f;
            main.startSpeed = 1.2f;
            main.startSize = 0.25f;
            main.startColor = new Color(0.55f, 0.48f, 0.4f, 0.55f);
            main.gravityModifier = 0.05f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1f, 2f, 0.2f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            return ps;
        }

        /// <summary>A pointable plank (as on the menu signpost): board + label in an unscaled group.</summary>
        private static PlankButton Plank(Transform parent, string name, Vector3 at, Vector3 size)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.position = at;
            var board = Box("Board", at, size, Mat("Camp_Wood"), group);
            board.transform.localPosition = Vector3.zero;
            var label = Text("Label", name.ToUpperInvariant(), at, size.x * 0.92f, size.y * 0.5f, new Color(0.97f, 0.93f, 0.82f), group);
            label.transform.localPosition = new Vector3(0f, 0f, -size.z * 0.5f - 0.002f);

            var surface = board.AddComponent<ColliderSurface>();
            Set(surface, ("_collider", board.GetComponent<Collider>()));
            var ray = board.AddComponent<RayInteractable>();
            Set(ray, ("_surface", surface));
            var button = group.gameObject.AddComponent<PlankButton>();
            Set(button, ("_interactable", ray), ("_plank", board.GetComponent<Renderer>()), ("_label", label));
            return button;
        }

        private static GameObject Box(string name, Vector3 at, Vector3 size, Material material, Transform parent)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, true);
            box.transform.position = at;
            box.transform.localScale = size;
            if (material != null) box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static TextMeshPro Text(string name, string text, Vector3 at, float width, float size, Color colour, Transform parent)
        {
            var go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var tmp = go.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size * 10f;
            tmp.color = colour;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(width, size * 4f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        // ---------------------------------------------------------------- lookups

        private static GameObject Find(string name)
        {
            var found = _scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == name);
            if (found == null) throw new InvalidOperationException($"Night 1: '{name}' isn't in {_scene.name}. Build the Camp Ashwood blockout first.");
            return found.gameObject;
        }

        private static GameObject Child(Component parent, string name) => Child(parent.transform, name);

        private static GameObject Child(Transform parent, string name)
        {
            var found = parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (found == null) throw new InvalidOperationException($"Night 1: '{parent.name}' has no '{name}'.");
            return found.gameObject;
        }

        private static AudioClip Clip(string name)
        {
            var path = AssetDatabase.FindAssets($"{name} t:AudioClip").Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == name);
            if (path == null) Debug.LogWarning($"[Night1] No sound '{name}'; that cue is silent until one is added.");
            return path != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(path) : null;
        }

        private static GameObject Prefab(string name)
        {
            var path = AssetDatabase.FindAssets($"{name} t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == name);
            return path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
        }

        private static Material Mat(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"Assets/Project Resources/Camp/Materials/{name}.mat");

        private static Material Paper() => Mat("Camp_Paper");

        private static void Set(Object target, params (string field, Object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var p = so.FindProperty(field) ?? throw new InvalidOperationException($"{target.GetType().Name} has no '{field}'.");
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, params GameObject[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(Object target, string field, long value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p.propertyType == SerializedPropertyType.Enum) p.enumValueFlag = (int)value;
            else p.longValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector(Object target, string field, Vector3 value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
