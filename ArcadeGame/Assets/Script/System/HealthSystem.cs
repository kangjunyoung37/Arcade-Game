using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    private float _currentHealth;
    public bool isInvincible = false;
    public Action<float, float> onHealthChange;
    public Action onDied;

    private void OnEnable()
    {
        _currentHealth = maxHealth;
        onHealthChange?.Invoke(_currentHealth, maxHealth);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_currentHealth <= 0) return;
        _currentHealth -= damageInfo.damage;
        onHealthChange?.Invoke(_currentHealth, maxHealth);
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            //onDied?.Invoke();
        }
    }
}
