using System;
using System.IO;
using UnityEngine;

/// <summary>Progress that survives between runs and sessions: wallet, upgrades, dungeons cleared. One JSON file.</summary>
[Serializable]
public sealed class SaveData
{
    public int coins;
    public int dungeonsCompleted;
    public int bonusHealth;
    public int runsStarted;
    public int deaths;

    static SaveData current;
    static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static SaveData Current => current ??= Load();

    static SaveData Load()
    {
        try
        {
            if (File.Exists(FilePath)) return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) ?? new SaveData();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] could not read {FilePath}, starting fresh: {e.Message}");
        }
        return new SaveData();
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Current));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] could not write {FilePath}: {e.Message}");
        }
    }
}
