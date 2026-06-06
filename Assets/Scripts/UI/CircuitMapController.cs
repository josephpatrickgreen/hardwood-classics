using System.Collections.Generic;
using ChainNet.Core;
using ChainNet.Data;
using ChainNet.Gameplay;
using ChainNet.RoguelikeMap;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChainNet.UI
{
    /// <summary>
    /// Drives the CircuitMap scene. Generates node buttons, draws connection lines,
    /// handles node selection, and bridges into MatchContext.
    /// </summary>
    public class CircuitMapController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform mapContainer;
        [SerializeField] private GameObject nodeButtonPrefab;
        [SerializeField] private GameObject lineRendererPrefab;

        [Header("Layout")]
        [SerializeField] private float columnSpacing = 160f;
        [SerializeField] private float rowSpacing = 120f;
        [SerializeField] private Vector2 mapOrigin = new(80f, 400f);

        [Header("Run Data (can be assigned in editor for testing)")]
        [SerializeField] private TeamData defaultStartTeam;
        [SerializeField] private CourtData defaultCourt;

        // ── Private state ─────────────────────────────────────────────────────
        private List<List<MapNodeRuntime>> mapLayers;
        private readonly Dictionary<string, GameObject> nodeObjects = new();
        private MapNodeRuntime pendingNode;

        private void Start()
        {
            EnsureRunExists();
            BuildMapUI();
        }

        // ── Initialization ────────────────────────────────────────────────────
        private void EnsureRunExists()
        {
            if (RunManager.Instance?.CurrentRun != null) return;

            if (defaultStartTeam == null)
            {
                Debug.LogWarning("CircuitMapController: No RunManager run and no defaultStartTeam set.");
                return;
            }

            RunManager.Instance?.StartCircuitRun(defaultStartTeam);
        }

        // ── Map construction ──────────────────────────────────────────────────
        private void BuildMapUI()
        {
            var run = RunManager.Instance?.CurrentRun;
            if (run == null) return;

            // Group nodes by layer from nodeId (format "L{layer}_N{i}")
            mapLayers = RegroupByLayer(run.mapNodes);

            // Place node buttons
            for (var col = 0; col < mapLayers.Count; col++)
            {
                var layer = mapLayers[col];
                for (var row = 0; row < layer.Count; row++)
                {
                    var node = layer[row];
                    var pos = new Vector2(
                        mapOrigin.x + col * columnSpacing,
                        mapOrigin.y - row * rowSpacing);
                    CreateNodeButton(node, pos);
                }
            }

            // Draw connections
            for (var col = 0; col < mapLayers.Count - 1; col++)
            {
                foreach (var node in mapLayers[col])
                {
                    foreach (var next in node.connectedNodes)
                    {
                        DrawConnection(node, next);
                    }
                }
            }

            RefreshNodeVisuals();
        }

        private void CreateNodeButton(MapNodeRuntime node, Vector2 uiPos)
        {
            GameObject go;
            if (nodeButtonPrefab != null)
            {
                go = Instantiate(nodeButtonPrefab, mapContainer);
            }
            else
            {
                go = new GameObject($"Node_{node.nodeId}");
                go.transform.SetParent(mapContainer, false);
                var img = go.AddComponent<Image>();
                img.color = Color.gray;
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(80f, 80f);

                var label = new GameObject("Label");
                label.transform.SetParent(go.transform, false);
                var txt = label.AddComponent<TextMeshProUGUI>();
                txt.text = NodeLabel(node.nodeType);
                txt.fontSize = 10f;
                txt.alignment = TextAlignmentOptions.Center;
                var lrt = label.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            var rectTransform = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = uiPos;
            go.name = $"Node_{node.nodeId}";

            var button = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            var capturedNode = node;
            button.onClick.AddListener(() => OnNodeClicked(capturedNode));

            nodeObjects[node.nodeId] = go;
        }

        private void DrawConnection(MapNodeRuntime from, MapNodeRuntime to)
        {
            if (!nodeObjects.TryGetValue(from.nodeId, out var fromGo)) return;
            if (!nodeObjects.TryGetValue(to.nodeId, out var toGo)) return;

            if (lineRendererPrefab != null)
            {
                var line = Instantiate(lineRendererPrefab, mapContainer);
                // Line renderer setup should be done through the prefab;
                // just position it between nodes.
                line.transform.position = Vector3.Lerp(fromGo.transform.position, toGo.transform.position, 0.5f);
            }
        }

        // ── Selection ─────────────────────────────────────────────────────────
        private void OnNodeClicked(MapNodeRuntime node)
        {
            if (!node.isAvailable || node.isCompleted)
            {
                Debug.Log($"Node {node.nodeId} is not available.");
                return;
            }

            pendingNode = node;

            if (IsMatchNode(node.nodeType))
            {
                LaunchMatch(node);
            }
            else
            {
                // Non-match node: resolve inline (shop/trainer/etc)
                ResolveNonMatchNode(node);
            }
        }

        private void LaunchMatch(MapNodeRuntime node)
        {
            var run = RunManager.Instance?.CurrentRun;
            if (run == null) return;

            var enemy = node.enemyTeam != null ? new TeamRuntime(node.enemyTeam) : BuildTestEnemyTeam();
            var court = node.court ?? defaultCourt;

            MatchContext.Instance?.PrepareMatch(run.playerTeam, enemy, court, node,
                node.nodeType == MapNodeType.BossCourt);

            // Mark completed and unlock next nodes
            node.isCompleted = true;
            foreach (var next in node.connectedNodes)
                next.isAvailable = true;

            Save.SaveManager.Instance?.SaveRun(run);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Match");
        }

        private void ResolveNonMatchNode(MapNodeRuntime node)
        {
            node.isCompleted = true;
            foreach (var next in node.connectedNodes)
                next.isAvailable = true;

            Save.SaveManager.Instance?.SaveRun(RunManager.Instance?.CurrentRun);
            RefreshNodeVisuals();

            var eventCtrl = FindFirstObjectByType<NodeEventController>();
            eventCtrl?.ShowNodeEvent(node);
        }

        // ── Visuals ───────────────────────────────────────────────────────────
        public void RefreshNodeVisuals()
        {
            var run = RunManager.Instance?.CurrentRun;
            if (run == null) return;

            foreach (var node in run.mapNodes)
            {
                if (!nodeObjects.TryGetValue(node.nodeId, out var go)) continue;
                var img = go.GetComponent<Image>();
                if (img == null) continue;

                if (node.isCompleted)
                    img.color = new Color(0.4f, 0.4f, 0.4f);
                else if (node.isAvailable)
                    img.color = NodeColor(node.nodeType);
                else
                    img.color = new Color(0.2f, 0.2f, 0.2f);

                var btn = go.GetComponent<Button>();
                if (btn != null)
                    btn.interactable = node.isAvailable && !node.isCompleted;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static List<List<MapNodeRuntime>> RegroupByLayer(List<MapNodeRuntime> nodes)
        {
            var dict = new Dictionary<int, List<MapNodeRuntime>>();
            foreach (var n in nodes)
            {
                var layer = (int)n.mapPosition.x;
                if (!dict.ContainsKey(layer)) dict[layer] = new List<MapNodeRuntime>();
                dict[layer].Add(n);
            }

            var sorted = new List<List<MapNodeRuntime>>();
            var keys = new List<int>(dict.Keys);
            keys.Sort();
            foreach (var k in keys) sorted.Add(dict[k]);
            return sorted;
        }

        private static bool IsMatchNode(MapNodeType type)
        {
            return type is MapNodeType.PickupGame or MapNodeType.RivalGame or MapNodeType.DirtyCourt
                or MapNodeType.MoneyGame or MapNodeType.MiniBossCourt or MapNodeType.BossCourt;
        }

        private static Color NodeColor(MapNodeType type)
        {
            return type switch
            {
                MapNodeType.BossCourt => new Color(0.8f, 0.1f, 0.1f),
                MapNodeType.MiniBossCourt => new Color(0.8f, 0.4f, 0.1f),
                MapNodeType.DirtyCourt => new Color(0.5f, 0.3f, 0.1f),
                MapNodeType.Trainer => new Color(0.1f, 0.6f, 0.9f),
                MapNodeType.SneakerShop or MapNodeType.TapeDealer or MapNodeType.CornerStore =>
                    new Color(0.2f, 0.8f, 0.4f),
                MapNodeType.BarberShop => new Color(0.7f, 0.2f, 0.8f),
                _ => new Color(0.9f, 0.7f, 0.2f)
            };
        }

        private static string NodeLabel(MapNodeType type)
        {
            return type switch
            {
                MapNodeType.PickupGame => "Pickup",
                MapNodeType.RivalGame => "Rival",
                MapNodeType.DirtyCourt => "Dirty",
                MapNodeType.MoneyGame => "$$$",
                MapNodeType.Trainer => "Train",
                MapNodeType.SneakerShop => "Shop",
                MapNodeType.TapeDealer => "Tape",
                MapNodeType.OpenGym => "Gym",
                MapNodeType.BackAlley => "Alley",
                MapNodeType.BarberShop => "Cuts",
                MapNodeType.CornerStore => "Store",
                MapNodeType.MiniBossCourt => "Mini\nBoss",
                MapNodeType.BossCourt => "BOSS",
                _ => "?"
            };
        }

        private static TeamRuntime BuildTestEnemyTeam()
        {
            // Minimal placeholder for test runs without ScriptableObject wired enemies
            Debug.LogWarning("No enemy team assigned to node — using empty runtime.");
            return new TeamRuntime(ScriptableObject.CreateInstance<TeamData>());
        }
    }
}
