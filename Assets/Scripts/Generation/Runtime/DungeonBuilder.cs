using System.Collections.Generic;
using System.Linq;
using Generation.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Runtime
{
    /// <summary>Builds the room GameObjects (tilemaps, walls, props) for a DungeonLayout. No spawning yet (stage 4).</summary>
    public sealed class DungeonBuilder : MonoBehaviour
    {
        [Header("Libraries")]
        public TemplateLibrary templateLibrary;
        public PropLibrary propLibrary;

        [Header("Tiles")]
        public TileBase[] floorVariants;
        public TileBase floorDecorTile;
        public TileBase wallTile; // a RuleTile asset assigned here as TileBase

        public DungeonLayout CurrentLayout { get; private set; }
        public Dictionary<int, RoomController> RoomsById { get; } = new Dictionary<int, RoomController>();

        public DungeonLayout Build(GenerationRequest request)
        {
            Clear();

            var layout = DungeonGenerator.Generate(request, templateLibrary.Templates);
            CurrentLayout = layout;

            foreach (var room in layout.Rooms)
                RoomsById[room.Id] = BuildRoom(layout, room, request.Seed);

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

            PaintFloorAndDecor(floor, floorDecor, grid2D, w, h, anchorTileX, anchorTileY, Rng.ForStep(seed, $"tiles-{room.Id}"));
            PaintWalls(walls, layout, room, anchorTileX, anchorTileY);
            PlaceProps(controller, grid2D, w, h, '#', PropClass.Obstacle, anchorTileX, anchorTileY, Rng.ForStep(seed, $"props-obstacle-{room.Id}"));
            PlaceProps(controller, grid2D, w, h, 'P', PropClass.SolidDecor, anchorTileX, anchorTileY, Rng.ForStep(seed, $"props-decor-{room.Id}"));

            controller.TileBounds = ComputeTileBounds(room);

            return controller;
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

        static readonly HashSet<char> FloorLike = new HashSet<char> { '.', ',', 'E', 'L', 'S' };

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

        void PaintWalls(Tilemap walls, DungeonLayout layout, RoomNode room, int anchorTileX, int anchorTileY)
        {
            if (wallTile == null) return;

            var ownChunks = new HashSet<Cell>(room.Chunks);
            var openSides = new HashSet<(Cell chunk, Dir side)>();
            foreach (var door in layout.Doors)
            {
                if (door.RoomA == room.Id) openSides.Add((door.ChunkA, door.SideA));
                else if (door.RoomB == room.Id) openSides.Add((door.ChunkA + door.SideA.Step(), door.SideA.Opposite()));
            }

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
