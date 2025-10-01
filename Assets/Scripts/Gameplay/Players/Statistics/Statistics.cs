using System;
using UnityEngine;

public class Statistics : MonoBehaviour
{
    [Header("Stats Settings")]
    [SerializeField] private float maxValue     = 100f;
    [SerializeField] private float minValue     = 0f;
    [SerializeField] private float currentValue = 100f;

    /// <summary>
    /// Se lanza cada vez que cambia Current (valor actual).
    /// </summary>
    public event Action<float> OnValueChanged;

    /// <summary>
    /// Se lanza cuando Current llega a minValue.
    /// </summary>
    public event Action OnMinReached;

    public float Max        => maxValue;
    public float Min        => minValue;
    public float Current    => currentValue;
    public float Percentage => Mathf.Clamp01(currentValue / maxValue);

    protected virtual void Awake()
    {
        // Asegura que currentValue esté en rangos válidos
        currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
    }

    /// <summary>
    /// Lleva Current a maxValue.
    /// </summary>
    public virtual void SetToMax()
    {
        currentValue = maxValue;
        OnValueChanged?.Invoke(currentValue);
    }

    /// <summary>
    /// Lleva Current a minValue y dispara OnMinReached.
    /// </summary>
    public virtual void SetToMin()
    {
        currentValue = minValue;
        OnValueChanged?.Invoke(currentValue);
        OnMinReached?.Invoke();
    }

    /// <summary>
    /// Incrementa Current hasta maxValue.
    /// </summary>
    public virtual void Add(float amount)
    {
        if (amount <= 0f) return;

        currentValue = Mathf.Min(currentValue + amount, maxValue);
        OnValueChanged?.Invoke(currentValue);
    }

    /// <summary>
    /// Decrementa Current hasta minValue y dispara OnMinReached si llega a 0.
    /// </summary>
    public virtual void Subtract(float amount)
    {
        if (amount <= 0f) return;

        currentValue = Mathf.Max(currentValue - amount, minValue);
        OnValueChanged?.Invoke(currentValue);

        if (currentValue <= minValue)
            OnMinReached?.Invoke();
    }
}