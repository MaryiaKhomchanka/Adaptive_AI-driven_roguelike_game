using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>An interactable in the hub: E when close either starts a run (the dungeon gate) or shows a "coming soon" toast.</summary>
public sealed class HubStation : MonoBehaviour
{
    public string title;
    public string message;
    public bool startsRun;
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
        label.text = near ? $"[E] {title}" : title;
        label.color = near ? Color.yellow : Color.white;

        if (!near || !interact.WasPressedThisFrame() || GameFlow.Instance == null) return;
        if (startsRun) GameFlow.Instance.StartRun();
        else GameFlow.Instance.Toast(message);
    }
}
