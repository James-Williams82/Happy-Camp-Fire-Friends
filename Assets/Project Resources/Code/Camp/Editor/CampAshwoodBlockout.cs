using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CampfireFriends.Menu;
using Object = UnityEngine.Object;

namespace CampfireFriends.Camp.Editor
{
    /// <summary>
    /// Blocks out Camp Ashwood from Assets/Design/camp_overview_map.png for Night 1: the gate, the
    /// security office, the maintenance shed and generator, Cabin Row A (with Cabin 6's door
    /// opening on the build lattice), the court, the dining hall, the fire pit and the lake
    /// overlook, ringed by woods. Map pixels become metres at <see cref="MetresPerPixel"/>, with
    /// the main gate at the origin and north (+Z) toward the lake.
    ///
    /// The scene starts as a copy of BuildSandbox, so the player rig, building, sessions, the
    /// shift clock and the ambience come with it; the sandbox's test terrain is removed. The
    /// blockout is generated once: rerunning leaves an existing camp alone.
    /// </summary>
    public static class CampAshwoodBlockout
    {
        public const string ScenePath = "Assets/Scenes/CampAshwood.unity";
        private const string SandboxPath = "Assets/Scenes/BuildSandbox.unity";
        private const string MaterialsFolder = "Assets/Project Resources/Camp/Materials";
        private const string TreesFolder = "Assets/SimpleNaturePack/Prefabs";
        private const string RootName = "Camp Ashwood (blockout)";

        private const float MetresPerPixel = 0.12f;

        // Ashwood Lake: the camp's shore to the far shore where the game begins, and the ferry's line across.
        private const float LakeSouth = 77.6f;
        private const float LakeAcross = 122f;
        private const float LakeNorth = LakeSouth + LakeAcross;
        private const float FerryX = 17f;
        private static readonly Vector2 GatePixel = new Vector2(565f, 880f);

        /// <summary>A map pixel as a point on the ground.</summary>
        private static Vector3 Map(float px, float py) =>
            new Vector3((px - GatePixel.x) * MetresPerPixel, 0f, (GatePixel.y - py) * MetresPerPixel);

        private enum Side { North, South, East, West }

        private sealed class Door
        {
            public Side Side;
            public float Offset;   // along the wall from its centre
            public float Width = 1f;
            public float Height = 2f;
        }

        private static Dictionary<string, Material> _materials;
        private static readonly List<Rect> Clearings = new List<Rect>();

        [MenuItem("Campfire Friends/Camp/Build Camp Ashwood Blockout")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!File.Exists(ScenePath))
            {
                if (!AssetDatabase.CopyAsset(SandboxPath, ScenePath)) throw new InvalidOperationException($"Couldn't copy {SandboxPath}.");
                AssetDatabase.Refresh();
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (scene.GetRootGameObjects().Any(r => r.name == RootName))
            {
                Debug.Log($"[Camp] {scene.name} already has its blockout; left as it is. Delete '{RootName}' to regenerate.");
                return;
            }

            RemoveSandboxTerrain(scene);
            _materials = BuildMaterials();
            Clearings.Clear();

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            BuildGround(root.transform);
            BuildEntrance(root.transform);
            BuildSecurityOffice(root.transform);
            BuildMaintenanceShed(root.transform);
            BuildCabinRowA(root.transform);
            BuildCentre(root.transform);
            BuildLakeside(root.transform);
            BuildDockArrival(root.transform);
            var farShore = BuildFarShore(root.transform);
            var ferry = BuildFerry(root.transform);
            BuildBackground(root.transform);
            BuildPaths(root.transform);
            BuildYardLamps(root.transform);
            BuildWoods(root.transform);

            PlacePlayerAtCampfire(scene);
            WireArrival(scene, farShore, ferry);
            StaticBatch(root);

            EditorSceneManager.SaveScene(scene);
            EnsureInBuildSettings(ScenePath);
            Debug.Log($"[Camp] {scene.name}: Camp Ashwood blocked out from the map. Players start at the campfire across the lake.");
        }

        // ---------------------------------------------------------------- areas

