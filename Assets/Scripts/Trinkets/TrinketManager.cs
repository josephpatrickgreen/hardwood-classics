using ChainNet.Data;
using ChainNet.Gameplay;
using UnityEngine;

namespace ChainNet.Trinkets
{
    public class TrinketManager : MonoBehaviour
    {
        public void EquipTrinket(PlayerRuntime player, TrinketData trinket)
        {
            if (player == null || trinket == null || player.equippedTrinkets.Contains(trinket))
            {
                return;
            }

            player.equippedTrinkets.Add(trinket);
        }
    }
}
