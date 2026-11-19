using Generation.Runtime;
using UnityEngine;

/// <summary>Player-facing options, stored in PlayerPrefs. Music and SFX volumes are placeholders until sound exists.</summary>
public static class GameSettings
{
    const string MasterKey = "settings.master";
    const string MusicKey = "settings.music";
    const string SfxKey = "settings.sfx";
    const string FullscreenKey = "settings.fullscreen";
    const string ShakeKey = "settings.shake";

    public static float Master { get => PlayerPrefs.GetFloat(MasterKey, 1f); set { PlayerPrefs.SetFloat(MasterKey, value); Apply(); } }
    public static float Music { get => PlayerPrefs.GetFloat(MusicKey, 0.8f); set => PlayerPrefs.SetFloat(MusicKey, value); }
    public static float Sfx { get => PlayerPrefs.GetFloat(SfxKey, 0.8f); set => PlayerPrefs.SetFloat(SfxKey, value); }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        set { PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); Apply(); }
    }

    public static bool ScreenShake
    {
        get => PlayerPrefs.GetInt(ShakeKey, 1) == 1;
        set { PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0); Apply(); }
    }

    public static void Apply()
    {
        AudioListener.volume = Master;
        Screen.fullScreen = Fullscreen;
        RoomCamera.ShakeScale = ScreenShake ? 1f : 0f;
    }
}
