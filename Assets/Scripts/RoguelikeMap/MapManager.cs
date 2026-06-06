using System.Collections.Generic;
using ChainNet.Core;
using ChainNet.Data;
using ChainNet.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChainNet.RoguelikeMap
{
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private string matchSceneName = "Match";
        [SerializeField] private CourtData defaultCourt;

        private readonly List<MapNodeRuntime> allNodes = new();

        public void InitializeMap(List<MapNodeRuntime> nodes)
        {
            allNodes.Clear();
            allNodes.AddRange(nodes);
        }

        public bool TrySelectNode(MapNodeRuntime node)
        {
            if (node == null || !node.isAvailable || node.isCompleted)
                return false;

            ResolveNode(node);
            return true;
        }

        public void ResolveNode(MapNodeRuntime node)
        {
            node.isCompleted = true;
            foreach (var connected in node.connectedNodes)
                connected.isAvailable = true;

            if (IsMatchNode(node.nodeType))
                LaunchMatchForNode(node);
            else
                ResolveNonMatchNode(node);

            Save.SaveManager.Instance?.SaveRun(RunManager.Instance?.CurrentRun);
        }

        private void LaunchMatchForNode(MapNodeRuntime node)
        {
            var run = RunManager.Instance?.CurrentRun;
            if (run == null) return;

            TeamRuntime enemy;
            if (node.enemyTeam != null)
                enemy = new TeamRuntime(node.enemyTeam);
            else
            {
                Debug.LogWarning($"Node {node.nodeId} has no enemyTeam assigned — match will use empty team.");
                enemy = new TeamRuntime(ScriptableObject.CreateInstance<TeamData>());
            }

            var court = node.court ?? defaultCourt;
            MatchContext.Instance?.PrepareMatch(run.playerTeam, enemy, court, node,
                node.nodeType == MapNodeType.BossCourt);

            SceneManager.LoadScene(matchSceneName);
        }

        private static void ResolveNonMatchNode(MapNodeRuntime node)
        {
            Debug.Log($"Resolved non-match node: {node.nodeType}");
            // CircuitMapController finds NodeEventController and shows the modal.
        }

        private static bool IsMatchNode(MapNodeType type)
        {
            return type is MapNodeType.PickupGame
                or MapNodeType.RivalGame
                or MapNodeType.DirtyCourt
                or MapNodeType.MiniBossCourt
                or MapNodeType.BossCourt;
        }
    }
}
