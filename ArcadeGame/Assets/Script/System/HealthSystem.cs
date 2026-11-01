using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    private float _currentHealth;
    public bool isInvincible = false;
    public Action<float, float> onHealthChange;
    public Action onDied;
    public event Action<DamageInfo> DamageReceived;
    public bool IsAlive => _currentHealth > 0f;

    private void OnEnable()
    {
        _currentHealth = maxHealth;
        onHealthChange?.Invoke(_currentHealth, maxHealth);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_currentHealth <= 0 || isInvincible) return;

        if(damageInfo.damage <= 0f || float.IsNaN(damageInfo.damage) || float.IsInfinity(damageInfo.damage))
            return;
        
        _currentHealth -= damageInfo.damage;
        
        onHealthChange?.Invoke(_currentHealth, maxHealth);
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            //onDied?.Invoke();
        }
        DamageReceived?.Invoke(damageInfo);
    }
}