        private static void RemoveSandboxTerrain(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == "Ground" || go.name.StartsWith("Rock (") || go.name.StartsWith("Raised Deck") || go.name == "Climb Test")
                    Object.DestroyImmediate(go);
            }
        }

        private static void BuildGround(Transform root)
        {
            // One big walkable meadow; the woods and the lake shape where people actually go.
            var ground = Box(root, "Ground", new Vector3(10f, -0.25f, 100f), new Vector3(220f, 0.5f, 280f), "Grass");
            Walkable(ground);
        }

        /// <summary>The road, the main gate with its chain and notices, and the fence either side.</summary>
        private static void BuildEntrance(Transform root)
        {
            var area = Group(root, "Main Entrance");

            var road = Box(area, "Road", new Vector3(0f, 0.02f, -10f), new Vector3(170f, 0.04f, 7f), "Asphalt");
            Walkable(road);
            Clear(new Vector3(0f, 0f, -10f), 170f, 10f);

            // Gate posts and two leaves, closed, with a loose chain between them.
            Box(area, "Gate Post West", new Vector3(-3.2f, 1.2f, 2f), new Vector3(0.3f, 2.4f, 0.3f), "DarkWood");
            Box(area, "Gate Post East", new Vector3(3.2f, 1.2f, 2f), new Vector3(0.3f, 2.4f, 0.3f), "DarkWood");
            var gate = Group(area, "Gate");
            Box(gate, "Gate Leaf West", new Vector3(-1.55f, 0.9f, 2f), new Vector3(3f, 1.4f, 0.08f), "Wood");
            Box(gate, "Gate Leaf East", new Vector3(1.55f, 0.9f, 2f), new Vector3(3f, 1.4f, 0.08f), "Wood");
            Box(gate, "Chain", new Vector3(0f, 1.1f, 1.94f), new Vector3(0.5f, 0.06f, 0.04f), "Metal", collider: false);

            // CAMP ASHWOOD sign on an arch over the gate, paint faded.
            Box(area, "Sign Post West", new Vector3(-3.6f, 2.3f, 2.3f), new Vector3(0.25f, 4.6f, 0.25f), "DarkWood");
            Box(area, "Sign Post East", new Vector3(3.6f, 2.3f, 2.3f), new Vector3(0.25f, 4.6f, 0.25f), "DarkWood");
            Box(area, "Sign Board", new Vector3(0f, 4.1f, 2.3f), new Vector3(7.6f, 0.9f, 0.12f), "Wood");
            Label(area, "Sign Text", "CAMP ASHW OD", new Vector3(0f, 4.1f, 2.22f), 6f, 0.6f, new Color(0.85f, 0.8f, 0.65f));

            var notice = Box(area, "Demolition Notice Post", new Vector3(4.6f, 0.75f, 1.6f), new Vector3(0.12f, 1.5f, 0.12f), "DarkWood");
            Box(area, "Demolition Notice", new Vector3(4.6f, 1.35f, 1.53f), new Vector3(0.6f, 0.45f, 0.02f), "Paper");
            Label(area, "Demolition Notice Text", "PRIVATE PROPERTY\nDEMOLITION PENDING", new Vector3(4.6f, 1.35f, 1.51f), 0.55f, 0.06f, new Color(0.6f, 0.1f, 0.08f));

            // Perimeter fence along the road, the gate the only way in.
            Box(area, "Fence West", new Vector3(-36.6f, 0.75f, 2f), new Vector3(66.5f, 1.5f, 0.1f), "Wood");
            Box(area, "Fence East", new Vector3(41.6f, 0.75f, 2f), new Vector3(76.5f, 1.5f, 0.1f), "Wood");

            // A worn sign points the way to the office.
            Box(area, "Office Sign Post", new Vector3(5.5f, 0.8f, 5.5f), new Vector3(0.1f, 1.6f, 0.1f), "DarkWood");
            Box(area, "Office Sign", new Vector3(6.1f, 1.45f, 5.5f), new Vector3(1.1f, 0.25f, 0.04f), "Wood");
            Label(area, "Office Sign Text", "SECURITY  >", new Vector3(6.1f, 1.45f, 5.47f), 1f, 0.12f, new Color(0.9f, 0.88f, 0.8f));

            Clear(new Vector3(0f, 0f, 4f), 14f, 8f);
            Clear(new Vector3(0f, 0f, -3f), 18f, 12f); // where the players arrive
        }

        /// <summary>The security office: the players' base. Enterable, door to the parking lot.</summary>
        private static void BuildSecurityOffice(Transform root)
        {
            var at = Map(800f, 760f);
            var office = Building(root, "Security Office", at, new Vector3(8f, 3f, 6f),
                new[] { new Door { Side = Side.South, Offset = -1.5f, Width = 1.2f, Height = 2.1f } }, "Siding", "Roof");

            // Placeholders for Night 1's office props; they become interactables in the mission pass.
            var props = Group(office, "Props");
            Box(props, "Desk", at + new Vector3(1.5f, 0.38f, 1.6f), new Vector3(1.8f, 0.76f, 0.8f), "DarkWood");
            Box(props, "Clipboard Spot (work orders)", at + new Vector3(1.2f, 0.78f, 1.5f), new Vector3(0.25f, 0.02f, 0.33f), "Paper", collider: false);
            Box(props, "Logbook Spot", at + new Vector3(2.0f, 0.78f, 1.6f), new Vector3(0.3f, 0.04f, 0.4f), "DarkWood", collider: false);
            Box(props, "Radio Spot", at + new Vector3(2.3f, 0.86f, 1.8f), new Vector3(0.15f, 0.18f, 0.08f), "Metal", collider: false);
            Box(props, "Flashlight Locker", at + new Vector3(-3.4f, 0.95f, 2.3f), new Vector3(0.9f, 1.9f, 0.5f), "Metal");
            Box(props, "Map Board", at + new Vector3(-0.5f, 1.6f, 2.77f), new Vector3(1.6f, 1.1f, 0.03f), "Paper", collider: false);
            Box(props, "Key Hooks", at + new Vector3(-2.2f, 1.6f, 2.88f), new Vector3(0.6f, 0.2f, 0.04f), "Metal", collider: false);
            Box(props, "Heater (do not use)", at + new Vector3(3.4f, 0.3f, -2.2f), new Vector3(0.6f, 0.6f, 0.3f), "Metal");

            // Parking lot in front, with the owner's beat-up pickup.
            var lot = Box(root, "Parking Lot", Map(735f, 815f) + new Vector3(0f, 0.015f, 0f), new Vector3(16f, 0.03f, 10f), "Gravel");
            Walkable(lot);
            Clear(Map(735f, 815f), 18f, 12f);
            Box(root, "Pickup (abandoned)", Map(705f, 820f) + new Vector3(0f, 0.8f, 0f), new Vector3(2f, 1.6f, 4.8f), "Metal");
        }

        /// <summary>The maintenance shed (fuse cabinet, fuel, falling bucket) and the generator behind it.</summary>
        private static void BuildMaintenanceShed(Transform root)
        {
            var at = Map(1010f, 575f);
            var shed = Building(root, "Maintenance Shed", at, new Vector3(9f, 3.5f, 6f),
                new[] { new Door { Side = Side.West, Offset = 0f, Width = 2.4f, Height = 2.6f } }, "Siding", "Roof");

            var props = Group(shed, "Props");
            Box(props, "Fuse Cabinet (left side)", at + new Vector3(-2.5f, 0.9f, 2.6f), new Vector3(1.2f, 1.8f, 0.5f), "Metal");
            Box(props, "Workbench", at + new Vector3(1.5f, 0.45f, 2.5f), new Vector3(3f, 0.9f, 0.8f), "Wood");
            Box(props, "Shelf (bucket falls from here)", at + new Vector3(3.9f, 1.2f, -1f), new Vector3(0.5f, 2.4f, 2.5f), "Wood");
            Box(props, "Bucket", at + new Vector3(3.8f, 2.0f, -0.6f), new Vector3(0.3f, 0.32f, 0.3f), "Metal");
            Box(props, "Fuel Can", at + new Vector3(-3.6f, 0.25f, -2.3f), new Vector3(0.35f, 0.5f, 0.2f), "Red");
            Box(props, "Breaker Panel", at + new Vector3(4.38f, 1.4f, 1.5f), new Vector3(0.1f, 0.8f, 0.6f), "Metal", collider: false);

            var generator = Group(root, "Generator");
            var genAt = at + new Vector3(0f, 0f, -4.6f);
            Box(generator, "Generator Body", genAt + new Vector3(0f, 0.55f, 0f), new Vector3(1.8f, 1.1f, 1f), "Green");
            Box(generator, "Fuel Feed", genAt + new Vector3(-0.95f, 0.85f, 0f), new Vector3(0.12f, 0.15f, 0.12f), "Metal", collider: false);
            Box(generator, "Starter Pull", genAt + new Vector3(0.95f, 0.7f, 0.2f), new Vector3(0.1f, 0.1f, 0.1f), "Metal", collider: false);
            Box(generator, "Floodlight Pole", genAt + new Vector3(2.5f, 2.5f, 0f), new Vector3(0.15f, 5f, 0.15f), "Metal");
            Clear(genAt, 7f, 5f);

            var yard = Box(root, "Shed Yard", at + new Vector3(-6f, 0.015f, -2f), new Vector3(6f, 0.03f, 10f), "Gravel");
            Walkable(yard);
        }

        /// <summary>
        /// Cabin Row A, cabins 1–6. Cabin 6 is enterable and axis-aligned with its east wall on a
        /// lattice line, so its pried-open door (z 26–27, 1 × 2 m) can be boarded with build pieces.
        /// </summary>
        private static void BuildCabinRowA(Transform root)
        {
            var row = Group(root, "Cabin Row A");

            // Cabin 6: east wall's outer face at x −3.0, door centred on z 26.5.
            Building(row, "Cabin 6", new Vector3(-6f, 0f, 26.5f), new Vector3(6f, 3f, 4.5f),
                new[] { new Door { Side = Side.East, Offset = 0f, Width = 1f, Height = 2f } }, "Siding", "Roof");
            Box(row, "Cabin 6 Number", new Vector3(-2.97f, 2.4f, 25.6f), new Vector3(0.02f, 0.3f, 0.3f), "Paper", collider: false);
            Label(row, "Cabin 6 Number Text", "6", new Vector3(-2.955f, 2.4f, 25.6f), 0.3f, 0.25f, Color.black, Quaternion.Euler(0f, -90f, 0f));
            Box(row, "Cabin 6 Bunk", new Vector3(-8f, 0.5f, 28f), new Vector3(1.9f, 1f, 0.9f), "DarkWood");
            Box(row, "Cabin 6 Shelf", new Vector3(-8.9f, 1.3f, 25f), new Vector3(0.3f, 1.2f, 1f), "Wood");
            Clear(new Vector3(-6f, 0f, 26.5f), 9f, 8f);

            // The others are closed up (one, further down, is locked); facing the path.
            var cabins = new (string name, float px, float py, float yaw)[]
            {
                ("Cabin 5", 320f, 585f, 30f), ("Cabin 4", 410f, 630f, 20f),
                ("Cabin 3", 260f, 545f, 40f), ("Cabin 2", 225f, 490f, 55f), ("Cabin 1 (locked)", 190f, 430f, 70f),
            };
            foreach (var (name, px, py, yaw) in cabins)
            {
                var at = Map(px, py);
                var turn = Quaternion.Euler(0f, yaw, 0f);
                var cabin = Group(row, name);

                var body = Box(cabin, "Body", at + new Vector3(0f, 1.5f, 0f), new Vector3(6f, 3f, 4.5f), "Siding");
                var roof = Box(cabin, "Roof", at + new Vector3(0f, 3.15f, 0f), new Vector3(6.6f, 0.3f, 5.1f), "Roof");
                var door = Box(cabin, "Door", at + turn * new Vector3(3.01f, 1f, 0f), new Vector3(0.05f, 2f, 1f), "DarkWood", collider: false);
                foreach (var part in new[] { body, roof, door }) part.transform.rotation = turn;

                Blocker(body);
                Clear(at, 9f, 8f);
            }
        }

        /// <summary>Basketball court, recreation field, dining hall and fire pit.</summary>
        private static void BuildCentre(Transform root)
        {
            var centre = Group(root, "Camp Centre");

            var courtAt = Map(490f, 470f);
            var court = Box(centre, "Basketball Court (working lights)", courtAt + new Vector3(0f, 0.02f, 0f), new Vector3(15f, 0.04f, 14f), "Concrete");
            Walkable(court);
            Box(centre, "Hoop Pole North", courtAt + new Vector3(0f, 1.6f, 6.6f), new Vector3(0.12f, 3.2f, 0.12f), "Metal");
            Box(centre, "Hoop North", courtAt + new Vector3(0f, 3.05f, 6.2f), new Vector3(1.2f, 0.8f, 0.05f), "Paper");
            Box(centre, "Hoop Pole South", courtAt + new Vector3(0f, 1.6f, -6.6f), new Vector3(0.12f, 3.2f, 0.12f), "Metal");
            Box(centre, "Hoop South", courtAt + new Vector3(0f, 3.05f, -6.2f), new Vector3(1.2f, 0.8f, 0.05f), "Paper");
            Box(centre, "Basketball (flat)", courtAt + new Vector3(2f, 0.12f, -3f), new Vector3(0.24f, 0.22f, 0.24f), "Orange");
            Clear(courtAt, 18f, 17f);

            var fieldAt = Map(440f, 360f);
            Box(centre, "Recreation Field", fieldAt + new Vector3(0f, 0.01f, 0f), new Vector3(30f, 0.02f, 16f), "Field", collider: false);
            Clear(fieldAt, 32f, 18f);

            var hallAt = Map(800f, 430f);
            var hall = Box(centre, "Dining Hall", hallAt + new Vector3(0f, 2.5f, 0f), new Vector3(24f, 5f, 12f), "Siding");
            Blocker(hall);
            Box(centre, "Dining Hall Roof", hallAt + new Vector3(0f, 5.4f, 0f), new Vector3(25f, 0.8f, 13f), "Roof");
            Box(centre, "Dining Hall Doors (latch to check)", hallAt + new Vector3(0f, 1.1f, -6.03f), new Vector3(2.2f, 2.2f, 0.06f), "DarkWood", collider: false);
            Box(centre, "Dining Hall Latch", hallAt + new Vector3(0.6f, 1.1f, -6.08f), new Vector3(0.15f, 0.08f, 0.04f), "Metal", collider: false);
            Box(centre, "Rec Hall Sign (buzzes)", hallAt + new Vector3(-7f, 4.2f, -6.05f), new Vector3(4f, 0.6f, 0.06f), "Paper", collider: false);
            Clear(hallAt, 28f, 16f);

            var pitAt = Map(790f, 315f);
            Box(centre, "Fire Pit Ring", pitAt + new Vector3(0f, 0.15f, 0f), new Vector3(2.2f, 0.3f, 2.2f), "Rock");
            for (int i = 0; i < 4; i++)
            {
                var log = Box(centre, $"Log Bench {i + 1}", pitAt + Quaternion.Euler(0f, i * 90f + 45f, 0f) * new Vector3(0f, 0.22f, 3.2f), new Vector3(2.4f, 0.44f, 0.5f), "DarkWood");
                log.transform.rotation = Quaternion.Euler(0f, i * 90f + 45f, 0f);
            }
            Clear(pitAt, 10f, 10f);
        }

        /// <summary>The lake, beach, boat dock, fishing kiosk and overlook rail, and the lifeguard tower (climbable).</summary>
        private static void BuildLakeside(Transform root)
        {
            var lake = Group(root, "Lakeside");

            // The water sits just over the meadow and stops the teleport arc: nobody lands on the lake.
            var waterAt = new Vector3(28.2f, 0f, LakeSouth + LakeAcross * 0.5f);
            Blocker(Box(lake, "Ashwood Lake", waterAt + new Vector3(0f, 0.03f, 0f), new Vector3(100f, 0.06f, LakeAcross), "Water"));
            Box(lake, "Lake Shore (camp)", new Vector3(28.2f, 0.02f, LakeSouth - 0.6f), new Vector3(100f, 0.04f, 1.2f), "Mud", collider: false);
            Box(lake, "Lake Shore (far)", new Vector3(28.2f, 0.02f, LakeNorth + 0.6f), new Vector3(100f, 0.04f, 1.2f), "Mud", collider: false);
            Clear(waterAt, 104f, LakeAcross + 4f);

            var beachAt = Map(600f, 230f);
            // Just above the water where the two meet, so it can be walked to the edge.
            var beach = Box(lake, "Beach", beachAt + new Vector3(0f, 0.04f, 0f), new Vector3(26f, 0.08f, 10f), "Sand");
            Walkable(beach);
            Clear(beachAt, 28f, 12f);

            // Runs on under the moored ferry's deck, so there's no lake to step over.
            var dock = Box(lake, "Boat Dock (ferry landing)", new Vector3(FerryX, 0.25f, LakeSouth + 5.95f), new Vector3(3f, 0.3f, 12.7f), "Wood");
            Walkable(dock);
            Clear(new Vector3(FerryX, 0f, LakeSouth - 2f), 8f, 6f);

            var overlookAt = Map(915f, 200f);
            var deck = Box(lake, "Overlook Deck", overlookAt + new Vector3(0f, 0.1f, 0f), new Vector3(6f, 0.2f, 4f), "Wood");
            Walkable(deck);
            Box(lake, "Overlook Rail", overlookAt + new Vector3(0f, 0.55f, 2f), new Vector3(6f, 0.9f, 0.08f), "DarkWood");
            Box(lake, "Fishing Kiosk", overlookAt + new Vector3(-3.6f, 1.2f, -1f), new Vector3(1.2f, 2.4f, 1.2f), "Wood");
            Box(lake, "Fishing Rod (optional)", overlookAt + new Vector3(-2.8f, 1f, -0.4f), new Vector3(0.03f, 1.8f, 0.03f), "DarkWood", collider: false);
            Clear(overlookAt, 10f, 7f);

            // Lifeguard tower: out of service, but its ladder climbs.
            var towerAt = Map(480f, 165f);
            var tower = Group(lake, "Lifeguard Tower (out of service)");
            foreach (var corner in new[] { new Vector3(-0.9f, 0f, -0.9f), new Vector3(0.9f, 0f, -0.9f), new Vector3(-0.9f, 0f, 0.9f), new Vector3(0.9f, 0f, 0.9f) })
                Box(tower, "Leg", towerAt + corner + new Vector3(0f, 1.5f, 0f), new Vector3(0.15f, 3f, 0.15f), "DarkWood");
            var platform = Box(tower, "Platform", towerAt + new Vector3(0f, 3.05f, 0f), new Vector3(2.2f, 0.1f, 2.2f), "Wood");
            Walkable(platform);
            Box(tower, "Rail", towerAt + new Vector3(0f, 3.6f, 1.05f), new Vector3(2.2f, 1f, 0.06f), "Wood");
            var ladder = Group(tower, "Ladder (climbable)");
            ladder.gameObject.AddComponent<CampfireFriends.Climbing.Climbable>();
            Box(ladder, "Ladder Rail West", towerAt + new Vector3(-0.35f, 1.6f, -1.25f), new Vector3(0.07f, 3.2f, 0.07f), "DarkWood");
            Box(ladder, "Ladder Rail East", towerAt + new Vector3(0.35f, 1.6f, -1.25f), new Vector3(0.07f, 3.2f, 0.07f), "DarkWood");
            for (int i = 1; i <= 10; i++)
                Box(ladder, $"Rung {i}", towerAt + new Vector3(0f, i * 0.3f, -1.25f), new Vector3(0.7f, 0.045f, 0.06f), "Wood");
            Clear(towerAt, 6f, 6f);

            // It stands in the shallows: a sandbar out from the beach to walk to its ladder.
            float barSouth = beachAt.z + 4f, barNorth = towerAt.z + 2.2f;
            var sandbar = Box(lake, "Sandbar (to the tower)", new Vector3(towerAt.x, 0.04f, (barSouth + barNorth) * 0.5f), new Vector3(4.5f, 0.08f, barNorth - barSouth), "Sand");
            Walkable(sandbar);
        }

        /// <summary>Everything off Night 1's route, readable from a distance: Cabin Row B, the camp store, staff cabins, the archery range and the blocked old-site trail.</summary>
        private static void BuildBackground(Transform root)
        {
            var far = Group(root, "Off Route");

            var rowB = new (string name, float px, float py)[]
            {
                ("Cabin 7", 1080f, 345f), ("Cabin 8", 1110f, 395f), ("Cabin 9", 1170f, 430f),
                ("Cabin 10", 1235f, 465f), ("Cabin 11", 1290f, 510f), ("Cabin 12", 1325f, 570f),
            };
            foreach (var (name, px, py) in rowB)
            {
                var cabin = Box(far, name, Map(px, py) + new Vector3(0f, 1.5f, 0f), new Vector3(6f, 3f, 4.5f), "Siding");
                cabin.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
                Blocker(cabin);
                Clear(Map(px, py), 9f, 8f);
            }

            foreach (var (name, px, py, size) in new[]
            {
                ("Camp Store (closed)", 1200f, 690f, new Vector3(8f, 3.5f, 6f)),
                ("Staff Cabin A (off limits)", 1140f, 840f, new Vector3(6f, 3f, 5f)),
                ("Staff Cabin B (off limits)", 1205f, 800f, new Vector3(6f, 3f, 5f)),
            })
            {
                var building = Box(far, name, Map(px, py) + new Vector3(0f, size.y * 0.5f, 0f), size, "Siding");
                Blocker(building);
                Clear(Map(px, py), size.x + 3f, size.z + 3f);
            }

            var archeryAt = Map(1180f, 230f);
            Box(far, "Archery Range (closed)", archeryAt + new Vector3(0f, 0.6f, 0f), new Vector3(1.2f, 1.2f, 0.2f), "Paper");
            Clear(archeryAt, 12f, 10f);

            // The old-site spur: a fallen tree and a work-order barrier, not an invisible wall.
            var trailAt = Map(1110f, 75f);
            Box(far, "Fallen Tree (old service road)", trailAt + new Vector3(0f, 0.4f, 0f), new Vector3(7f, 0.8f, 0.8f), "DarkWood");
            Box(far, "Barrier", trailAt + new Vector3(0f, 0.6f, -1.5f), new Vector3(3f, 1.2f, 0.1f), "Orange");
        }

        /// <summary>Worn dirt paths for the Night 1 horseshoe, plus the walk to the lake.</summary>
        private static void BuildPaths(Transform root)
        {
            var paths = Group(root, "Paths");
            var route = new[]
            {
                // gate → lot → office
                (new Vector3(0f, 0f, 2f), Map(700f, 830f)), (Map(700f, 830f), Map(780f, 790f)),
                // office → central path → shed
                (Map(735f, 790f), Map(735f, 640f)), (Map(735f, 640f), Map(960f, 600f)),
                // central path → Cabin 6 → along Row A
                (Map(735f, 700f), new Vector3(-2.5f, 0f, 26.5f)), (new Vector3(-6f, 0f, 22f), Map(410f, 610f)),
                (Map(410f, 610f), Map(320f, 565f)), (Map(320f, 565f), Map(255f, 520f)), (Map(255f, 520f), Map(215f, 455f)),
                // Row A → court → dining hall
                (Map(410f, 610f), Map(490f, 530f)), (Map(560f, 470f), Map(735f, 480f)), (Map(735f, 640f), Map(735f, 480f)),
                // dining hall → fire pit → overlook and beach
                (Map(790f, 360f), Map(790f, 330f)), (Map(800f, 300f), Map(900f, 215f)), (Map(780f, 300f), Map(620f, 240f)),
                // overlook → back toward the office along the east side
                (Map(900f, 215f), Map(960f, 380f)), (Map(960f, 380f), Map(960f, 600f)),
            };

            int n = 0;
            foreach (var (from, to) in route)
            {
                var mid = (from + to) * 0.5f;
                var length = Vector3.Distance(from, to);
                var path = Box(paths, $"Path {++n}", mid + new Vector3(0f, 0.01f, 0f), new Vector3(2.2f, 0.02f, length + 1.5f), "Dirt", collider: false);
                path.transform.rotation = Quaternion.LookRotation(to - from, Vector3.up);
                ClearAlong(from, to, 5f);
            }
        }

        /// <summary>
        /// Yard lamps on the security circuit and the court's own lights. Saved switched off: the
        /// generator and breaker bring them on in Night 1.
        /// </summary>
        private static void BuildYardLamps(Transform root)
        {
            var security = Group(root, "Yard Lamps (security circuit)");
            foreach (var at in new[]
            {
                Map(700f, 800f), Map(760f, 760f), Map(735f, 680f), Map(840f, 625f), Map(940f, 610f),
                Map(640f, 690f), new Vector3(-1f, 0f, 23.5f), Map(410f, 600f), Map(320f, 555f), Map(255f, 505f),
            })
                Lamp(security, at);

            var court = Group(root, "Court Lights (recreation circuit)");
            var courtAt = Map(490f, 470f);
            foreach (var corner in new[] { new Vector3(-8f, 0f, -7.5f), new Vector3(8f, 0f, 7.5f) })
                Lamp(court, courtAt + corner, 6f);
        }

        /// <summary>Woods everywhere the camp isn't: trees from SimpleNaturePack, denser toward the edges.</summary>
        private static void BuildWoods(Transform root)
        {
            var trees = AssetDatabase.FindAssets("t:Prefab Tree_", new[] { TreesFolder })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>).Where(p => p != null).ToArray();
            var undergrowth = AssetDatabase.FindAssets("t:Prefab", new[] { TreesFolder })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileName(p).StartsWith("Bush_") || Path.GetFileName(p).StartsWith("Rock_") || Path.GetFileName(p).StartsWith("Stump_"))
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).Where(p => p != null).ToArray();
            if (trees.Length == 0)
            {
                Debug.LogWarning($"[Camp] No Tree_ prefabs in {TreesFolder}; the woods are left out.");
                return;
            }

            var woods = Group(root, "Woods");
            var random = new System.Random(1984);
            int placed = 0;
            for (int attempt = 0; attempt < 9000 && placed < 560; attempt++)
            {
                var at = new Vector3(Mathf.Lerp(-95f, 115f, (float)random.NextDouble()), 0f, Mathf.Lerp(-4f, 235f, (float)random.NextDouble()));
                if (Clearings.Any(c => c.Contains(new Vector2(at.x, at.z)))) continue;

                // Thinner near the middle of camp, dense at the edges.
                float edge = Mathf.Max(Mathf.InverseLerp(40f, 90f, Mathf.Abs(at.x - 10f)), at.z > LakeNorth ? 1f : 0f);
                if (random.NextDouble() > 0.35 + 0.65 * edge) continue;

                var prefab = trees[random.Next(trees.Length)];
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, woods);
                tree.transform.position = at;
                tree.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                tree.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.4f, (float)random.NextDouble());
                placed++;

                if (undergrowth.Length > 0 && random.NextDouble() < 0.3)
                {
                    var bush = (GameObject)PrefabUtility.InstantiatePrefab(undergrowth[random.Next(undergrowth.Length)], woods);
                    bush.transform.position = at + new Vector3((float)random.NextDouble() * 3f - 1.5f, 0f, (float)random.NextDouble() * 3f - 1.5f);
                    bush.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                }
            }
            Debug.Log($"[Camp] Woods: {placed} trees.");
        }

        // ---------------------------------------------------------------- arrival by ferry

        private static readonly Vector3 CampfireSeat = new Vector3(22f, 0f, LakeNorth + 5f);

        private sealed class FarShore
        {
            public GameObject Sign;
            public PlankButton[] Planks;
            public TMP_Text Note;
        }

        /// <summary>
        /// Night 1's opening, where the ferry lands: a chained gate across the landward end of the
        /// dock, the CAMP ASHWOOD arch over it and the demolition notice.
        /// </summary>
        private static void BuildDockArrival(Transform root)
        {
            var arrival = Group(root, "Dock Arrival (Night 1 opening)");
            float z = LakeSouth - 0.3f;

            Box(arrival, "Dock Gate Post West", new Vector3(FerryX - 1.8f, 1.1f, z), new Vector3(0.25f, 2.2f, 0.25f), "DarkWood");
            Box(arrival, "Dock Gate Post East", new Vector3(FerryX + 1.8f, 1.1f, z), new Vector3(0.25f, 2.2f, 0.25f), "DarkWood");
            var gate = Group(arrival, "Dock Gate");
            Box(gate, "Gate Leaf West", new Vector3(FerryX - 0.85f, 0.8f, z), new Vector3(1.6f, 1.2f, 0.08f), "Wood");
            Box(gate, "Gate Leaf East", new Vector3(FerryX + 0.85f, 0.8f, z), new Vector3(1.6f, 1.2f, 0.08f), "Wood");
            Box(gate, "Chain", new Vector3(FerryX, 1.0f, z + 0.06f), new Vector3(0.45f, 0.06f, 0.04f), "Metal", collider: false);

            Box(arrival, "Arch Post West", new Vector3(FerryX - 2.2f, 2.2f, z - 0.4f), new Vector3(0.22f, 4.4f, 0.22f), "DarkWood");
            Box(arrival, "Arch Post East", new Vector3(FerryX + 2.2f, 2.2f, z - 0.4f), new Vector3(0.22f, 4.4f, 0.22f), "DarkWood");
            Box(arrival, "Arch Sign", new Vector3(FerryX, 3.9f, z - 0.4f), new Vector3(4.8f, 0.8f, 0.1f), "Wood");
            // Read from the ferry, looking south at the camp.
            Label(arrival, "Arch Sign Text", "CAMP ASHW OD", new Vector3(FerryX, 3.9f, z - 0.34f), 4.4f, 0.5f, new Color(0.85f, 0.8f, 0.65f), Quaternion.Euler(0f, 180f, 0f));

            Box(arrival, "Demolition Notice Post", new Vector3(FerryX + 2.8f, 0.75f, z - 0.2f), new Vector3(0.12f, 1.5f, 0.12f), "DarkWood");
            Box(arrival, "Demolition Notice", new Vector3(FerryX + 2.8f, 1.35f, z - 0.13f), new Vector3(0.6f, 0.45f, 0.02f), "Paper");
            Label(arrival, "Demolition Notice Text", "PRIVATE PROPERTY\nDEMOLITION PENDING", new Vector3(FerryX + 2.8f, 1.35f, z - 0.11f), 0.55f, 0.06f, new Color(0.6f, 0.1f, 0.08f), Quaternion.Euler(0f, 180f, 0f));

            // The walk from the landing up past the fire pit toward the office.
            var path = Box(arrival, "Path (landing to fire pit)", Vector3.Lerp(new Vector3(FerryX, 0f, z - 1f), Map(790f, 315f), 0.5f) + new Vector3(0f, 0.01f, 0f),
                new Vector3(2.2f, 0.02f, Vector3.Distance(new Vector3(FerryX, 0f, z - 1f), Map(790f, 315f)) + 1f), "Dirt", collider: false);
            path.transform.rotation = Quaternion.LookRotation(Map(790f, 315f) - new Vector3(FerryX, 0f, z - 1f));
            ClearAlong(new Vector3(FerryX, 0f, z - 1f), Map(790f, 315f), 5f);
            Clear(new Vector3(FerryX, 0f, z - 1f), 9f, 4f);
        }

        /// <summary>
        /// Where the game begins (the main menu concept): a campfire on the far shore at night, two
        /// benches, a lamp-lit signpost whose planks are the menu, and the ferry landing.
        /// </summary>
        private static FarShore BuildFarShore(Transform root)
        {
            var shore = Group(root, "Far Shore (main menu)");
            var fire = CampfireSeat + new Vector3(0.5f, 0f, -2.2f); // in front, the lake beyond it

            Box(shore, "Fire Ring", fire + new Vector3(0f, 0.12f, 0f), new Vector3(1.4f, 0.24f, 1.4f), "Rock");
            Box(shore, "Embers", fire + new Vector3(0f, 0.26f, 0f), new Vector3(0.9f, 0.08f, 0.9f), "Ember", collider: false);
            for (int i = 0; i < 3; i++)
            {
                var log = Box(shore, $"Burning Log {i + 1}", fire + new Vector3(0f, 0.36f, 0f), new Vector3(0.9f, 0.14f, 0.14f), "DarkWood", collider: false);
                log.transform.rotation = Quaternion.Euler(0f, i * 60f, 12f);
            }
            var fireLight = new GameObject("Fire Light", typeof(Light), typeof(CampfireFriends.Arrival.FireFlicker)).GetComponent<Light>();
            fireLight.transform.SetParent(shore, false);
            fireLight.transform.position = fire + new Vector3(0f, 0.8f, 0f);
            fireLight.type = LightType.Point;
            fireLight.range = 9f;
            fireLight.intensity = 2.4f;
            fireLight.color = new Color(1f, 0.55f, 0.25f);
            fireLight.shadows = LightShadows.Soft;

            Box(shore, "Bench (your seat)", CampfireSeat + new Vector3(0f, 0.22f, 0.3f), new Vector3(2.4f, 0.44f, 0.5f), "DarkWood");
            Box(shore, "Bench (across the fire)", fire + new Vector3(2.4f, 0.22f, 0.4f), new Vector3(0.5f, 0.44f, 2.4f), "DarkWood");
            Box(shore, "Mug", fire + new Vector3(2.4f, 0.5f, 0.9f), new Vector3(0.09f, 0.1f, 0.09f), "Paper", collider: false);
            Box(shore, "Backpack", CampfireSeat + new Vector3(-1.4f, 0.25f, 0.4f), new Vector3(0.4f, 0.5f, 0.3f), "Red");

            var ground = Box(shore, "Campsite", fire + new Vector3(-1f, 0.01f, 0f), new Vector3(12f, 0.02f, 10f), "Dirt");
            Walkable(ground);
            Clear(fire + new Vector3(-1f, 0f, 0f), 16f, 14f);

            // The signpost menu, front-left of the seat (facing south, left is east) and turned toward it.
            var signAt = CampfireSeat + new Vector3(1.9f, 0f, -1.5f);
            var sign = Group(shore, "Signpost Menu");
            sign.position = signAt;
            sign.rotation = Quaternion.LookRotation(signAt - (CampfireSeat + Vector3.up * 1.2f) - Vector3.up * (signAt.y - 1.2f), Vector3.up);
            sign.rotation = Quaternion.Euler(0f, sign.rotation.eulerAngles.y, 0f);

            LocalBox(sign, "Post", new Vector3(-0.75f, 1.1f, 0.08f), new Vector3(0.14f, 2.2f, 0.14f), "DarkWood");
            LocalBox(sign, "Title Board", new Vector3(0f, 2.0f, 0f), new Vector3(1.4f, 0.32f, 0.06f), "Wood");
            LocalLabel(sign, "Title", "CAMP ASHWOOD", new Vector3(0f, 2.0f, -0.035f), 1.3f, 0.2f, new Color(0.92f, 0.88f, 0.78f));

            var planks = new PlankButton[4];
            for (int i = 0; i < planks.Length; i++)
                planks[i] = Plank(sign, $"Plank {i + 1}", new Vector3(i == 0 ? 0f : -0.05f, 1.62f - i * 0.25f, 0f), new Vector3(i == 0 ? 1.15f : 1f, 0.2f, 0.05f), "PLANK");

            LocalBox(sign, "Note Board", new Vector3(0f, 0.55f, 0f), new Vector3(1.2f, 0.32f, 0.03f), "Paper", collider: false);
            var note = LocalLabel(sign, "Note", "", new Vector3(0f, 0.55f, -0.02f), 1.12f, 0.06f, new Color(0.15f, 0.12f, 0.1f));
            note.textWrappingMode = TextWrappingModes.Normal;

            var lampHead = LocalBox(sign, "Lamp", new Vector3(-0.6f, 2.35f, -0.15f), new Vector3(0.18f, 0.14f, 0.18f), "Ember", collider: false);
            var lamp = new GameObject("Lamp Light", typeof(Light)).GetComponent<Light>();
            lamp.transform.SetParent(sign, false);
            lamp.transform.localPosition = new Vector3(-0.6f, 2.2f, -0.3f);
            lamp.type = LightType.Point;
            lamp.range = 4f;
            lamp.intensity = 1.6f;
            lamp.color = new Color(1f, 0.75f, 0.45f);

            // The landing the ferry leaves from.
            var landing = Box(shore, "Ferry Landing (far shore)", new Vector3(FerryX, 0.25f, LakeNorth - 5.15f), new Vector3(3f, 0.3f, 10.7f), "Wood");
            Walkable(landing);
            var landingPath = Box(shore, "Path (campfire to landing)", Vector3.Lerp(CampfireSeat, new Vector3(FerryX, 0f, LakeNorth), 0.5f) + new Vector3(0f, 0.012f, 0f),
                new Vector3(1.8f, 0.02f, Vector3.Distance(CampfireSeat, new Vector3(FerryX, 0f, LakeNorth)) + 1f), "Dirt");
            landingPath.transform.rotation = Quaternion.LookRotation(new Vector3(FerryX, 0f, LakeNorth) - CampfireSeat);
            Walkable(landingPath);
            ClearAlong(CampfireSeat, new Vector3(FerryX, 0f, LakeNorth), 6f);

            return new FarShore { Sign = sign.gameObject, Planks = planks, Note = note };
        }

        /// <summary>
        /// The ferry: a hull, a walkable deck, rails, a lantern and a bell (a plank to ring), with
        /// its two moorings. It's a scene NetworkObject so a session shares it.
        /// </summary>
        private static CampfireFriends.Arrival.Ferry BuildFerry(Transform root)
        {
            var moorings = Group(root, "Ferry Moorings");
            var far = Group(moorings, "Ferry Stop (far shore)");
            far.SetPositionAndRotation(new Vector3(FerryX, 0f, LakeNorth - 13.3f), Quaternion.Euler(0f, 180f, 0f));
            var camp = Group(moorings, "Ferry Stop (camp dock)");
            camp.SetPositionAndRotation(new Vector3(FerryX, 0f, LakeSouth + 15.1f), Quaternion.Euler(0f, 180f, 0f));
            var arrivalPoint = Group(moorings, "Arrival Point (missed the boat)");
            arrivalPoint.position = new Vector3(FerryX, 0.4f, LakeSouth + 2f);

            var ferryGo = new GameObject("Ferry", typeof(Fusion.NetworkObject), typeof(CampfireFriends.Arrival.Ferry), typeof(CampfireFriends.Network.NetworkFerry));
            ferryGo.transform.SetParent(root, false);
            ferryGo.transform.SetPositionAndRotation(far.position, far.rotation);
            var ferry = ferryGo.transform;

            LocalBox(ferry, "Hull", new Vector3(0f, 0.05f, 0f), new Vector3(3.2f, 0.6f, 7f), "Green", collider: false);
            var deck = LocalBox(ferry, "Deck", new Vector3(0f, 0.35f, 0f), new Vector3(3f, 0.1f, 6.6f), "Wood");
            Walkable(deck);
            Blocker(LocalBox(ferry, "Rail Port", new Vector3(-1.5f, 0.8f, 0f), new Vector3(0.06f, 0.8f, 6.6f), "DarkWood"));
            Blocker(LocalBox(ferry, "Rail Starboard", new Vector3(1.5f, 0.8f, 0f), new Vector3(0.06f, 0.8f, 6.6f), "DarkWood"));
            Blocker(LocalBox(ferry, "Rail Bow", new Vector3(0f, 0.8f, 3.3f), new Vector3(3f, 0.8f, 0.06f), "DarkWood"));
            LocalBox(ferry, "Canopy Post Port", new Vector3(-1.4f, 1.4f, -2.9f), new Vector3(0.08f, 2f, 0.08f), "DarkWood");
            LocalBox(ferry, "Canopy Post Starboard", new Vector3(1.4f, 1.4f, -2.9f), new Vector3(0.08f, 2f, 0.08f), "DarkWood");
            LocalBox(ferry, "Canopy", new Vector3(0f, 2.42f, -2.4f), new Vector3(3f, 0.06f, 1.4f), "Roof", collider: false);

            var lantern = new GameObject("Lantern", typeof(Light)).GetComponent<Light>();
            lantern.transform.SetParent(ferry, false);
            lantern.transform.localPosition = new Vector3(0f, 2.2f, -2.4f);
            lantern.type = LightType.Point;
            lantern.range = 7f;
            lantern.intensity = 1.4f;
            lantern.color = new Color(1f, 0.78f, 0.5f);

            LocalBox(ferry, "Bell Post", new Vector3(1.1f, 1.2f, 2.6f), new Vector3(0.08f, 1.6f, 0.08f), "DarkWood");
            LocalBox(ferry, "Bell", new Vector3(1.1f, 2.05f, 2.55f), new Vector3(0.22f, 0.24f, 0.22f), "Brass", collider: false);
            var bell = Plank(ferry, "Bell Plank", new Vector3(0.75f, 1.35f, 2.55f), new Vector3(0.9f, 0.18f, 0.05f), "RING THE BELL");

            var ferryComponent = ferryGo.GetComponent<CampfireFriends.Arrival.Ferry>();
            var so = new SerializedObject(ferryComponent);
            so.FindProperty("_farShore").objectReferenceValue = far;
            so.FindProperty("_campDock").objectReferenceValue = camp;
            so.FindProperty("_deck").objectReferenceValue = deck.GetComponent<Collider>();
            so.FindProperty("_arrivalPoint").objectReferenceValue = arrivalPoint;
            so.ApplyModifiedPropertiesWithoutUndo();
            var net = new SerializedObject(ferryGo.GetComponent<CampfireFriends.Network.NetworkFerry>());
            net.FindProperty("_ferry").objectReferenceValue = ferryComponent;
            net.ApplyModifiedPropertiesWithoutUndo();

            // The bell plank is found by WireArrival via this name.
            bell.name = "Bell Plank";
            Clear(new Vector3(FerryX, 0f, (LakeNorth + LakeSouth) * 0.5f), 8f, LakeAcross + 30f);
            return ferryComponent;
        }

        /// <summary>
        /// Connects the menu, the clock, the fade and the ferry: the menu shows at launch (the
        /// session menu waits for the left menu button), the clock holds at night until play
        /// begins, and the local player rides the ferry.
        /// </summary>
        private static void WireArrival(Scene scene, FarShore shore, CampfireFriends.Arrival.Ferry ferry)
        {
            var roots = scene.GetRootGameObjects();
            T[] All<T>() where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

            var clock = All<CampfireFriends.Shifts.LocalShiftClock>().FirstOrDefault();
            if (clock != null)
            {
                var clockSo = new SerializedObject(clock);
                clockSo.FindProperty("_startIn").enumValueIndex = (int)CampfireFriends.Shifts.ShiftPhase.NightWatch;
                clockSo.FindProperty("_holdUntilStarted").boolValue = true;
                clockSo.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var sessionMenu in All<CampfireFriends.Network.SessionMenu>())
            {
                var menuSo = new SerializedObject(sessionMenu);
                menuSo.FindProperty("_openAtStart").boolValue = false;
                menuSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var menu = shore.Sign.AddComponent<MainMenu>();
            var so = new SerializedObject(menu);
            var planks = so.FindProperty("_planks");
            planks.arraySize = shore.Planks.Length;
            for (int i = 0; i < shore.Planks.Length; i++) planks.GetArrayElementAtIndex(i).objectReferenceValue = shore.Planks[i];
            so.FindProperty("_note").objectReferenceValue = shore.Note;
            so.FindProperty("_session").objectReferenceValue = All<CampfireFriends.Network.NetworkSession>().FirstOrDefault();
            so.FindProperty("_clock").objectReferenceValue = clock;
            so.FindProperty("_ferry").objectReferenceValue = ferry;
            so.FindProperty("_bell").objectReferenceValue = ferry.GetComponentsInChildren<PlankButton>(true).FirstOrDefault(p => p.name == "Bell Plank");
            so.FindProperty("_sign").objectReferenceValue = shore.Sign;
            so.ApplyModifiedPropertiesWithoutUndo();

            // A fade on the eyes, for the move from the menu's night to the shift's afternoon.
            var eyes = All<Transform>().FirstOrDefault(t => t.name == "CenterEyeAnchor");
            if (eyes != null && eyes.GetComponent<OVRScreenFade>() == null)
            {
                var fade = eyes.gameObject.AddComponent<OVRScreenFade>();
                fade.fadeTime = 1.5f;
                fade.fadeOnStart = true;
            }

            var locomotor = All<PlayerLocomotor>().FirstOrDefault();
            if (locomotor != null)
            {
                var locomotorSo = new SerializedObject(locomotor);
                var rider = new GameObject("Ferry Rider", typeof(CampfireFriends.Arrival.FerryRider)).GetComponent<CampfireFriends.Arrival.FerryRider>();
                SceneManager.MoveGameObjectToScene(rider.gameObject, scene);
                var riderSo = new SerializedObject(rider);
                riderSo.FindProperty("_ferry").objectReferenceValue = ferry;
                riderSo.FindProperty("_locomotor").objectReferenceValue = locomotor;
                riderSo.FindProperty("_playerOrigin").objectReferenceValue = locomotorSo.FindProperty("_playerOrigin").objectReferenceValue;
                riderSo.FindProperty("_head").objectReferenceValue = locomotorSo.FindProperty("_playerHead").objectReferenceValue ?? eyes;
                riderSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>A pointable plank: a box with a label on its front, a ray interactable and a <see cref="PlankButton"/>.</summary>
        private static PlankButton Plank(Transform parent, string name, Vector3 localCentre, Vector3 size, string text)
        {
            // An unscaled group, so the label isn't stretched by the board's scale and both hide together.
            var group = Group(parent, name);
            group.localPosition = localCentre;
            group.localRotation = Quaternion.identity;
            var plank = LocalBox(group, "Board", Vector3.zero, size, "Wood");
            var label = LocalLabel(group, "Label", text, new Vector3(0f, 0f, -size.z * 0.5f - 0.004f), size.x * 0.9f, size.y * 0.55f, new Color(0.95f, 0.9f, 0.8f));

            var surface = plank.AddComponent<ColliderSurface>();
            var surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("_collider").objectReferenceValue = plank.GetComponent<Collider>();
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();

            var ray = plank.AddComponent<RayInteractable>();
            var raySo = new SerializedObject(ray);
            raySo.FindProperty("_surface").objectReferenceValue = surface;
            raySo.ApplyModifiedPropertiesWithoutUndo();

            var button = group.gameObject.AddComponent<PlankButton>();
            var buttonSo = new SerializedObject(button);
            buttonSo.FindProperty("_interactable").objectReferenceValue = ray;
            buttonSo.FindProperty("_plank").objectReferenceValue = plank.GetComponent<Renderer>();
            buttonSo.FindProperty("_label").objectReferenceValue = label;
            buttonSo.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        /// <summary>A box placed in its (unscaled) parent's space.</summary>
        private static GameObject LocalBox(Transform parent, string name, Vector3 localCentre, Vector3 size, string material, bool collider = true)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localCentre;
            box.transform.localRotation = Quaternion.identity;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = _materials[material];
            if (!collider) Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        /// <summary>Text in its parent's space, readable from the parent's −Z side.</summary>
        private static TextMeshPro LocalLabel(Transform parent, string name, string text, Vector3 localCentre, float width, float size, Color color)
        {
            var go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCentre;
            go.transform.localRotation = Quaternion.identity;
            var tmp = go.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size * 10f;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(width, size * 4f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        // ---------------------------------------------------------------- player

        /// <summary>Starts the players on the bench by the campfire, looking across the lake at the camp.</summary>
        private static void PlacePlayerAtCampfire(Scene scene)
        {
            var rig = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OVRCameraRig>(true)).FirstOrDefault();
            if (rig == null) return;
            rig.transform.SetPositionAndRotation(CampfireSeat, Quaternion.Euler(0f, 180f, 0f));
        }

        // ---------------------------------------------------------------- pieces

        /// <summary>
        /// A hollow building: floor, four walls (doors cut where asked), roof. Its outer footprint
        /// is <paramref name="size"/> centred on <paramref name="at"/> at ground level.
        /// </summary>
        private static Transform Building(Transform parent, string name, Vector3 at, Vector3 size, Door[] doors, string wallMaterial, string roofMaterial)
        {
            var building = Group(parent, name);
            const float thick = 0.2f;
            float w = size.x, h = size.y, d = size.z;

            var floor = Box(building, "Floor", at + new Vector3(0f, 0.05f, 0f), new Vector3(w - 2f * thick, 0.1f, d - 2f * thick), "Floorboards");
            Walkable(floor);

            Wall(building, "Wall North", at + new Vector3(0f, 0f, d * 0.5f - thick * 0.5f), w, h, thick, Vector3.right, doors.Where(x => x.Side == Side.North), wallMaterial);
            Wall(building, "Wall South", at + new Vector3(0f, 0f, -d * 0.5f + thick * 0.5f), w, h, thick, Vector3.right, doors.Where(x => x.Side == Side.South), wallMaterial);
            Wall(building, "Wall East", at + new Vector3(w * 0.5f - thick * 0.5f, 0f, 0f), d - 2f * thick, h, thick, Vector3.forward, doors.Where(x => x.Side == Side.East), wallMaterial);
            Wall(building, "Wall West", at + new Vector3(-w * 0.5f + thick * 0.5f, 0f, 0f), d - 2f * thick, h, thick, Vector3.forward, doors.Where(x => x.Side == Side.West), wallMaterial);

            var roof = Box(building, "Roof", at + new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.6f, 0.3f, d + 0.6f), roofMaterial);
            Blocker(roof);

            Clear(at, w + 4f, d + 4f);
            return building;
        }

        /// <summary>A straight wall along <paramref name="along"/>, split around its doors (door, wall either side, lintel above).</summary>
        private static void Wall(Transform parent, string name, Vector3 centre, float length, float height, float thick, Vector3 along, IEnumerable<Door> doors, string material)
        {
            var across = along == Vector3.right ? new Vector3(0f, 0f, thick) : new Vector3(thick, 0f, 0f);
            Vector3 Size(float l, float hh) => along * l + across + Vector3.up * hh;

            float start = -length * 0.5f;
            int segment = 0;
            foreach (var door in doors.OrderBy(x => x.Offset))
            {
                float doorStart = door.Offset - door.Width * 0.5f, doorEnd = door.Offset + door.Width * 0.5f;
                if (doorStart > start)
                    Blocker(Box(parent, $"{name} {++segment}", centre + along * ((start + doorStart) * 0.5f) + Vector3.up * height * 0.5f, Size(doorStart - start, height), material));
                Blocker(Box(parent, $"{name} Lintel {segment}", centre + along * door.Offset + Vector3.up * ((door.Height + height) * 0.5f), Size(door.Width, height - door.Height), material));
                start = doorEnd;
            }
            if (length * 0.5f > start)
                Blocker(Box(parent, $"{name} {++segment}", centre + along * ((start + length * 0.5f) * 0.5f) + Vector3.up * height * 0.5f, Size(length * 0.5f - start, height), material));
        }

        /// <summary>A lamp post with a light that starts off; its owner circuit switches it.</summary>
        private static void Lamp(Transform parent, Vector3 at, float height = 3.6f)
        {
            var lamp = Group(parent, "Lamp");
            lamp.position = at;
            Box(lamp, "Post", at + new Vector3(0f, height * 0.5f, 0f), new Vector3(0.12f, height, 0.12f), "Metal");
            Box(lamp, "Head", at + new Vector3(0f, height, 0.25f), new Vector3(0.3f, 0.12f, 0.45f), "Metal", collider: false);
            var light = new GameObject("Light", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(lamp, false);
            light.transform.position = at + new Vector3(0f, height - 0.15f, 0.3f);
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 2.2f;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.shadows = LightShadows.None;
            light.enabled = false;
        }

        private static GameObject Box(Transform parent, string name, Vector3 centre, Vector3 size, string material, bool collider = true)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, true);
            box.transform.position = centre;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = _materials[material];
            if (!collider) Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static void Label(Transform parent, string name, string text, Vector3 at, float width, float size, Color color, Quaternion? rotation = null)
        {
            var go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, rotation ?? Quaternion.identity);
            var tmp = go.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size * 10f;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(width, size * 3f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>Somewhere the player can teleport to.</summary>
        private static void Walkable(GameObject surface) => Teleport(surface, allow: true);

        /// <summary>Stops the teleport arc, so nobody teleports through walls.</summary>
        private static void Blocker(GameObject solid) => Teleport(solid, allow: false);

        private static void Teleport(GameObject target, bool allow)
        {
            var collider = target.GetComponent<Collider>();
            if (collider == null) return;

            var surface = target.AddComponent<ColliderSurface>();
            var surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("_collider").objectReferenceValue = collider;
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();

            var interactable = target.AddComponent<TeleportInteractable>();
            var so = new SerializedObject(interactable);
            so.FindProperty("_surface").objectReferenceValue = surface;
            so.FindProperty("_allowTeleport").boolValue = allow;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Clear(Vector3 centre, float width, float depth) =>
            Clearings.Add(new Rect(centre.x - width * 0.5f, centre.z - depth * 0.5f, width, depth));

        private static void ClearAlong(Vector3 from, Vector3 to, float width)
        {
            int steps = Mathf.CeilToInt(Vector3.Distance(from, to) / 3f);
            for (int i = 0; i <= steps; i++) Clear(Vector3.Lerp(from, to, i / (float)Mathf.Max(1, steps)), width, width);
        }

        private static void StaticBatch(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<Light>() != null) continue;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
        }

        // ---------------------------------------------------------------- materials

        private static Dictionary<string, Material> BuildMaterials()
        {
            EnsureFolder(MaterialsFolder);
            var colours = new Dictionary<string, Color>
            {
                ["Grass"] = new Color(0.22f, 0.3f, 0.17f),
                ["Field"] = new Color(0.28f, 0.36f, 0.2f),
                ["Dirt"] = new Color(0.36f, 0.29f, 0.2f),
                ["Gravel"] = new Color(0.42f, 0.4f, 0.37f),
                ["Asphalt"] = new Color(0.17f, 0.17f, 0.18f),
                ["Concrete"] = new Color(0.45f, 0.45f, 0.43f),
                ["Sand"] = new Color(0.62f, 0.56f, 0.42f),
                ["Mud"] = new Color(0.24f, 0.2f, 0.15f),
                ["Water"] = new Color(0.08f, 0.14f, 0.18f),
                ["Siding"] = new Color(0.45f, 0.38f, 0.3f),
                ["Floorboards"] = new Color(0.33f, 0.26f, 0.18f),
                ["Roof"] = new Color(0.2f, 0.27f, 0.24f),
                ["Wood"] = new Color(0.5f, 0.38f, 0.24f),
                ["DarkWood"] = new Color(0.27f, 0.19f, 0.12f),
                ["Metal"] = new Color(0.35f, 0.36f, 0.37f),
                ["Paper"] = new Color(0.82f, 0.79f, 0.7f),
                ["Rock"] = new Color(0.4f, 0.4f, 0.4f),
                ["Green"] = new Color(0.22f, 0.32f, 0.2f),
                ["Red"] = new Color(0.55f, 0.1f, 0.08f),
                ["Orange"] = new Color(0.75f, 0.35f, 0.1f),
                ["Brass"] = new Color(0.62f, 0.5f, 0.22f),
                ["Ember"] = new Color(1f, 0.45f, 0.12f),
            };

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var materials = new Dictionary<string, Material>();
            foreach (var (name, colour) in colours.Select(kv => (kv.Key, kv.Value)))
            {
                string path = $"{MaterialsFolder}/Camp_{name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = $"Camp_{name}", enableInstancing = true };
                    material.SetColor("_BaseColor", colour);
                    material.SetFloat("_Smoothness", name == "Water" ? 0.75f : 0.15f);
                    if (name == "Water")
                    {
                        // The sky reflection is captured in daylight and never darkens: off, so the lake goes dark at night.
                        material.SetFloat("_EnvironmentReflections", 0f);
                        material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                    }
                    if (name == "Ember")
                    {
                        // Glows in the dark: the fire and lamps read at night without lighting.
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", colour * 2.5f);
                    }
                    AssetDatabase.CreateAsset(material, path);
                }
                materials[name] = material;
            }
            return materials;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }

        private static void EnsureInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static void BuildBatch()
        {
            try
            {
                Build();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
