using System;
using System.Collections.Generic;
using ChainNet.Data;
using ChainNet.Gameplay;
using UnityEngine;

namespace ChainNet.RoguelikeMap
{
    [Serializable]
    public class MapNodeRuntime
    {
        public string nodeId;
        public MapNodeType nodeType;
        public Vector2 mapPosition;
        public List<MapNodeRuntime> connectedNodes = new();
        public bool isAvailable;
        public bool isCompleted;
        public TeamData enemyTeam;
        public CourtData court;
        public List<RewardData> possibleRewards = new();
    }

    public class MapGenerator
    {
        private readonly MapNodeType[] weightedNodes =
        {
            MapNodeType.PickupGame,
            MapNodeType.PickupGame,
            MapNodeType.RivalGame,
            MapNodeType.RivalGame,
            MapNodeType.DirtyCourt,
            MapNodeType.MoneyGame,
            MapNodeType.Trainer,
            MapNodeType.SneakerShop,
            MapNodeType.TapeDealer,
            MapNodeType.OpenGym,
            MapNodeType.BackAlley,
            MapNodeType.CornerStore,
            MapNodeType.MiniBossCourt
        };

        public List<List<MapNodeRuntime>> Generate(int layers = 8)
        {
            var result = new List<List<MapNodeRuntime>>(layers);
            for (var layer = 0; layer < layers; layer++)
            {
                var row = new List<MapNodeRuntime>();
                var count = layer == 0 || layer == layers - 1 ? 1 : UnityEngine.Random.Range(2, 5);
                for (var i = 0; i < count; i++)
                {
                    var type = layer == 0
                        ? MapNodeType.OpenGym
                        : (layer == layers - 1 ? MapNodeType.BossCourt : weightedNodes[UnityEngine.Random.Range(0, weightedNodes.Length)]);

                    row.Add(new MapNodeRuntime
                    {
                        nodeId = $"L{layer}_N{i}",
                        nodeType = type,
                        mapPosition = new Vector2(layer, i),
                        isAvailable = layer == 0
                    });
                }

                result.Add(row);
            }

            for (var layer = 0; layer < result.Count - 1; layer++)
            {
                foreach (var node in result[layer])
                {
                    var links = UnityEngine.Random.Range(1, Math.Min(3, result[layer + 1].Count) + 1);
                    for (var i = 0; i < links; i++)
                    {
                        var target = result[layer + 1][UnityEngine.Random.Range(0, result[layer + 1].Count)];
                        if (!node.connectedNodes.Contains(target))
                        {
                            node.connectedNodes.Add(target);
                        }
                    }
                }
            }

            return result;
        }
    }
}
