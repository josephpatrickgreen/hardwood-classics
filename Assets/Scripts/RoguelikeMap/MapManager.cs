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

        private readonly List<MapNodeRuntime> allNodes = new();

        public void InitializeMap(List<MapNodeRuntime> nodes)
        {
            allNodes.Clear();
            allNodes.AddRange(nodes);
        }

        public bool TrySelectNode(MapNodeRuntime node)
        {
            if (node == null || !node.isAvailable || node.isCompleted)
            {
                return false;
            }

            ResolveNode(node);
            return true;
        }

        public void ResolveNode(MapNodeRuntime node)
        {
            node.isCompleted = true;
            foreach (var connected in node.connectedNodes)
            {
                connected.isAvailable = true;
            }

            if (IsMatchNode(node.nodeType))
            {
                SceneManager.LoadScene(matchSceneName);
            }
            else
            {
                ResolveNonMatchNode(node);
            }

            Save.SaveManager.Instance?.SaveRun(RunManager.Instance.CurrentRun);
        }

        private static void ResolveNonMatchNode(MapNodeRuntime node)
        {
            // Placeholder hooks for prototype UIs: trainer/shop/recruit/event/heal.
            Debug.Log($"Resolved non-match node: {node.nodeType}");
        }

        private static bool IsMatchNode(MapNodeType type)
        {
            return type is MapNodeType.PickupGame
                or MapNodeType.RivalGame
                or MapNodeType.DirtyCourt
                or MapNodeType.MoneyGame
                or MapNodeType.MiniBossCourt
                or MapNodeType.BossCourt;
        }
    }
}
