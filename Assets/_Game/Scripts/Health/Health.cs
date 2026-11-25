using System;
using UnityEngine;

// HP for the player and the enemies. After a hit there's a short window where
// nothing can damage you again, so fights don't feel cheap.
public class Health : MonoBehaviour
{
    public int maxHP = 100;
    public float invincibleTime = 0.5f;

    public int HP { get; private set; }
    public bool IsDead => HP <= 0;

    public event Action<int, int> Changed;   // current, max
    public event Action<Vector2> Damaged;    // where the hit came from
    public event Action Died;

    private float lastHit = -99f;

    private void Awake()
    {
        HP = maxHP;
    }

    // false means the hit was ignored (already dead, or still invincible)
    public bool TakeDamage(int amount, Vector2 fromPosition)
    {
        if (IsDead || Time.time < lastHit + invincibleTime) return false;

        lastHit = Time.time;
        HP = Mathf.Max(0, HP - amount);

        Changed?.Invoke(HP, maxHP);
        Damaged?.Invoke(fromPosition);
        if (HP <= 0) Died?.Invoke();
        return true;
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        HP = Mathf.Min(maxHP, HP + amount);
        Changed?.Invoke(HP, maxHP);
    }

    public void ResetFull()
    {
        HP = maxHP;
        lastHit = -99f;
        Changed?.Invoke(HP, maxHP);
    }

    public void NotifyChanged()
    {
        Changed?.Invoke(HP, maxHP);
    }
}
