using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>An interactable in the hub: E when close either starts a run (the dungeon gate) or shows a "coming soon" toast.</summary>
public enum StationAction { StartRun, Message, BuyHealth }

public sealed class HubStation : MonoBehaviour
{
    public string title;
    public string message;
    public StationAction action;
    public float reach = 2.4f;
    public Transform player;
    public TMP_Text label;

    InputAction interact;

    void Start()
    {
        if (player != null && player.TryGetComponent<PlayerInput>(out var input))
            interact = input.actions["Interact"];
    }

    void Update()
    {
        if (player == null || interact == null) return;

        var near = Vector2.Distance(player.position, transform.position) <= reach;
        var shown = action == StationAction.BuyHealth ? $"{title}: +1 heart ({HealthCost()}c)" : title;
        label.text = near ? $"[E] {shown}" : shown;
        label.color = near ? Color.yellow : Color.white;

        if (!near || !interact.WasPressedThisFrame() || GameFlow.Instance == null) return;
        switch (action)
        {
            case StationAction.StartRun: GameFlow.Instance.StartRun(); break;
            case StationAction.BuyHealth: BuyHealth(); break;
            default: GameFlow.Instance.Toast(message); break;
        }
    }

    static int HealthCost() => 10 * (SaveData.Current.bonusHealth + 1);

    void BuyHealth()
    {
        var save = SaveData.Current;
        var cost = HealthCost();
        if (save.coins < cost)
        {
            GameFlow.Instance.Toast($"Need {cost} coins, you have {save.coins}");
            return;
        }

        save.coins -= cost;
        save.bonusHealth++;
        SaveData.Save();
        player.GetComponent<PlayerHealth>().GrowMaxHealth(1);
        GameFlow.Instance.RefreshCoins();
        GameFlow.Instance.Toast("Max health +1");
    }
}
