using Generation.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>Creates the MainMenu and Hub scenes and puts MainMenu, Hub and the dungeon scene into Build Settings.
/// The menu UI itself is built at runtime by GameFlow, so the MainMenu scene is only a camera.</summary>
public static class GameScenesBootstrapper
{
    const string MenuPath = "Assets/Scenes/MainMenu.unity";
    const string HubPath = "Assets/Scenes/Hub.unity";
    const string DungeonPath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Game/Create Menu And Hub Scenes")]
    public static void Run()
    {
        CreateMenuScene();
        CreateHubScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuPath, true),
            new EditorBuildSettingsScene(HubPath, true),
            new EditorBuildSettingsScene(DungeonPath, true),
        };
        EditorUtility.DisplayDialog("Game scenes", "Created MainMenu and Hub and set Build Settings (MainMenu, Hub, SampleScene).", "OK");
    }

    static GameObject NewCamera(Scene scene)
    {
        var go = new GameObject("Main Camera");
        SceneManager.MoveGameObjectToScene(go, scene);
        var camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.05f, 0.09f);
        go.AddComponent<AudioListener>();
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 0, -10);
        return go;
    }

    static void CreateMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        NewCamera(scene);
        EditorSceneManager.SaveScene(scene, MenuPath);
        EditorSceneManager.CloseScene(scene, true);
    }

    static void CreateHubScene()
    {
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player 1.prefab");
        var heartsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/HeartsContainer.prefab");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        NewCamera(scene).AddComponent<RoomCamera>();

        var lightGO = new GameObject("Global Light 2D");
        SceneManager.MoveGameObjectToScene(lightGO, scene);
        var globalLight = lightGO.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = 1f;

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
        player.transform.position = new Vector3(11f, 3f, 0f);

        var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasGO, scene);
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var hearts = (GameObject)PrefabUtility.InstantiatePrefab(heartsPrefab, canvasGO.transform);

        var health = new SerializedObject(player.GetComponent<PlayerHealth>());
        health.FindProperty("heartUI").objectReferenceValue = hearts.GetComponent<HeartUI>();
        health.ApplyModifiedPropertiesWithoutUndo();

        var hubGO = new GameObject("Hub");
        SceneManager.MoveGameObjectToScene(hubGO, scene);
        var hub = hubGO.AddComponent<HubBuilder>();
        hub.floorTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Generation/Placeholders/Tiles/FloorTile0.asset");
        hub.wallTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Generation/Placeholders/Tiles/WallRuleTile.asset");
        hub.player = player.transform;

        EditorSceneManager.SaveScene(scene, HubPath);
        EditorSceneManager.CloseScene(scene, true);
    }
}
