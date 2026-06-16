using UnityEngine;

namespace Seventh.Core.Events
{
    public readonly struct PlayerHealthChangedEvent
    {
        public readonly int CurrentHealth;
        public readonly int MaxHealth;

        public PlayerHealthChangedEvent(int current, int max)
        {
            CurrentHealth = current;
            MaxHealth = max;
        }
    }

    public readonly struct PlayerDashCooldownEvent
    {
        public readonly bool IsOnCooldown;
        public readonly float CooldownMaxTime;
        public readonly float CooldownTimeRemaining;

        public PlayerDashCooldownEvent(bool isOnCooldown, float cooldownTimeRemaining, float cooldownMaxTime)
        {
            IsOnCooldown = isOnCooldown;
            CooldownTimeRemaining = cooldownTimeRemaining;
            CooldownMaxTime = cooldownMaxTime;
        }
    }
}
