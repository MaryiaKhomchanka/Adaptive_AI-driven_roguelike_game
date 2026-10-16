using System.Collections.Generic;
using System.Linq;
using Generation.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Generation.Runtime
{
    enum MinimapRoomState { Hidden, Seen, BossHint, Visited, Current }

    /// <summary>
    /// Spec section 11: built from DungeonLayout, no second camera. One chunk = a square; a room's chunks sit
    /// flush against each other (no seam) while different rooms keep a gap, by using each room's own minimum
    /// chunk as a local origin inside the shared (gapped) chunk grid.
    /// </summary>
    public sealed class MinimapController : MonoBehaviour
    {
        public DungeonBuilder builder;
        public RoomTransitionController transitionController;
        public Transform player;

        [Header("Layout")]
        public RectTransform content; // centered on the current room each time it changes
        public float chunkSize = 14f;
        public float chunkGap = 2f;
        public bool showDoorConnectors = true;
        public bool revealBossRoom = true;

        [Header("Colors")]
        public Color seenColor = new Color32(55, 55, 55, 255);
        public Color bossHintColor = new Color32(85, 70, 70, 255);
        public Color visitedColor = new Color32(190, 190, 190, 255);
        public Color currentColor = Color.white;
        public Color doorConnectorColor = new Color32(130, 130, 130, 255);

        [Header("Icons (placeholder colors, spec 13)")]
        public Color enemyIconColor = new Color32(200, 40, 40, 255);
        public Color lootIconColor = new Color32(230, 200, 60, 255);
        public Color treasureIconColor = new Color32(210, 170, 40, 255);
        public Color shopIconColor = new Color32(40, 90, 200, 255);
        public Color npcIconColor = new Color32(40, 160, 70, 255);
        public Color bossIconColor = new Color32(140, 20, 20, 255);
        public float iconSize = 5f;

        readonly Dictionary<int, List<Image>> roomChunkImages = new Dictionary<int, List<Image>>();
        readonly Dictionary<int, RectTransform> roomIconRows = new Dictionary<int, RectTransform>();
        readonly Dictionary<int, Vector2> roomCenters = new Dictionary<int, Vector2>();
        RectTransform playerDot;
        DungeonLayout layout;
        bool revealAll;

        void OnEnable()
        {
            DungeonEvents.DungeonGenerated += OnDungeonGenerated;
            DungeonEvents.RoomEntered += OnRoomChanged;
            DungeonEvents.RoomCleared += OnRoomContentChanged;
            DungeonEvents.RoomLooted += OnRoomContentChanged;
            DungeonEvents.BossResolved += OnRoomContentChanged;
        }

        void OnDisable()
        {
            DungeonEvents.DungeonGenerated -= OnDungeonGenerated;
            DungeonEvents.RoomEntered -= OnRoomChanged;
            DungeonEvents.RoomCleared -= OnRoomContentChanged;
            DungeonEvents.RoomLooted -= OnRoomContentChanged;
            DungeonEvents.BossResolved -= OnRoomContentChanged;
        }

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                revealAll = !revealAll;
                RefreshStatesAndIcons();
            }
#endif
            UpdatePlayerDot();
        }

        void OnDungeonGenerated(DungeonLayout newLayout)
        {
            layout = newLayout;
            Build();
        }

        void OnRoomChanged(int roomId, RoomType type, bool isFirstVisit)
        {
            RecenterOnCurrentRoom();
            RefreshStatesAndIcons();
        }

        void OnRoomContentChanged(int roomId) => RefreshStatesAndIcons();

        void Build()
        {
            foreach (Transform child in content) Destroy(child.gameObject);
            roomChunkImages.Clear();
            roomIconRows.Clear();
            roomCenters.Clear();

            foreach (var room in layout.Rooms)
            {
                var minX = room.Chunks.Min(c => c.X);
                var minY = room.Chunks.Min(c => c.Y);
                var images = new List<Image>();

                foreach (var chunk in room.Chunks)
                {
                    var go = new GameObject($"Chunk_{room.Id}_{chunk.X}_{chunk.Y}", typeof(RectTransform));
                    go.transform.SetParent(content, false);
                    var rt = (RectTransform)go.transform;
                    rt.sizeDelta = new Vector2(chunkSize, chunkSize);
                    rt.anchoredPosition = RoomLocalOrigin(minX, minY) + new Vector2(chunk.X - minX, chunk.Y - minY) * chunkSize;
                    var image = go.AddComponent<Image>();
                    image.raycastTarget = false;
                    images.Add(image);
                }

                roomChunkImages[room.Id] = images;

                var centerLocal = new Vector2(
                    room.Chunks.Average(c => c.X - minX + 0.5f),
                    room.Chunks.Average(c => c.Y - minY + 0.5f)) * chunkSize + RoomLocalOrigin(minX, minY);
                roomCenters[room.Id] = centerLocal;

                var iconRowGO = new GameObject($"Icons_{room.Id}", typeof(RectTransform));
                iconRowGO.transform.SetParent(content, false);
                var iconRow = (RectTransform)iconRowGO.transform;
                iconRow.anchoredPosition = centerLocal;
                roomIconRows[room.Id] = iconRow;
            }

            if (showDoorConnectors) BuildDoorConnectors();

            var dotGO = new GameObject("PlayerDot", typeof(RectTransform));
            dotGO.transform.SetParent(content, false);
            playerDot = (RectTransform)dotGO.transform;
            playerDot.sizeDelta = new Vector2(iconSize, iconSize);
            var dotImage = dotGO.AddComponent<Image>();
            dotImage.color = Color.cyan;
            dotImage.raycastTarget = false;

            RecenterOnCurrentRoom();
            RefreshStatesAndIcons();
        }

        void BuildDoorConnectors()
        {
            foreach (var door in layout.Doors)
            {
                var roomA = layout.Rooms.First(r => r.Id == door.RoomA);
                var minXA = roomA.Chunks.Min(c => c.X);
                var minYA = roomA.Chunks.Min(c => c.Y);
                var chunkAPos = RoomLocalOrigin(minXA, minYA) + new Vector2(door.ChunkA.X - minXA, door.ChunkA.Y - minYA) * chunkSize;

                var go = new GameObject($"Door_{door.RoomA}_{door.RoomB}", typeof(RectTransform));
                go.transform.SetParent(content, false);
                var rt = (RectTransform)go.transform;
                var isHorizontal = door.SideA == Dir.N || door.SideA == Dir.S;
                rt.sizeDelta = isHorizontal ? new Vector2(chunkSize * 0.4f, chunkGap) : new Vector2(chunkGap, chunkSize * 0.4f);

                var offset = door.SideA switch
                {
                    Dir.N => new Vector2(chunkSize / 2f, chunkSize + chunkGap / 2f),
                    Dir.S => new Vector2(chunkSize / 2f, -chunkGap / 2f),
                    Dir.E => new Vector2(chunkSize + chunkGap / 2f, chunkSize / 2f),
                    _ => new Vector2(-chunkGap / 2f, chunkSize / 2f),
                };
                rt.anchoredPosition = chunkAPos + offset;

                var image = go.AddComponent<Image>();
                image.color = doorConnectorColor;
                image.raycastTarget = false;
            }
        }

        Vector2 RoomLocalOrigin(int minChunkX, int minChunkY) =>
            new Vector2(minChunkX, minChunkY) * (chunkSize + chunkGap);

        void RecenterOnCurrentRoom()
        {
            if (transitionController == null || !roomCenters.TryGetValue(transitionController.CurrentRoomId, out var center)) return;
            content.anchoredPosition = -center;
        }

        void RefreshStatesAndIcons()
        {
            if (layout == null) return;

            var visited = new HashSet<int>(layout.Rooms.Where(r => IsVisited(r.Id)).Select(r => r.Id));
            var seen = new HashSet<int>();
            foreach (var door in layout.Doors)
            {
                if (visited.Contains(door.RoomA) && !visited.Contains(door.RoomB)) seen.Add(door.RoomB);
                if (visited.Contains(door.RoomB) && !visited.Contains(door.RoomA)) seen.Add(door.RoomA);
            }

            foreach (var room in layout.Rooms)
            {
                var state = ComputeState(room, visited, seen);
                var color = StateColor(state);
                foreach (var image in roomChunkImages[room.Id]) image.color = color;

                var showIcons = revealAll || state == MinimapRoomState.Visited || state == MinimapRoomState.Current;
                RefreshIcons(room, showIcons);
            }
        }

        MinimapRoomState ComputeState(RoomNode room, HashSet<int> visited, HashSet<int> seen)
        {
            if (revealAll) return room.Id == transitionController?.CurrentRoomId ? MinimapRoomState.Current : MinimapRoomState.Visited;
            if (transitionController != null && room.Id == transitionController.CurrentRoomId) return MinimapRoomState.Current;
            if (visited.Contains(room.Id)) return MinimapRoomState.Visited;
            if (revealBossRoom && room.Id == layout.BossRoomId) return MinimapRoomState.BossHint;
            if (seen.Contains(room.Id)) return MinimapRoomState.Seen;
            return MinimapRoomState.Hidden;
        }

        Color StateColor(MinimapRoomState state)
        {
            switch (state)
            {
                case MinimapRoomState.Seen: return seenColor;
                case MinimapRoomState.BossHint: return bossHintColor;
                case MinimapRoomState.Visited: return visitedColor;
                case MinimapRoomState.Current: return currentColor;
                default: return new Color(0, 0, 0, 0); // Hidden: not drawn
            }
        }

        bool IsVisited(int roomId) =>
            builder != null && builder.RoomsById.TryGetValue(roomId, out var rc) && rc.State != RoomState.Unvisited;

        void RefreshIcons(RoomNode room, bool show)
        {
            var row = roomIconRows[room.Id];
            foreach (Transform child in row) Destroy(child.gameObject);
            if (!show) return;

            var slots = new (bool present, Color color)[]
            {
                (RoomActivity.GetCount(room.Id, RoomMemberKind.Enemy) > 0, enemyIconColor),
                (RoomActivity.GetCount(room.Id, RoomMemberKind.Loot) > 0, lootIconColor),
                (RoomActivity.GetCount(room.Id, RoomMemberKind.Treasure) > 0, treasureIconColor),
                (RoomActivity.GetCount(room.Id, RoomMemberKind.Shop) > 0, shopIconColor),
                (RoomActivity.GetCount(room.Id, RoomMemberKind.Npc) > 0, npcIconColor),
                (RoomActivity.GetCount(room.Id, RoomMemberKind.Boss) > 0, bossIconColor),
            };

            var active = slots.Where(s => s.present).ToList();
            var startX = -(active.Count - 1) * iconSize / 2f;
            for (var i = 0; i < active.Count; i++)
            {
                var go = new GameObject("Icon", typeof(RectTransform));
                go.transform.SetParent(row, false);
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(iconSize, iconSize);
                rt.anchoredPosition = new Vector2(startX + i * iconSize, 0);
                var icon = go.AddComponent<Image>();
                icon.color = active[i].color;
                icon.raycastTarget = false;
            }
        }

        void UpdatePlayerDot()
        {
            if (playerDot == null || player == null || transitionController == null) return;
            if (!builder.RoomsById.TryGetValue(transitionController.CurrentRoomId, out var currentRoom)) return;

            var minX = currentRoom.Room.Chunks.Min(c => c.X);
            var minY = currentRoom.Room.Chunks.Min(c => c.Y);
            var chunkPos = new Vector2(player.position.x, player.position.y) / ShapeDefinition.ChunkTiles;
            playerDot.anchoredPosition = RoomLocalOrigin(minX, minY) + (chunkPos - new Vector2(minX, minY)) * chunkSize;
        }
    }
}
