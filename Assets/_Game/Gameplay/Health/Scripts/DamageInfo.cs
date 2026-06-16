using UnityEngine;

namespace Seventh.Gameplay.Health
{
    public enum HitIntensity
    {
        Light,
        Medium,
        Heavy
    }

    public struct DamageInfo
    {
        public int DamageAmount;
        public float KnockbackForce;
        public Vector2 HitDirection;
        public GameObject Attacker;
        public HitIntensity Intensity;
        public bool IsSilent;

        public DamageInfo(int damageAmount, float knockbackForce, Vector2 hitDirection, GameObject attacker, HitIntensity intensity = HitIntensity.Light, bool isSilent = false)
        {
            DamageAmount = damageAmount;
            KnockbackForce = knockbackForce;
            HitDirection = hitDirection;
            Attacker = attacker;
            Intensity = intensity;
            IsSilent = isSilent;
        }
    }
}
