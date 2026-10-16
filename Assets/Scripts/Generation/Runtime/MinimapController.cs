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
    /// chunk as a local origin inside the shared (gapped) chunk grid. All positions are chunk *centers* -
    /// RectTransforms default to a center pivot, so corner-based math here would visibly offset everything
    /// toward one corner (which is what made icons/dot look pinned to a chunk's corner before this fix).
    /// Icons are pooled per room (fixed 6 slots, toggled) rather than destroyed/recreated on every refresh.
    /// </summary>
    public sealed class MinimapController : MonoBehaviour
    {
        public DungeonBuilder builder;
        public RoomTransitionController transitionController;
        public Transform player;

        [Header("Layout")]
        public RectTransform content;
        public float chunkSize = 26f;
        public float chunkGap = 3f;
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

        static readonly RoomMemberKind[] IconKinds =
        {
            RoomMemberKind.Enemy, RoomMemberKind.Loot, RoomMemberKind.Treasure,
            RoomMemberKind.Shop, RoomMemberKind.Npc, RoomMemberKind.Boss,
        };

        readonly Dictionary<int, List<Image>> roomChunkImages = new Dictionary<int, List<Image>>();
        readonly Dictionary<int, Image[]> roomIconPool = new Dictionary<int, Image[]>();
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
            // Debug.isDebugBuild covers the editor and development builds without the deprecated DEVELOPMENT_BUILD directive.
            if (Debug.isDebugBuild && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                revealAll = !revealAll;
                RefreshStatesAndIcons();
            }
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
            roomIconPool.Clear();
            roomCenters.Clear();

            foreach (var room in layout.Rooms)
            {
                var minX = room.Chunks.Min(c => c.X);
                var minY = room.Chunks.Min(c => c.Y);
                var images = new List<Image>();

                foreach (var chunk in room.Chunks)
                {
                    var image = CreateImage($"Chunk_{room.Id}_{chunk.X}_{chunk.Y}", content, new Vector2(chunkSize, chunkSize));
                    image.rectTransform.anchoredPosition = ChunkCenterLocal(minX, minY, chunk.X, chunk.Y);
                    images.Add(image);
                }
                roomChunkImages[room.Id] = images;

                var centerLocal = new Vector2(
                    (float)room.Chunks.Average(c => c.X - minX),
                    (float)room.Chunks.Average(c => c.Y - minY)) * chunkSize + RoomLocalOrigin(minX, minY) + Vector2.one * chunkSize / 2f;
                roomCenters[room.Id] = centerLocal;

                var iconRowGO = new GameObject($"Icons_{room.Id}", typeof(RectTransform));
                iconRowGO.transform.SetParent(content, false);
                ((RectTransform)iconRowGO.transform).anchoredPosition = centerLocal;

                var pool = new Image[IconKinds.Length];
                var startX = -(IconKinds.Length - 1) * iconSize / 2f;
                for (var i = 0; i < IconKinds.Length; i++)
                {
                    var icon = CreateImage("Icon", iconRowGO.transform, new Vector2(iconSize, iconSize));
                    icon.rectTransform.anchoredPosition = new Vector2(startX + i * iconSize, 0);
                    icon.gameObject.SetActive(false);
                    pool[i] = icon;
                }
                roomIconPool[room.Id] = pool;
            }

            if (showDoorConnectors) BuildDoorConnectors();

            var dotImage = CreateImage("PlayerDot", content, new Vector2(iconSize, iconSize));
            dotImage.color = Color.cyan;
            playerDot = dotImage.rectTransform;

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
                var chunkACenter = ChunkCenterLocal(minXA, minYA, door.ChunkA.X, door.ChunkA.Y);

                var isHorizontal = door.SideA == Dir.N || door.SideA == Dir.S;
                var image = CreateImage($"Door_{door.RoomA}_{door.RoomB}", content,
                    isHorizontal ? new Vector2(chunkSize * 0.4f, chunkGap) : new Vector2(chunkGap, chunkSize * 0.4f));
                image.color = doorConnectorColor;

                var halfStep = (chunkSize + chunkGap) / 2f;
                var offset = door.SideA switch
                {
                    Dir.N => new Vector2(0, halfStep),
                    Dir.S => new Vector2(0, -halfStep),
                    Dir.E => new Vector2(halfStep, 0),
                    _ => new Vector2(-halfStep, 0),
                };
                image.rectTransform.anchoredPosition = chunkACenter + offset;
            }
        }

        static Image CreateImage(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        /// <summary>Pixel-space center of one chunk, in a room-independent grid where different rooms' chunks
        /// are chunkGap apart but a single room's own chunks are exactly chunkSize apart (no seam).</summary>
        Vector2 ChunkCenterLocal(int roomMinX, int roomMinY, int chunkX, int chunkY) =>
            RoomLocalOrigin(roomMinX, roomMinY) + new Vector2(chunkX - roomMinX, chunkY - roomMinY) * chunkSize + Vector2.one * chunkSize / 2f;

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

        /// <summary>HasBeenVisited (not State != Unvisited): a room can be pre-warmed - rendered active a tile
        /// early so there's no pop - well before the player has actually set foot in it. Using Prewarm's state
        /// here was exactly what made the minimap and fog of war reveal a room before the player had entered it.</summary>
        bool IsVisited(int roomId) =>
            builder != null && builder.RoomsById.TryGetValue(roomId, out var rc) && rc.HasBeenVisited;

        void RefreshIcons(RoomNode room, bool show)
        {
            var pool = roomIconPool[room.Id];
            for (var i = 0; i < IconKinds.Length; i++)
            {
                var present = show && RoomActivity.GetCount(room.Id, IconKinds[i]) > 0;
                pool[i].gameObject.SetActive(present);
            }

            // Re-pack visible icons so removed ones don't leave a gap in the row.
            var visibleIndex = 0;
            var visibleCount = pool.Count(p => p.gameObject.activeSelf);
            var startX = -(visibleCount - 1) * iconSize / 2f;
            foreach (var icon in pool)
            {
                if (!icon.gameObject.activeSelf) continue;
                icon.color = IconColor(System.Array.IndexOf(pool, icon));
                icon.rectTransform.anchoredPosition = new Vector2(startX + visibleIndex * iconSize, 0);
                visibleIndex++;
            }
        }

        Color IconColor(int kindIndex)
        {
            switch (IconKinds[kindIndex])
            {
                case RoomMemberKind.Enemy: return enemyIconColor;
                case RoomMemberKind.Loot: return lootIconColor;
                case RoomMemberKind.Treasure: return treasureIconColor;
                case RoomMemberKind.Shop: return shopIconColor;
                case RoomMemberKind.Npc: return npcIconColor;
                default: return bossIconColor;
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
