using System;
using System.Collections.Generic;
using ChainNet.Data;
using ChainNet.Events;
using ChainNet.Gameplay;
using UnityEngine;

namespace ChainNet.Characters
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float sprintMultiplier = 1.35f;
        [SerializeField] private FoulManager foulManager;
        [SerializeField] private bool dirtyModifierHeld;
        private CharacterController controller;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
            var speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
            controller?.Move(move * (speed * Time.deltaTime));

            dirtyModifierHeld = Input.GetKey(KeyCode.LeftControl);

            if (Input.GetKeyDown(KeyCode.Space)) Shoot();
            if (Input.GetKeyDown(KeyCode.E)) Pass();
            if (Input.GetKeyDown(KeyCode.Q)) UseDribbleMove();
            if (Input.GetKeyDown(KeyCode.R)) UseSpecial();
            if (Input.GetKeyDown(KeyCode.F)) CallFoul();
            if (Input.GetKeyDown(KeyCode.C)) Steal();
            if (Input.GetKeyDown(KeyCode.V)) BlockOrRebound();
        }

        public void Pass() { }
        public void Shoot() { }
        public void UseDribbleMove() { }
        public void Steal() { }
        public void BlockOrRebound() { }
        public void UseSpecial() { }

        public bool CallFoul()
        {
            if (foulManager == null)
            {
                return false;
            }

            // Hook runtime caller/defender references in scene wiring.
            return false;
        }
    }

    public class SpecialController : MonoBehaviour
    {
        public static event Action<PlayerRuntime> OnSpecialUsed;

        private readonly Dictionary<PlayerRuntime, float> cooldowns = new();

        public bool CanUseSpecial(PlayerRuntime player)
        {
            return player?.data?.special != null && (!cooldowns.ContainsKey(player) || cooldowns[player] <= 0f);
        }

        public void UseSpecial(PlayerRuntime player)
        {
            if (!CanUseSpecial(player))
            {
                return;
            }

            cooldowns[player] = player.data.special.cooldownSeconds;
            player.specialCooldownRemaining = player.data.special.cooldownSeconds;
            ApplyPrototypeSpecialEffect(player);
            OnSpecialUsed?.Invoke(player);
        }

        public void TickCooldowns(float deltaTime)
        {
            var keys = new List<PlayerRuntime>(cooldowns.Keys);
            foreach (var player in keys)
            {
                cooldowns[player] = Mathf.Max(0f, cooldowns[player] - deltaTime);
                player.specialCooldownRemaining = cooldowns[player];
            }
        }

        public void ReduceCooldown(PlayerRuntime player, float amount)
        {
            if (!cooldowns.ContainsKey(player))
            {
                return;
            }

            cooldowns[player] = Mathf.Max(0f, cooldowns[player] - amount);
            player.specialCooldownRemaining = cooldowns[player];
        }

        private static void ApplyPrototypeSpecialEffect(PlayerRuntime player)
        {
            switch (player.data.special.specialType)
            {
                case SpecialType.YoYoPass:
                case SpecialType.BoomBoxBounce:
                    player.currentStats.vision += 1;
                    break;
                case SpecialType.PosterChild:
                case SpecialType.FenceRattler:
                    player.currentStats.finish += 1;
                    break;
                case SpecialType.ChainNetSniper:
                    player.currentStats.jumper += 1;
                    break;
            }
        }
    }
}
