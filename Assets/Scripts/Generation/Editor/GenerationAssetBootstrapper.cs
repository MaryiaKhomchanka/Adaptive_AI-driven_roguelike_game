using System.IO;
using System.Linq;
using Generation.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Generation.Editor
{
    /// <summary>
    /// One-shot tool (spec 5, optional Generation.Editor assembly): creates placeholder sprites/tiles/props,
    /// the library ScriptableObjects, and Assets/Scenes/GenerationTest.unity. Run once from the menu;
    /// safe to re-run (overwrites its own generated assets, touches nothing else).
    /// Never touches the scene(s) you currently have open: the test scene is built and saved as a separate
    /// additive scene, and SampleScene is only opened additively to clone the Player, then closed unsaved.
    /// </summary>
    public static class GenerationAssetBootstrapper
    {
        const string PlaceholdersRoot = "Assets/Generation/Placeholders";
        const string TemplatesRoot = "Assets/Generation/Templates";
        const string ConfigRoot = "Assets/Generation";
        const string ScenePath = "Assets/Scenes/GenerationTest.unity";
        const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Generation/Bootstrap Placeholder Assets And Test Scene")]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog(
                    "Generation bootstrap",
                    "This creates placeholder sprites/tiles/prefabs under Assets/Generation/Placeholders, " +
                    "config assets under Assets/Generation, and Assets/Scenes/GenerationTest.unity.\n\n" +
                    "It briefly opens SampleScene.unity additively (to clone the Player) and closes it again " +
                    "without saving. Your currently open scene(s) are not touched.\n\nProceed?",
                    "Proceed", "Cancel"))
                return;

            Directory.CreateDirectory(PlaceholdersRoot + "/Sprites");
            Directory.CreateDirectory(PlaceholdersRoot + "/Tiles");
            Directory.CreateDirectory(PlaceholdersRoot + "/Props");
            AssetDatabase.Refresh();

            var sprites = PlaceholderSprites.CreateAll(PlaceholdersRoot + "/Sprites");
            var tiles = PlaceholderTiles.CreateAll(PlaceholdersRoot + "/Tiles", sprites);
            var props = PlaceholderProps.CreateAll(PlaceholdersRoot + "/Props", sprites);

            var config = CreateOrLoad<GenerationConfig>(ConfigRoot + "/GenerationConfig.asset");
            var templateLibrary = BuildTemplateLibrary();
            var propLibrary = BuildPropLibrary(props);

            BuildTestScene(config, templateLibrary, propLibrary, tiles);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Generation bootstrap", "Done. Open Assets/Scenes/GenerationTest.unity.", "OK");
        }

        static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static TemplateLibrary BuildTemplateLibrary()
        {
            var library = CreateOrLoad<TemplateLibrary>(ConfigRoot + "/TemplateLibrary.asset");
            var files = AssetDatabase.FindAssets("", new[] { TemplatesRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".txt"))
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<TextAsset>)
                .ToList();
            library.templateFiles = files;
            EditorUtility.SetDirty(library);
            return library;
        }

        static PropLibrary BuildPropLibrary(PlaceholderProps.Result props)
        {
            var library = CreateOrLoad<PropLibrary>(ConfigRoot + "/PropLibrary.asset");
            library.entries = new System.Collections.Generic.List<PropEntry>
            {
                new PropEntry { propClass = PropClass.Obstacle, width = 1, height = 1, weight = 1f, variants = new[] { props.Obstacle1x1 } },
                new PropEntry { propClass = PropClass.Obstacle, width = 2, height = 2, weight = 1.5f, variants = new[] { props.Obstacle2x2 } },
                new PropEntry { propClass = PropClass.Obstacle, width = 3, height = 1, weight = 1f, variants = new[] { props.Obstacle3x1 } },
                new PropEntry { propClass = PropClass.Obstacle, width = 1, height = 3, weight = 1f, variants = new[] { props.Obstacle1x3 } },
                new PropEntry { propClass = PropClass.SolidDecor, width = 1, height = 1, weight = 1f, variants = new[] { props.SolidDecor1x1 } },
                new PropEntry { propClass = PropClass.SolidDecor, width = 2, height = 2, weight = 1f, variants = new[] { props.SolidDecor2x2 } },
            };
            EditorUtility.SetDirty(library);
            return library;
        }

        static void BuildTestScene(GenerationConfig config, TemplateLibrary templateLibrary, PropLibrary propLibrary, PlaceholderTiles.Result tiles)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var camera = new GameObject("Main Camera");
                SceneManager.MoveGameObjectToScene(camera, scene);
                var cam = camera.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5;
                cam.backgroundColor = Color.black;
                cam.clearFlags = CameraClearFlags.SolidColor;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.tag = "MainCamera";
                var roomCamera = camera.AddComponent<RoomCamera>();

                var dungeonGO = new GameObject("Dungeon");
                SceneManager.MoveGameObjectToScene(dungeonGO, scene);
                var builder = dungeonGO.AddComponent<DungeonBuilder>();
                builder.templateLibrary = templateLibrary;
                builder.propLibrary = propLibrary;
                builder.floorVariants = tiles.FloorVariants;
                builder.floorDecorTile = tiles.FloorDecor;
                builder.wallTile = tiles.Wall;

                var player = ClonePlayerFromSampleScene(scene);

                var runnerGO = new GameObject("DungeonRunner");
                SceneManager.MoveGameObjectToScene(runnerGO, scene);
                var runner = runnerGO.AddComponent<DungeonRunner>();
                runner.builder = builder;
                runner.config = config;
                runner.player = player != null ? player.transform : null;
                runner.roomCamera = roomCamera;

                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static GameObject ClonePlayerFromSampleScene(Scene targetScene)
        {
            if (!File.Exists(SampleScenePath))
            {
                Debug.LogWarning($"{SampleScenePath} not found; GenerationTest scene will have no Player.");
                return null;
            }

            var sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Additive);
            try
            {
                var source = sampleScene.GetRootGameObjects().FirstOrDefault(go => go.name == "Player");
                if (source == null)
                {
                    Debug.LogWarning("Could not find a 'Player' root GameObject in SampleScene.");
                    return null;
                }

                var clone = Object.Instantiate(source);
                clone.name = "Player";
                SceneManager.MoveGameObjectToScene(clone, targetScene);

                clone.tag = "Player";
                if (clone.GetComponent<Collider2D>() == null)
                {
                    var collider = clone.AddComponent<CircleCollider2D>();
                    collider.radius = 0.3f;
                }
                foreach (var sr in clone.GetComponentsInChildren<SpriteRenderer>())
                    sr.sortingOrder = DungeonBuilder.ActorSortingOrder;

                return clone;
            }
            finally
            {
                EditorSceneManager.CloseScene(sampleScene, true);
            }
        }
    }
}
