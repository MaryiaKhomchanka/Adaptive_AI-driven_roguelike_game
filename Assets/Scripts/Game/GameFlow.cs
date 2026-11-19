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
    TextMeshProUGUI victoryStats, deathStats, toast, coinsLabel, bossNameLabel;
    string pendingToast;
    GameObject bossBar;
    RectTransform bossFill;
    Button[] rebindButtons;
    InputActionAsset bindings;
    InputActionRebindingExtensions.RebindingOperation rebinding;
    InputActionMap rebindMap;
    InputAction pauseAction;

    const string BindingsKey = "settings.bindings";
    static readonly (string action, string title)[] Rebinds = { ("Attack", "Атака"), ("Interact", "Действие"), ("Pause", "Пауза") };
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
        DungeonEvents.CoinsCollected += OnCoinsCollected;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); // Boot runs after the first scene is already loaded
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        PlayerHealth.Died -= OnPlayerDied;
        DungeonEvents.DungeonCompleted -= OnDungeonCompleted;
        DungeonEvents.CoinsCollected -= OnCoinsCollected;
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

        bindings = player != null ? player.actions : null;
        var savedBindings = PlayerPrefs.GetString(BindingsKey, "");
        if (bindings != null && savedBindings != "") bindings.LoadBindingOverridesFromJson(savedBindings);
        RefreshRebinds();

        bossBar.SetActive(false);
        coinsLabel.gameObject.SetActive(scene.name != GameScenes.MainMenu);
        RefreshCoins();
        if (pendingToast != null)
        {
            Toast(pendingToast);
            pendingToast = null;
        }

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

    void OnPlayerDied()
    {
        SaveData.Current.deaths++;
        SaveData.Save();
        var lost = RunState.Discard();
        deathStats.text = lost > 0 ? $"Потеряно монет: {lost}" : "";
        RefreshCoins();
        StartCoroutine(ShowAfterDelay(deathPage, 1.5f));
    }

    void OnCoinsCollected(int amount)
    {
        RunState.AddCoins(amount);
        RefreshCoins();
    }

    public void RefreshCoins()
    {
        var inDungeon = SceneManager.GetActiveScene().name == GameScenes.Dungeon;
        coinsLabel.text = inDungeon && RunState.Coins > 0
            ? $"Монеты: {SaveData.Current.coins}  (+{RunState.Coins} за забег, сохранятся только при победе)"
            : $"Монеты: {SaveData.Current.coins}";
    }

    public void SetBossBar(string title, float fraction)
    {
        bossBar.SetActive(true);
        bossNameLabel.text = title;
        bossFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    public void HideBossBar() => bossBar.SetActive(false);

    void OnApplicationQuit() => SaveData.Save();

    void OnDungeonCompleted(DungeonSummary summary)
    {
        if (ended) return;
        SaveData.Current.dungeonsCompleted++;
        var banked = RunState.Bank();
        RefreshCoins();
        victoryStats.text = $"Время: {summary.TimeSeconds:0} с\nКомнат пройдено: {summary.RoomsVisited}/{summary.RoomsTotal}\n" +
                            $"Комнат зачищено: {summary.RoomsCleared}\nМонет сохранено: {banked}\nВсего пройдено данжей: {SaveData.Current.dungeonsCompleted}";
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

    public void StartRun()
    {
        SaveData.Current.runsStarted++;
        DungeonRunner.StartDungeonIndex = SaveData.Current.dungeonsCompleted + 1; // each cleared dungeon makes the next one bigger
        Go(GameScenes.Dungeon);
    }
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
        var lost = RunState.Discard(); // leaving mid-run (hub/menu from pause) forfeits the run's loot
        if (lost > 0) pendingToast = $"Забег прерван: потеряно монет {lost}";
        SaveData.Save();
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
        deathStats = UiKit.Label(death, "", 28);
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
        fullscreenButton = UiKit.Button(settings, "", () => { GameSettings.Fullscreen = !GameSettings.Fullscreen; RefreshToggles(); }, 44);
        shakeButton = UiKit.Button(settings, "", () => { GameSettings.ScreenShake = !GameSettings.ScreenShake; RefreshToggles(); }, 44);
        UiKit.Label(settings, "Движение: WASD. Backspace отменяет переназначение; ЛКМ всегда атакует", 20);
        rebindButtons = new Button[Rebinds.Length];
        for (var i = 0; i < Rebinds.Length; i++)
        {
            var index = i;
            rebindButtons[i] = UiKit.Button(settings, "", () => StartRebind(index), 44);
        }
        UiKit.Button(settings, "Сбросить управление", ResetBindings, 44);
        UiKit.Button(settings, "Назад", CloseSettings, 44);
        RefreshToggles();
        RefreshRebinds();

        coinsLabel = UiKit.Label(canvasGO.transform, "", 30);
        coinsLabel.alignment = TextAlignmentOptions.Left;
        var coinsRt = (RectTransform)coinsLabel.transform;
        coinsRt.anchorMin = coinsRt.anchorMax = coinsRt.pivot = Vector2.zero;
        coinsRt.anchoredPosition = new Vector2(30, 24);
        coinsRt.sizeDelta = new Vector2(500, 50);

        bossBar = new GameObject("BossBar", typeof(RectTransform), typeof(Image));
        bossBar.transform.SetParent(canvasGO.transform, false);
        var barRt = (RectTransform)bossBar.transform;
        barRt.anchorMin = barRt.anchorMax = barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = new Vector2(0, -60);
        barRt.sizeDelta = new Vector2(900, 28);
        bossBar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bossBar.transform, false);
        bossFill = (RectTransform)fill.transform;
        bossFill.anchorMin = Vector2.zero;
        bossFill.anchorMax = Vector2.one;
        bossFill.offsetMin = new Vector2(4, 4);
        bossFill.offsetMax = new Vector2(-4, -4);
        fill.GetComponent<Image>().color = new Color(0.8f, 0.15f, 0.2f);
        bossNameLabel = UiKit.Label(bossBar.transform, "", 26, bold: true);
        var nameRt = (RectTransform)bossNameLabel.transform;
        nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 1f);
        nameRt.pivot = new Vector2(0.5f, 0f);
        nameRt.anchoredPosition = new Vector2(0, 4);
        nameRt.sizeDelta = new Vector2(600, 36);
        bossBar.SetActive(false);

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

    void StartRebind(int index)
    {
        var action = bindings != null ? bindings.FindAction(Rebinds[index].action) : null;
        if (action == null)
        {
            Toast("Переназначение доступно из паузы в игре");
            return;
        }

        rebinding?.Dispose();
        UiKit.SetButtonText(rebindButtons[index], $"{Rebinds[index].title}: нажмите клавишу...");
        rebindMap = action.actionMap;
        rebindMap.Disable(); // an enabled action can't be rebound
        rebinding = action.PerformInteractiveRebinding(0)
            .WithControlsExcluding("<Mouse>")
            .WithCancelingThrough("<Keyboard>/backspace")
            .OnComplete(_ => FinishRebind())
            .OnCancel(_ => FinishRebind())
            .Start();
    }

    void FinishRebind()
    {
        rebinding?.Dispose();
        rebinding = null;
        rebindMap?.Enable();
        PlayerPrefs.SetString(BindingsKey, bindings.SaveBindingOverridesAsJson());
        RefreshRebinds();
    }

    void ResetBindings()
    {
        if (bindings == null)
        {
            Toast("Сброс доступен из паузы в игре");
            return;
        }
        bindings.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BindingsKey);
        RefreshRebinds();
    }

    void RefreshRebinds()
    {
        if (rebindButtons == null) return;
        for (var i = 0; i < Rebinds.Length; i++)
        {
            var action = bindings != null ? bindings.FindAction(Rebinds[i].action) : null;
            UiKit.SetButtonText(rebindButtons[i], $"{Rebinds[i].title}: {(action != null ? action.GetBindingDisplayString(0) : "-")}");
        }
    }

    void RefreshToggles()
    {
        UiKit.SetButtonText(fullscreenButton, $"Полный экран: {(GameSettings.Fullscreen ? "вкл" : "выкл")}");
        UiKit.SetButtonText(shakeButton, $"Тряска экрана: {(GameSettings.ScreenShake ? "вкл" : "выкл")}");
    }
}
