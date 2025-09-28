using System;
using UnityEngine;


public class LifeSystem : Statistics
{


    /// <summary>
    /// Se lanza cuando la vida llega a cero.
    /// </summary>
    public event Action OnDeath;

protected override void Awake()
{
    base.Awake();
    // Suscribirse al evento de llegar al mínimo
    OnMinReached += HandleDeath;
}

    /// <summary>
    /// Aplica daño; si amount &gt; 0, reduce vida.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        Subtract(amount);
    }

    /// <summary>
    /// Cura hasta maxValue.
    /// </summary>
    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        Add(amount);
    }

    /// <summary>
    /// Intenta aplicar daño; devuelve true si había suficiente vida.
    /// </summary>
    public bool CanTakeDamage(float amount)
    {
        if (amount <= 0f) return false;
        if (Current > amount)
        {
            Subtract(amount);
            return true;
        }
        return false;
    }

    private void HandleDeath()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}