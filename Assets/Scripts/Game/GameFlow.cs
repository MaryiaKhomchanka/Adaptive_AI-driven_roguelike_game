using System.Collections;
using Generation.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent game flow: main menu, pause, settings, death and victory screens, and moving between menu, hub and dungeon.
/// Creates itself in every scene (including when a scene is played straight from the editor) and builds its UI in code.
/// Death and victory both end in the hub, which is where the player respawns.
/// </summary>
public sealed class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }

    GameObject mainPage, pausePage, settingsPage, deathPage, victoryPage;
    GameObject settingsReturnPage;
    Button pauseHubButton, fullscreenButton, shakeButton;
    TextMeshProUGUI victoryStats, toast;
    InputAction pauseAction;
    bool paused;
    bool ended;
    float toastUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("GameFlow");
        DontDestroyOnLoad(go);
        go.AddComponent<GameFlow>();
    }

    void Awake()
    {
        Instance = this;
        GameSettings.Apply();
        BuildUi();

        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayerHealth.Died += OnPlayerDied;
        DungeonEvents.DungeonCompleted += OnDungeonCompleted;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); // Boot runs after the first scene is already loaded
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        PlayerHealth.Died -= OnPlayerDied;
        DungeonEvents.DungeonCompleted -= OnDungeonCompleted;
        if (Instance == this) Instance = null;
    }

    // ---- scene hooks

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        paused = ended = false;
        Time.timeScale = 1f;
        HideAll();

        EnsureEventSystem();
        var player = FindFirstObjectByType<PlayerInput>();
        pauseAction = player != null ? player.actions["Pause"] : null;

        var runner = FindFirstObjectByType<DungeonRunner>();
        if (runner != null) runner.advanceOnCompletion = false; // the victory screen decides what happens next

        if (scene.name == GameScenes.MainMenu) Show(mainPage);
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    void OnPlayerDied() => StartCoroutine(ShowAfterDelay(deathPage, 1.5f));

    void OnDungeonCompleted(DungeonSummary summary)
    {
        if (ended) return;
        victoryStats.text = $"Время: {summary.TimeSeconds:0} с\nКомнат пройдено: {summary.RoomsVisited}/{summary.RoomsTotal}\n" +
                            $"Комнат зачищено: {summary.RoomsCleared}";
        End(victoryPage);
    }

    IEnumerator ShowAfterDelay(GameObject page, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        End(page);
    }

    void End(GameObject page)
    {
        ended = true;
        Time.timeScale = 0f;
        Show(page);
    }

    // ---- input

    void Update()
    {
        if (Time.unscaledTime > toastUntil && toast.gameObject.activeSelf) toast.gameObject.SetActive(false);

        if (pauseAction == null || ended || !pauseAction.WasPressedThisFrame()) return;
        if (settingsPage.activeSelf) CloseSettings();
        else if (!BossPromptOpen()) SetPaused(!paused);
    }

    static bool BossPromptOpen()
    {
        var prompt = FindFirstObjectByType<BossEntryPrompt>();
        return prompt != null && prompt.panel != null && prompt.panel.activeSelf;
    }

    void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = value ? 0f : 1f;
        HideAll();
        if (!value) return;
        pauseHubButton.gameObject.SetActive(SceneManager.GetActiveScene().name != GameScenes.Hub);
        Show(pausePage);
    }

    // ---- navigation (also used by hub objects)

    public void StartRun() => Go(GameScenes.Dungeon);
    public void ToHub() => Go(GameScenes.Hub);
    public void ToMainMenu() => Go(GameScenes.MainMenu);

    public void Toast(string text)
    {
        toast.text = text;
        toast.gameObject.SetActive(true);
        toastUntil = Time.unscaledTime + 2.5f;
    }

    void Go(string scene)
    {
        Time.timeScale = 1f;
        paused = ended = false;
        HideAll();
        SceneManager.LoadScene(scene);
    }

    void OpenSettings(GameObject returnTo)
    {
        settingsReturnPage = returnTo;
        Show(settingsPage);
    }

    void CloseSettings() => Show(settingsReturnPage != null ? settingsReturnPage : pausePage);

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---- UI

    GameObject[] Pages => new[] { mainPage, pausePage, settingsPage, deathPage, victoryPage };

    void HideAll()
    {
        foreach (var page in Pages) page.SetActive(false);
    }

    void Show(GameObject page)
    {
        HideAll();
        page.SetActive(true);
    }

    void BuildUi()
    {
        var canvasGO = new GameObject("GameFlowCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var dim = new Color(0f, 0f, 0f, 0.8f);

        var mainTuple = UiKit.Page(canvasGO.transform, "MainMenu", new Color(0.05f, 0.05f, 0.09f, 1f));
        mainPage = mainTuple.root;
        var main = mainTuple.content;
        UiKit.Label(main, Application.productName, 64, bold: true);
        UiKit.Label(main, " ", 20);
        UiKit.Button(main, "Играть", ToHub);
        UiKit.Button(main, "Настройки", () => OpenSettings(mainPage));
        UiKit.Button(main, "Выход", Quit);

        var pauseTuple = UiKit.Page(canvasGO.transform, "Pause", dim);
        pausePage = pauseTuple.root;
        var pause = pauseTuple.content;
        UiKit.Label(pause, "Пауза", 56, bold: true);
        UiKit.Button(pause, "Продолжить", () => SetPaused(false));
        UiKit.Button(pause, "Настройки", () => OpenSettings(pausePage));
        pauseHubButton = UiKit.Button(pause, "В хаб (прервать забег)", ToHub);
        UiKit.Button(pause, "В главное меню", ToMainMenu);

        var deathTuple = UiKit.Page(canvasGO.transform, "Death", new Color(0.15f, 0f, 0f, 0.85f));
        deathPage = deathTuple.root;
        var death = deathTuple.content;
        UiKit.Label(death, "Вы погибли", 64, bold: true);
        UiKit.Button(death, "Возродиться в хабе", ToHub);
        UiKit.Button(death, "В главное меню", ToMainMenu);

        var victoryTuple = UiKit.Page(canvasGO.transform, "Victory", new Color(0f, 0.1f, 0.05f, 0.85f));
        victoryPage = victoryTuple.root;
        var victory = victoryTuple.content;
        UiKit.Label(victory, "Данж пройден!", 64, bold: true);
        victoryStats = UiKit.Label(victory, "", 28);
        UiKit.Button(victory, "Вернуться в хаб", ToHub);

        var settingsTuple = UiKit.Page(canvasGO.transform, "Settings", new Color(0.05f, 0.05f, 0.09f, 0.97f));
        settingsPage = settingsTuple.root;
        var settings = settingsTuple.content;
        UiKit.Label(settings, "Настройки", 48, bold: true);
        VolumeRow(settings, "Общая громкость", GameSettings.Master, v => GameSettings.Master = v);
        VolumeRow(settings, "Музыка (скоро)", GameSettings.Music, v => GameSettings.Music = v);
        VolumeRow(settings, "Звуки (скоро)", GameSettings.Sfx, v => GameSettings.Sfx = v);
        fullscreenButton = UiKit.Button(settings, "", () => { GameSettings.Fullscreen = !GameSettings.Fullscreen; RefreshToggles(); });
        shakeButton = UiKit.Button(settings, "", () => { GameSettings.ScreenShake = !GameSettings.ScreenShake; RefreshToggles(); });
        UiKit.Label(settings, "Управление: WASD - движение, E - действие, F / ЛКМ - атака, Esc - пауза", 22);
        UiKit.Button(settings, "Назад", CloseSettings);
        RefreshToggles();

        var toastGO = UiKit.Label(canvasGO.transform, "", 30);
        toast = toastGO;
        var toastRt = (RectTransform)toast.transform;
        toastRt.anchorMin = toastRt.anchorMax = toastRt.pivot = new Vector2(0.5f, 0f);
        toastRt.anchoredPosition = new Vector2(0, 90);
        toastRt.sizeDelta = new Vector2(900, 60);
        toast.gameObject.SetActive(false);

        HideAll();
    }

    static void VolumeRow(Transform parent, string title, float value, UnityEngine.Events.UnityAction<float> onChange)
    {
        var label = UiKit.Label(parent, "", 24);
        void Refresh(float v) => label.text = $"{title}: {Mathf.RoundToInt(v * 100)}%";
        Refresh(value);
        UiKit.Slider(parent, value, v => { Refresh(v); onChange(v); });
    }

    void RefreshToggles()
    {
        UiKit.SetButtonText(fullscreenButton, $"Полный экран: {(GameSettings.Fullscreen ? "вкл" : "выкл")}");
        UiKit.SetButtonText(shakeButton, $"Тряска экрана: {(GameSettings.ScreenShake ? "вкл" : "выкл")}");
    }
}
