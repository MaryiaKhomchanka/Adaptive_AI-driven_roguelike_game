using System.Collections.Generic;
using System.Linq;
using Generation.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Runtime
{
    /// <summary>Builds the room GameObjects (tilemaps, walls, props, door triggers) for a DungeonLayout, and
    /// spawns enemies/loot/specials the first time a room is activated.</summary>
    public sealed class DungeonBuilder : MonoBehaviour
    {
        /// <summary>
        /// Sorting order shared by props and characters, strictly above Floor/FloorDecor/Walls.
        /// Transparency Sort Mode = Custom Axis (Y) only breaks ties *within* the same sortingOrder bucket;
        /// it never overrides sortingOrder itself. Without this gap, floor tiles and the player share
        /// order 0 and the Y-sort tie-break can put the floor on top of the player.
        /// </summary>
        public const int ActorSortingOrder = 3;

        [Header("Libraries")]
        public TemplateLibrary templateLibrary;
        public PropLibrary propLibrary;
        public EnemySpawnTable enemyTable;
        public LootTable lootTable;
        public SpecialPrefabs specialPrefabs;

        [Header("Tiles")]
        public TileBase[] floorVariants;
        public TileBase floorDecorTile;
        public TileBase wallTile; // a RuleTile asset assigned here as TileBase

        [Header("Doors")]
        [Tooltip("Decorative only (spec 4: doors never close/lock); flanks each opening so it doesn't read as a plain hole in the wall.")]
        public Sprite doorFrameSprite;

        public DungeonLayout CurrentLayout { get; private set; }
        public Dictionary<int, RoomController> RoomsById { get; } = new Dictionary<int, RoomController>();

        public DungeonLayout Build(GenerationRequest request)
        {
            Clear();

            var enemyDefs = enemyTable != null ? enemyTable.ToDefs() : new List<EnemySpawnDef>();
            var lootDefs = lootTable != null ? lootTable.ToDefs() : new List<LootSpawnDef>();

            var layout = DungeonGenerator.Generate(request, templateLibrary.Templates, enemyDefs, lootDefs);
            CurrentLayout = layout;

            foreach (var room in layout.Rooms)
                RoomsById[room.Id] = BuildRoom(layout, room, request.Seed);

            BuildDoorTriggers(layout);

            DungeonEvents.RaiseDungeonGenerated(layout);

            return layout;
        }

        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            RoomsById.Clear();
            CurrentLayout = null;
        }

        RoomController BuildRoom(DungeonLayout layout, RoomNode room, int seed)
        {
            var roomGO = new GameObject($"Room_{room.Id}_{room.Type}");
            roomGO.transform.SetParent(transform, false);
            var controller = roomGO.AddComponent<RoomController>();

            var gridGO = new GameObject("Grid");
            gridGO.transform.SetParent(roomGO.transform, false);
            gridGO.AddComponent<Grid>().cellSize = Vector3.one;

            var floor = CreateTilemap(gridGO.transform, "Floor", sortingOrder: 0, withWallCollider: false);
            var floorDecor = CreateTilemap(gridGO.transform, "FloorDecor", sortingOrder: 1, withWallCollider: false);
            var walls = CreateTilemap(gridGO.transform, "Walls", sortingOrder: 2, withWallCollider: true);

            var propsGO = NewChild(roomGO.transform, "Props");
            var spawnedGO = NewChild(roomGO.transform, "Spawned");
            var doorTriggersGO = NewChild(roomGO.transform, "DoorTriggers");

            controller.Room = room;
            controller.Floor = floor;
            controller.FloorDecor = floorDecor;
            controller.Walls = walls;
            controller.Props = propsGO.transform;
            controller.Spawned = spawnedGO.transform;
            controller.DoorTriggers = doorTriggersGO.transform;

            var template = templateLibrary.Templates[room.TemplateId];
            var grid2D = TemplateTransformUtil.ApplyToGrid(template, room.TemplateTransform, out var w, out var h);

            var anchorTileX = room.Anchor.X * ShapeDefinition.ChunkTiles;
            var anchorTileY = room.Anchor.Y * ShapeDefinition.ChunkTiles;

            var openSides = OpenSidesOf(layout, room);

            PaintFloorAndDecor(floor, floorDecor, grid2D, w, h, anchorTileX, anchorTileY, Rng.ForStep(seed, $"tiles-{room.Id}"));
            PaintWalls(walls, room, openSides);
            PlaceDoorFrames(controller, room, openSides);
            PlaceProps(controller, grid2D, w, h, '#', PropClass.Obstacle, anchorTileX, anchorTileY, Rng.ForStep(seed, $"props-obstacle-{room.Id}"));
            PlaceProps(controller, grid2D, w, h, 'P', PropClass.SolidDecor, anchorTileX, anchorTileY, Rng.ForStep(seed, $"props-decor-{room.Id}"));

            controller.TileBounds = ComputeTileBounds(room);

            var sSlots = FindSlotPositions(grid2D, w, h, 'S');
            if (sSlots.Count > 0)
                controller.SpecialSlotWorldPosition = new Vector3(anchorTileX + sSlots[0].x + 0.5f, anchorTileY + sSlots[0].y + 0.5f, 0f);

            controller.OnFirstVisit = c => SpawnRoomContents(c, grid2D, w, h, anchorTileX, anchorTileY, seed);

            roomGO.SetActive(false); // all rooms start hidden; RoomTransitionController activates the Start room

            return controller;
        }

        void SpawnRoomContents(RoomController controller, char[,] grid2D, int w, int h, int anchorTileX, int anchorTileY, int seed)
        {
            var room = controller.Room;

            var eSlots = FindSlotPositions(grid2D, w, h, 'E');
            var lSlots = FindSlotPositions(grid2D, w, h, 'L');
            var sSlots = FindSlotPositions(grid2D, w, h, 'S');

            foreach (var spawn in room.Enemies)
            {
                if (spawn.SlotIndex < 0 || spawn.SlotIndex >= eSlots.Count) continue;
                var prefab = enemyTable != null ? enemyTable.Find(spawn.Id) : null;
                if (prefab == null) continue;
                SpawnMember(controller, prefab, eSlots[spawn.SlotIndex], anchorTileX, anchorTileY, room.Id, RoomMemberKind.Enemy);
            }

            foreach (var spawn in room.Loot)
            {
                if (spawn.SlotIndex < 0 || spawn.SlotIndex >= lSlots.Count) continue;
                var prefab = lootTable != null ? lootTable.Find(spawn.Id) : null;
                if (prefab == null) continue;
                SpawnMember(controller, prefab, lSlots[spawn.SlotIndex], anchorTileX, anchorTileY, room.Id, RoomMemberKind.Loot);
            }

            if (room.Special != null && sSlots.Count > 0 && specialPrefabs != null)
            {
                var prefab = specialPrefabs.ForId(room.Special.Id);
                if (prefab != null)
                {
                    var slot = sSlots[Mathf.Clamp(room.Special.SlotIndex, 0, sSlots.Count - 1)];
                    var kind = room.Special.Id switch
                    {
                        "boss" => RoomMemberKind.Boss,
                        "treasure" => RoomMemberKind.Treasure,
                        "shop" => RoomMemberKind.Shop,
                        "npc" => RoomMemberKind.Npc,
                        _ => RoomMemberKind.Loot,
                    };
                    SpawnMember(controller, prefab, slot, anchorTileX, anchorTileY, room.Id, kind);
                }
            }
        }

        void SpawnMember(RoomController controller, GameObject prefab, (int x, int y) localTile, int anchorTileX, int anchorTileY, int roomId, RoomMemberKind kind)
        {
            var worldPos = new Vector3(anchorTileX + localTile.x + 0.5f, anchorTileY + localTile.y + 0.5f, 0f);
            var instance = Instantiate(prefab, worldPos, Quaternion.identity, controller.Spawned);
            var member = instance.GetComponent<RoomMember>() ?? instance.AddComponent<RoomMember>();
            member.Initialize(roomId, kind);
            RoomActivity.Register(roomId, kind);
            foreach (var sr in instance.GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = ActorSortingOrder;
        }

        static List<(int x, int y)> FindSlotPositions(char[,] grid2D, int w, int h, char symbol)
        {
            var result = new List<(int x, int y)>();
            for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
                if (grid2D[x, y] == symbol)
                    result.Add((x, y));
            return result;
        }

        void BuildDoorTriggers(DungeonLayout layout)
        {
            var transitionController = GetComponent<RoomTransitionController>();

            foreach (var door in layout.Doors)
            {
                var (center, size) = DoorTriggerShape(door);
                CreateDoorTrigger(RoomsById[door.RoomA], door.RoomA, door.RoomB, center, size, transitionController);
                CreateDoorTrigger(RoomsById[door.RoomB], door.RoomA, door.RoomB, center, size, transitionController);
            }
        }

        static (Vector2 center, Vector2 size) DoorTriggerShape(DoorLink door)
        {
            var originX = door.ChunkA.X * ShapeDefinition.ChunkTiles;
            var originY = door.ChunkA.Y * ShapeDefinition.ChunkTiles;
            const float along = 7.5f; // midpoint of local tiles 7 and 8
            const float chunkEdge = ShapeDefinition.ChunkTiles;

            switch (door.SideA)
            {
                case Dir.N: return (new Vector2(originX + along, originY + chunkEdge), new Vector2(2, 2));
                case Dir.S: return (new Vector2(originX + along, originY), new Vector2(2, 2));
                case Dir.E: return (new Vector2(originX + chunkEdge, originY + along), new Vector2(2, 2));
                default: return (new Vector2(originX, originY + along), new Vector2(2, 2)); // W
            }
        }

        void CreateDoorTrigger(RoomController owner, int roomAId, int roomBId, Vector2 center, Vector2 size, RoomTransitionController transitionController)
        {
            var go = NewChild(owner.DoorTriggers, $"DoorTrigger_{roomAId}_{roomBId}");
            go.transform.position = center;
            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;
            var trigger = go.AddComponent<DoorTrigger>();
            trigger.RoomAId = roomAId;
            trigger.RoomBId = roomBId;
            trigger.Controller = transitionController;
        }

        static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder, bool withWallCollider)
        {
            var go = NewChild(parent, name);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;

            if (withWallCollider)
            {
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                var tilemapCollider = go.AddComponent<TilemapCollider2D>();
                tilemapCollider.usedByComposite = true;
                go.AddComponent<CompositeCollider2D>();
            }

            return tilemap;
        }

        void PaintFloorAndDecor(Tilemap floor, Tilemap floorDecor, char[,] grid2D, int w, int h, int anchorTileX, int anchorTileY, Rng rng)
        {
            for (var tx = 0; tx < w; tx++)
            {
                for (var ty = 0; ty < h; ty++)
                {
                    var c = grid2D[tx, ty];
                    if (c == 'X') continue;

                    var cell = new Vector3Int(anchorTileX + tx, anchorTileY + ty, 0);
                    if (floorVariants != null && floorVariants.Length > 0)
                        floor.SetTile(cell, floorVariants[rng.NextInt(0, floorVariants.Length)]);

                    if (c == ',' && floorDecorTile != null)
                        floorDecor.SetTile(cell, floorDecorTile);
                }
            }
        }

        static HashSet<(Cell chunk, Dir side)> OpenSidesOf(DungeonLayout layout, RoomNode room)
        {
            var openSides = new HashSet<(Cell chunk, Dir side)>();
            foreach (var door in layout.Doors)
            {
                if (door.RoomA == room.Id) openSides.Add((door.ChunkA, door.SideA));
                else if (door.RoomB == room.Id) openSides.Add((door.ChunkA + door.SideA.Step(), door.SideA.Opposite()));
            }
            return openSides;
        }

        void PaintWalls(Tilemap walls, RoomNode room, HashSet<(Cell chunk, Dir side)> openSides)
        {
            if (wallTile == null) return;

            var ownChunks = new HashSet<Cell>(room.Chunks);

            foreach (var chunk in room.Chunks)
            {
                foreach (var dir in DirExtensions.All)
                {
                    if (ownChunks.Contains(chunk + dir.Step())) continue; // internal boundary, no wall

                    var isOpen = openSides.Contains((chunk, dir));
                    for (var i = 0; i < ShapeDefinition.ChunkTiles; i++)
                    {
                        if (isOpen && (i == 7 || i == 8)) continue; // door opening, 2 tiles wide

                        var (lx, ly) = BorderLocalTile(dir, i);
                        var tileX = chunk.X * ShapeDefinition.ChunkTiles + lx;
                        var tileY = chunk.Y * ShapeDefinition.ChunkTiles + ly;
                        walls.SetTile(new Vector3Int(tileX, tileY, 0), wallTile);
                    }
                }
            }
        }

        static (int lx, int ly) BorderLocalTile(Dir side, int index)
        {
            var last = ShapeDefinition.ChunkTiles - 1;
            switch (side)
            {
                case Dir.N: return (index, last);
                case Dir.S: return (index, 0);
                case Dir.E: return (last, index);
                default: return (0, index); // W
            }
        }

        void PlaceDoorFrames(RoomController controller, RoomNode room, HashSet<(Cell chunk, Dir side)> openSides)
        {
            if (doorFrameSprite == null) return;

            foreach (var (chunk, side) in openSides)
            {
                foreach (var index in new[] { 6, 9 }) // flank the 2-tile opening (local 7,8) on each side
                {
                    var (lx, ly) = BorderLocalTile(side, index);
                    var worldX = chunk.X * ShapeDefinition.ChunkTiles + lx + 0.5f;
                    var worldY = chunk.Y * ShapeDefinition.ChunkTiles + ly + 0.5f;

                    var go = new GameObject("DoorFrame");
                    go.transform.SetParent(controller.Props, false);
                    go.transform.position = new Vector3(worldX, worldY, 0f);
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = doorFrameSprite;
                    renderer.sortingOrder = 2; // with the walls, not with actors - it's part of the frame, not something to walk behind/in front of
                }
            }
        }

        void PlaceProps(RoomController controller, char[,] grid2D, int w, int h, char symbol, PropClass propClass, int anchorTileX, int anchorTileY, Rng rng)
        {
            if (propLibrary == null) return;
            var entries = propLibrary.For(propClass).Where(e => e.variants != null && e.variants.Length > 0).ToList();
            if (entries.Count == 0) return;

            var sizes = entries.Select(e => new PropSize(e.width, e.height, e.weight)).ToList();
            var placements = PropPlacer.Plan(grid2D, w, h, symbol, sizes, variantCount: 0, rng);

            foreach (var p in placements)
            {
                var entry = entries.FirstOrDefault(e => e.width == p.Width && e.height == p.Height) ?? entries[0];
                var prefab = entry.variants[rng.NextInt(0, entry.variants.Length)];
                var worldX = anchorTileX + p.X + p.Width / 2f;
                var worldY = anchorTileY + p.Y + p.Height / 2f;
                var instance = Instantiate(prefab, new Vector3(worldX, worldY, 0f), Quaternion.identity, controller.Props);
                instance.name = $"{propClass}_{p.X}_{p.Y}";
                foreach (var sr in instance.GetComponentsInChildren<SpriteRenderer>())
                    sr.sortingOrder = ActorSortingOrder;
            }
        }

        static RectInt ComputeTileBounds(RoomNode room)
        {
            var minX = room.Chunks.Min(c => c.X) * ShapeDefinition.ChunkTiles;
            var minY = room.Chunks.Min(c => c.Y) * ShapeDefinition.ChunkTiles;
            var maxX = (room.Chunks.Max(c => c.X) + 1) * ShapeDefinition.ChunkTiles;
            var maxY = (room.Chunks.Max(c => c.Y) + 1) * ShapeDefinition.ChunkTiles;
            return new RectInt(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
