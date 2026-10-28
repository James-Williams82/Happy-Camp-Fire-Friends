using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampfireFriends.Building;
using CampfireFriends.Network;
using CampfireFriends.Supplies;
using Fusion;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CampfireFriends.Construction.Editor
{
    /// <summary>
    /// Generates the build kit: materials, the base piece prefab and one variant per piece, the
    /// piece definitions and catalog, the build rig prefab with Desktop and Quest variants, and the
    /// network piece proxy. Re-running regenerates every asset in place (GUIDs are kept, so scene
    /// references survive). Scenes are never rebuilt once they exist: the Quest build rig is wired
    /// onto whatever Interaction SDK rig the scene already has.
    /// Batch: Unity -batchmode -projectPath . -executeMethod CampfireFriends.Construction.Editor.BuildKitBuilder.BuildAllBatch -quit
    /// </summary>
    public static class BuildKitBuilder
    {
        private const string Root = "Assets/Project Resources/Building";
        private const string Materials = Root + "/Materials";
        private const string Definitions = Root + "/Definitions";
        private const string PiecePrefabs = Root + "/Prefabs/Pieces";
        private const string RigPrefabs = Root + "/Prefabs/Rig";
        private const string CatalogPath = Root + "/PieceCatalog.asset";
        private const string ProxyPath = Root + "/PlacedPiece.prefab";
        private const string SandboxScenePath = "Assets/Scenes/BuildSandbox.unity";
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";

        // The authored world-space piece menu (Interaction SDK UI set). Its buttons become palette slots.
        private const string MenuPrefabPath = "Assets/UI/PRefabs/build-menu.prefab";

        // Interaction SDK: Runtime/Prefabs/Ray/ControllerRayInteractor.prefab
        private const string ControllerRayInteractorGuid = "44cf50d1a2e68b64791a3e3143ccda19";

        private sealed class Box
        {
            public Vector3 Centre;
            public Vector3 Size;
            public string Material;

            public Box(Vector3 centre, Vector3 size, string material = "Wood")
            {
                Centre = centre;
                Size = size;
                Material = material;
            }
        }

        private sealed class PieceSpec
        {
            public string Id;
            public string Name;
            public Vector3Int Footprint;
            public SupportRule Support;
            public int Boards;
            public Box[] Boxes;
        }

        /// <summary>Box filling the whole footprint, for simple pieces. Pivot is bottom-centre.</summary>
        private static Box Solid(Vector3Int footprint, string material = "Wood")
        {
            var size = (Vector3)footprint * BuildGrid.DefaultCellSize;
            return new Box(new Vector3(0f, size.y * 0.5f, 0f), size, material);
        }

        // The reference test kit from doc §6.2, plus the security-camp fence panel.
        private static PieceSpec[] Specs()
        {
            var block = new Vector3Int(2, 2, 2);
            var halfBlock = new Vector3Int(1, 1, 1);
            var wall = new Vector3Int(2, 4, 1);
            var floor = new Vector3Int(2, 1, 2);
            var roof = new Vector3Int(2, 1, 2);
            var beam = new Vector3Int(3, 1, 1);
            var fence = new Vector3Int(4, 3, 1);
            var pillar = new Vector3Int(1, 4, 1);

            return new[]
            {
                new PieceSpec { Id = "block", Name = "Block", Footprint = block, Support = SupportRule.Stack, Boards = 2, Boxes = new[] { Solid(block) } },
                new PieceSpec { Id = "half_block", Name = "Half Block", Footprint = halfBlock, Support = SupportRule.Stack, Boards = 1, Boxes = new[] { Solid(halfBlock, "LightWood") } },
                new PieceSpec { Id = "wall", Name = "Wall", Footprint = wall, Support = SupportRule.Stack, Boards = 3, Boxes = new[] { Solid(wall, "LightWood") } },
                new PieceSpec { Id = "floor", Name = "Floor", Footprint = floor, Support = SupportRule.Stack, Boards = 2, Boxes = new[] { Solid(floor, "DarkWood") } },
                new PieceSpec
                {
                    Id = "roof_panel", Name = "Roof Panel", Footprint = roof, Support = SupportRule.Roof, Boards = 4,
                    Boxes = new[] { new Box(new Vector3(0f, 0.1f, 0f), new Vector3(1f, 0.2f, 1f), "Roof") },
                },
                new PieceSpec
                {
                    Id = "beam", Name = "Beam", Footprint = beam, Support = SupportRule.SideAttach, Boards = 2,
                    Boxes = new[] { new Box(new Vector3(0f, 0.25f, 0f), new Vector3(1.5f, 0.3f, 0.3f), "DarkWood") },
                },
                new PieceSpec
                {
                    Id = "fence_panel", Name = "Fence Panel", Footprint = fence, Support = SupportRule.Ground, Boards = 2,
                    Boxes = new[]
                    {
                        new Box(new Vector3(0f, 0.75f, 0f), new Vector3(1.8f, 1.2f, 0.06f), "LightWood"),
                        new Box(new Vector3(-0.95f, 0.75f, 0f), new Vector3(0.1f, 1.5f, 0.1f), "DarkWood"),
                        new Box(new Vector3(0.95f, 0.75f, 0f), new Vector3(0.1f, 1.5f, 0.1f), "DarkWood"),
                    },
                },
                new PieceSpec { Id = "pillar", Name = "Pillar", Footprint = pillar, Support = SupportRule.Stack, Boards = 1, Boxes = new[] { Solid(pillar, "DarkWood") } },
            };
        }

        [MenuItem("Campfire Friends/Building/Rebuild Build Kit")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            BuildAssets();
            PrepareSandbox();
            UpgradeLobby();
            if (!Application.isBatchMode) EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            Debug.Log("[BuildKit] Done. BuildSandbox is open: press Play with the headset on.");
        }

        [MenuItem("Campfire Friends/Building/Wire Build Rig Into Open Scene")]
        public static void WireOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (WireRayRig(scene)) EditorSceneManager.SaveScene(scene);
        }

        public static void BuildAllBatch()
        {
            try
            {
                BuildAll();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Campfire Friends/Building/Rebuild Assets Only")]
        public static PieceCatalog BuildAssets()
        {
            foreach (var folder in new[] { Materials, Definitions, PiecePrefabs, RigPrefabs }) EnsureFolder(folder);

            var materials = BuildMaterials();
            var pieceBase = BuildPieceBase();

            var definitions = new List<PieceDefinition>();
            foreach (var spec in Specs())
            {
                var prefab = BuildPieceVariant(pieceBase, spec, materials);
                definitions.Add(BuildDefinition(spec, prefab));
            }

            var catalog = LoadOrCreate<PieceCatalog>(CatalogPath);
            catalog.CellSize = BuildGrid.DefaultCellSize;
            catalog.Pieces = definitions;
            EditorUtility.SetDirty(catalog);

            BuildProxy(catalog);
            var rigBase = BuildRigBase(materials);
            BuildRigVariant(rigBase, "BuildRig_Desktop", typeof(KeyboardMouseBuildInput), typeof(ScreenPointerAim));
            BuildRigVariant(rigBase, "BuildRig_Quest", typeof(RayInteractorBuildInput), typeof(RayInteractorAim));
            BuildPaletteMenuPrefab(catalog);

            AssetDatabase.SaveAssets();
            return catalog;
        }

        /// <summary>
        /// Opening a scene unloads assets nothing in it references, which turns a catalog held across
        /// the open into a destroyed object that serializes as None. Reload it after every open.
        /// </summary>
        private static PieceCatalog LoadCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<PieceCatalog>(CatalogPath)
                   ?? throw new InvalidOperationException($"{CatalogPath} is missing. Run Rebuild Assets Only.");
        }

        // ---------------------------------------------------------------- menu

        /// <summary>
        /// Puts a <see cref="BuildPaletteMenu"/> on the authored build menu prefab, its buttons in
        /// hierarchy order as the slots, so every instance and variant of the menu is a palette.
        /// </summary>
        private static void BuildPaletteMenuPrefab(PieceCatalog catalog)
        {
            if (!File.Exists(MenuPrefabPath)) return;

            var root = PrefabUtility.LoadPrefabContents(MenuPrefabPath);
            try
            {
                var menu = root.GetComponent<BuildPaletteMenu>();
                if (menu == null) menu = root.AddComponent<BuildPaletteMenu>();

                var so = new SerializedObject(menu);
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                var slots = so.FindProperty("_slots");
                var buttons = root.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                slots.arraySize = buttons.Length;
                for (int i = 0; i < buttons.Length; i++) slots.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
                if (buttons.Length < catalog.Count)
                    Debug.LogWarning($"[BuildKit] {MenuPrefabPath} has {buttons.Length} buttons for {catalog.Count} pieces; the rest are only reachable with B / Y.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Points every palette menu in the scene at the scene's build controller.</summary>
        private static void WireMenus(Scene scene, BuildController controller)
        {
            foreach (var menu in FindInScene<BuildPaletteMenu>(scene))
                Assign(menu, ("_controller", controller));
        }

        /// <summary>Interaction SDK canvases take ray and poke input through a PointableCanvasModule.</summary>
        private static void EnsurePointableEventSystem(Scene scene)
        {
            var system = FindInScene<EventSystem>(scene).FirstOrDefault();
            if (system == null)
            {
                var go = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(go, scene);
                system = go.AddComponent<EventSystem>();
            }

            foreach (var module in system.GetComponents<BaseInputModule>())
            {
                if (!(module is PointableCanvasModule)) Object.DestroyImmediate(module);
            }
            if (system.GetComponent<PointableCanvasModule>() == null) system.gameObject.AddComponent<PointableCanvasModule>();
        }

        // ---------------------------------------------------------------- materials

        private static Dictionary<string, Material> BuildMaterials()
        {
            return new Dictionary<string, Material>
            {
                ["Wood"] = Lit("Piece_Wood", new Color(0.55f, 0.38f, 0.22f)),
                ["LightWood"] = Lit("Piece_LightWood", new Color(0.72f, 0.56f, 0.36f)),
                ["DarkWood"] = Lit("Piece_DarkWood", new Color(0.36f, 0.24f, 0.14f)),
                ["Roof"] = Lit("Piece_Roof", new Color(0.32f, 0.34f, 0.38f)),
                ["Ground"] = Lit("Sandbox_Ground", new Color(0.33f, 0.42f, 0.27f)),
                ["Rock"] = Lit("Sandbox_Rock", new Color(0.5f, 0.5f, 0.52f)),
                ["GhostValid"] = Transparent("Ghost_Valid", new Color(0.3f, 1f, 0.45f, 0.4f)),
                ["GhostInvalid"] = Transparent("Ghost_Invalid", new Color(1f, 0.25f, 0.2f, 0.4f)),
                ["DeleteHighlight"] = Transparent("Highlight_Delete", new Color(1f, 0.45f, 0.1f, 0.65f)),
                ["LineValid"] = Unlit("Line_Valid", new Color(0.3f, 1f, 0.45f)),
                ["LineInvalid"] = Unlit("Line_Invalid", new Color(1f, 0.25f, 0.2f)),
                ["Pointer"] = Unlit("Line_Pointer", new Color(0.95f, 0.95f, 0.8f)),
            };
        }

        private static Material Lit(string name, Color color)
        {
            var material = LoadOrCreateMaterial(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Unlit(string name, Color color)
        {
            var material = LoadOrCreateMaterial(name, "Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Transparent(string name, Color color)
        {
            var material = Unlit(name, color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string name, string shaderName)
        {
            string path = $"{Materials}/{name}.mat";
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException($"Shader '{shaderName}' not found. Is URP installed?");

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            return material;
        }

        // ---------------------------------------------------------------- pieces

        /// <summary>
        /// Piece_Base: root (PieceView, pivot bottom-centre) ▸ Bounds (BoxCollider, sized from the
        /// footprint at runtime) ▸ Visual (meshes, supplied by each variant).
        /// </summary>
        private static GameObject BuildPieceBase()
        {
            var root = new GameObject("Piece_Base");
            try
            {
                var bounds = new GameObject("Bounds", typeof(BoxCollider));
                bounds.transform.SetParent(root.transform, false);
                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);

                var view = root.AddComponent<PieceView>();
                var so = new SerializedObject(view);
                so.FindProperty("_bounds").objectReferenceValue = bounds.GetComponent<BoxCollider>();
                so.FindProperty("_visual").objectReferenceValue = visual.transform;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, $"{PiecePrefabs}/Piece_Base.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildPieceVariant(GameObject pieceBase, PieceSpec spec, Dictionary<string, Material> materials)
        {
            return SaveVariant(pieceBase, $"{PiecePrefabs}/Piece_{Pascal(spec.Name)}.prefab", instance =>
            {
                instance.name = $"Piece_{Pascal(spec.Name)}";

                // Bake the footprint into the collider too, so the prefab looks right in the editor.
                var size = (Vector3)spec.Footprint * BuildGrid.DefaultCellSize;
                var collider = instance.transform.Find("Bounds").GetComponent<BoxCollider>();
                collider.size = size;
                collider.center = new Vector3(0f, size.y * 0.5f, 0f);

                var visual = instance.transform.Find("Visual");
                for (int i = 0; i < spec.Boxes.Length; i++)
                {
                    var box = spec.Boxes[i];
                    var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mesh.name = spec.Boxes.Length == 1 ? "Mesh" : $"Mesh {i + 1}";
                    Object.DestroyImmediate(mesh.GetComponent<Collider>());
                    mesh.transform.SetParent(visual, false);
                    mesh.transform.localPosition = box.Centre;
                    mesh.transform.localScale = box.Size;
                    mesh.GetComponent<MeshRenderer>().sharedMaterial = materials[box.Material];
                }
            });
        }

        private static PieceDefinition BuildDefinition(PieceSpec spec, GameObject prefab)
        {
            var definition = LoadOrCreate<PieceDefinition>($"{Definitions}/Piece_{Pascal(spec.Name)}.asset");
            definition.Id = spec.Id;
            definition.DisplayName = spec.Name;
            definition.Footprint = spec.Footprint;
            definition.Support = spec.Support;
            definition.Cost = new List<ResourceAmount> { new ResourceAmount(ResourceType.Boards, spec.Boards) };
            definition.Removable = true;
            definition.RefundOnRemove = true;
            definition.Prefab = prefab;
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void BuildProxy(PieceCatalog catalog)
        {
            var go = new GameObject("PlacedPiece", typeof(NetworkObject), typeof(PlacedPieceProxy));
            try
            {
                var so = new SerializedObject(go.GetComponent<PlacedPieceProxy>());
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, ProxyPath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ---------------------------------------------------------------- rig

        /// <summary>
        /// BuildRig_Base: root (BuildController) ▸ Preview (BuildPreviewView ▸ FootprintOutline) ▸ Pointer.
        /// Variants add the device-specific input and aim components.
        /// </summary>
        private static GameObject BuildRigBase(Dictionary<string, Material> materials)
        {
            var root = new GameObject("BuildRig_Base");
            try
            {
                var preview = new GameObject("Preview", typeof(BuildPreviewView));
                preview.transform.SetParent(root.transform, false);

                var outline = new GameObject("FootprintOutline", typeof(LineRenderer), typeof(BoundsOutlineView));
                outline.transform.SetParent(preview.transform, false);
                ConfigureLine(outline.GetComponent<LineRenderer>(), materials["LineValid"], 0.008f);
                Assign(outline.GetComponent<BoundsOutlineView>(), ("_validMaterial", materials["LineValid"]), ("_invalidMaterial", materials["LineInvalid"]));

                Assign(preview.GetComponent<BuildPreviewView>(),
                    ("_validMaterial", materials["GhostValid"]),
                    ("_invalidMaterial", materials["GhostInvalid"]),
                    ("_outline", outline.GetComponent<BoundsOutlineView>()));

                var pointer = new GameObject("Pointer", typeof(LineRenderer), typeof(PointerView));
                pointer.transform.SetParent(root.transform, false);
                ConfigureLine(pointer.GetComponent<LineRenderer>(), materials["Pointer"], 0.01f);

                var controller = root.AddComponent<BuildController>();
                Assign(controller,
                    ("_preview", preview.GetComponent<BuildPreviewView>()),
                    ("_pointer", pointer.GetComponent<PointerView>()),
                    ("_deleteHighlight", materials["DeleteHighlight"]));

                return PrefabUtility.SaveAsPrefabAsset(root, $"{RigPrefabs}/BuildRig_Base.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildRigVariant(GameObject rigBase, string name, Type inputType, Type aimType)
        {
            SaveVariant(rigBase, $"{RigPrefabs}/{name}.prefab", instance =>
            {
                instance.name = name;
                var input = instance.AddComponent(inputType);
                var aim = instance.AddComponent(aimType);
                Assign(instance.GetComponent<BuildController>(), ("_input", input), ("_aim", aim));
            });
        }

        private static void ConfigureLine(LineRenderer line, Material material, float width)
        {
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
        }

        // ---------------------------------------------------------------- scenes

        /// <summary>
        /// Creates the sandbox's test terrain and offline backend the first time, then keeps the
        /// backend on the current catalog and wires the Quest build rig onto the scene's own rig.
        /// </summary>
        private static void PrepareSandbox()
        {
            if (!File.Exists(SandboxScenePath)) CreateSandboxScene();

            var scene = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            var catalog = LoadCatalog();

            var backend = FindInScene<LocalConstructionBackend>(scene).FirstOrDefault()
                          ?? new GameObject("Construction (offline)").AddComponent<LocalConstructionBackend>();
            var so = new SerializedObject(backend);
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            var stock = so.FindProperty("_startingStock");
            if (stock.arraySize == 0) SetStock(stock, 500);
            so.ApplyModifiedPropertiesWithoutUndo();

            WireRayRig(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Test terrain only. The player rig is the scene author's: add the Meta building blocks, then wire.</summary>
        private static void CreateSandboxScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var materials = BuildMaterials();

            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.GetComponent<Light>().shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = materials["Ground"];

            // Scenery for testing: an off-lattice rock (Obstructed, scenery side faces) and a raised
            // deck whose top is ground one metre up.
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock (off-lattice obstacle)";
            rock.transform.position = new Vector3(3.13f, 0.4f, 2.71f);
            rock.transform.localScale = Vector3.one * 0.8f;
            rock.GetComponent<MeshRenderer>().sharedMaterial = materials["Rock"];

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Raised Deck (ground at 1 m)";
            deck.transform.position = new Vector3(-4f, 0.5f, 4f);
            deck.transform.localScale = new Vector3(3f, 1f, 3f);
            deck.GetComponent<MeshRenderer>().sharedMaterial = materials["Rock"];

            EditorSceneManager.SaveScene(scene, SandboxScenePath);
        }

        /// <summary>
        /// Composes the Quest build rig onto the scene's Interaction SDK controller rig. The right
        /// controller gets the stock ISDK ray interactor if it has none, then the rig's aim follows
        /// that ray and its confirm listens to that ray's trigger selector. Any other build rig in
        /// the scene is replaced, so there is exactly one.
        /// </summary>
        private static bool WireRayRig(Scene scene)
        {
            var controllers = FindInScene<Controller>(scene);
            var right = controllers.FirstOrDefault(c => c.name.Contains("Right"));
            var left = controllers.FirstOrDefault(c => c.name.Contains("Left"));
            if (right == null)
            {
                Debug.LogWarning($"[BuildKit] {scene.name}: no Interaction SDK RightController found, build rig not wired. " +
                                 "Add the Camera Rig and Controller Interactions building blocks, then run " +
                                 "Campfire Friends/Building/Wire Build Rig Into Open Scene.");
                return false;
            }

            var ray = right.GetComponentInChildren<RayInteractor>(true);
            if (ray == null) ray = AddRayInteractor(right);

            var selector = new SerializedObject(ray).FindProperty("_selector").objectReferenceValue;
            var rayController = ray.GetComponent<ControllerRef>();

            var rig = ReplaceBuildRigs(scene);
            Assign(rig.GetComponent<RayInteractorAim>(), ("_rayInteractor", ray));
            Assign(rig.GetComponent<RayInteractorBuildInput>(),
                ("_confirmSelector", selector),
                ("_mainHand", rayController != null ? rayController : (Object)right),
                ("_offHand", left));

            EnsurePointableEventSystem(scene);
            WireMenus(scene, rig.GetComponent<BuildController>());

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[BuildKit] {scene.name}: build rig wired to {ray.name} on {right.name}.", rig);
            return true;
        }

        private static RayInteractor AddRayInteractor(Controller controller)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(ControllerRayInteractorGuid));
            if (prefab == null) throw new InvalidOperationException("Interaction SDK ControllerRayInteractor prefab not found. Is com.meta.xr.sdk.interaction installed?");

            var parent = controller.transform.Find("ControllerInteractors");
            if (parent == null) parent = controller.transform;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = "ControllerRayInteractor";
            Assign(instance.GetComponent<ControllerRef>(), ("_controller", controller));
            return instance.GetComponent<RayInteractor>();
        }

        /// <summary>Keeps one BuildRig_Quest instance (adding it if needed) and removes every other build rig.</summary>
        private static GameObject ReplaceBuildRigs(Scene scene)
        {
            var questRig = AssetDatabase.LoadAssetAtPath<GameObject>($"{RigPrefabs}/BuildRig_Quest.prefab");
            GameObject kept = null;

            foreach (var controller in FindInScene<BuildController>(scene))
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(controller);
                if (root == null) root = controller.gameObject;

                if (kept == null && PrefabUtility.GetCorrespondingObjectFromSource(root) == questRig) kept = root;
                else Object.DestroyImmediate(root);
            }

            return kept != null ? kept : (GameObject)PrefabUtility.InstantiatePrefab(questRig, scene);
        }

        private static List<T> FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToList();
        }

        /// <summary>
        /// Points the Lobby's networked session at the new kit, removes the retired debug panel, and
        /// adds the Quest build rig.
        /// </summary>
        private static void UpgradeLobby()
        {
            if (!File.Exists(LobbyScenePath)) return;

            var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var catalog = LoadCatalog();
            var authority = Object.FindFirstObjectByType<ConstructionAuthority>(FindObjectsInactive.Include);
            if (authority != null)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(authority.gameObject);

                var so = new SerializedObject(authority);
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                so.FindProperty("_placedPiecePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NetworkObject>(ProxyPath);
                var stock = so.FindProperty("_startingStock");
                if (stock.arraySize == 0) SetStock(stock, 20);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            WireRayRig(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------------------------------------------------------------- helpers

        private static GameObject SaveVariant(GameObject basePrefab, string path, Action<GameObject> configure)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            try
            {
                configure(instance);
                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
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

        private static void SetStock(SerializedProperty list, int boards)
        {
            list.arraySize = 1;
            var entry = list.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative(nameof(ResourceAmount.Type)).enumValueIndex = (int)ResourceType.Boards;
            entry.FindPropertyRelative(nameof(ResourceAmount.Amount)).intValue = boards;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }

        private static string Pascal(string name) => name.Replace(" ", "");
    }
}
