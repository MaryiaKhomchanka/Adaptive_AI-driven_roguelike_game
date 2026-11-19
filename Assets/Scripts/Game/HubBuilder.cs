using Generation.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds the hub room at start: a walled floor, the dungeon gate and placeholder stations for future systems.
/// The hub is the start of the game and where the player respawns after dying or finishing a dungeon.
/// </summary>
public sealed class HubBuilder : MonoBehaviour
{
    public TileBase floorTile;
    public TileBase wallTile;
    public Transform player;
    public Vector2Int size = new Vector2Int(22, 14);

    void Start()
    {
        BuildTiles();

        Station("Dungeon Gate", "", StationAction.StartRun, new Color(1f, 0.8f, 0.2f), new Vector2(size.x / 2f, size.y - 3f));
        Station("Shop", "The shop opens later", StationAction.Message, new Color(0.3f, 0.5f, 1f), new Vector2(5f, size.y - 6f));
        Station("Upgrades", "", StationAction.BuyHealth, new Color(0.3f, 0.85f, 0.45f), new Vector2(size.x - 5f, size.y - 6f));

        var roomCamera = Camera.main != null ? Camera.main.GetComponent<RoomCamera>() : null;
        if (roomCamera == null || player == null) return;
        roomCamera.target = player;
        roomCamera.RoomBounds = new RectInt(0, 0, size.x, size.y);
        var cameraPosition = roomCamera.transform.position;
        roomCamera.transform.position = new Vector3(player.position.x, player.position.y, cameraPosition.z);
    }

    void BuildTiles()
    {
        var grid = new GameObject("Grid", typeof(Grid));
        grid.transform.SetParent(transform, false);
        var floor = DungeonBuilder.CreateTilemap(grid.transform, "Floor", 0, false);
        var walls = DungeonBuilder.CreateTilemap(grid.transform, "Walls", 2, true);
        walls.gameObject.layer = LayerMask.NameToLayer("Walls");

        for (var x = 0; x < size.x; x++)
        {
            for (var y = 0; y < size.y; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                floor.SetTile(cell, floorTile);
                if (x == 0 || y == 0 || x == size.x - 1 || y == size.y - 1) walls.SetTile(cell, wallTile);
            }
        }
    }

    void Station(string title, string message, StationAction action, Color color, Vector2 position)
    {
        var go = new GameObject(title);
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 16f; // the 1/8-unit Fx pixel becomes a 2x2 block
        go.layer = LayerMask.NameToLayer("Walls");

        var sprite = go.AddComponent<SpriteRenderer>();
        sprite.sprite = Fx.Pixel;
        sprite.color = color;
        sprite.sortingOrder = DungeonBuilder.ActorSortingOrder;
        PixelLit.World(sprite);
        go.AddComponent<BoxCollider2D>().size = new Vector2(0.125f, 0.125f);

        var labelGO = new GameObject(title + " label");
        labelGO.transform.SetParent(transform, false);
        labelGO.transform.position = position + Vector2.up * 1.7f;
        var label = labelGO.AddComponent<TextMeshPro>();
        if (Fx.Font != null) label.font = Fx.Font;
        label.fontSize = 2.4f;
        label.alignment = TextAlignmentOptions.Center;
        label.sortingOrder = DungeonBuilder.ActorSortingOrder + 3;
        label.rectTransform.sizeDelta = new Vector2(12f, 1f);

        var station = go.AddComponent<HubStation>();
        station.title = title;
        station.message = message;
        station.action = action;
        station.player = player;
        station.label = label;
    }
}
