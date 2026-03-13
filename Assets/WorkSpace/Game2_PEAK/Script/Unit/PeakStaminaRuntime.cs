using UnityEngine;

[System.Serializable]
public class PeakStaminaRuntime
{
    private float current;
    private float extra;
    private float recoverDelayTimer;
    private bool consumedThisStep;

    public float Current => current;
    public float Extra => extra;
    public float Total => current + extra;

    public void Initialize(float maxStamina, float startExtraStamina)
    {
        current = Mathf.Max(0f, maxStamina);
        extra = Mathf.Max(0f, startExtraStamina);
        recoverDelayTimer = 0f;
        consumedThisStep = false;
    }

    public void BeginStep()
    {
        consumedThisStep = false;
    }

    public void TickTimers(float dt)
    {
        if (recoverDelayTimer > 0f)
            recoverDelayTimer -= dt;
    }

    public void Clamp(float maxStamina)
    {
        current = Mathf.Clamp(current, 0f, maxStamina);
        extra = Mathf.Max(0f, extra);
    }

    public bool Consume(float amount)
    {
        if (amount <= 0f)
            return true;

        if (Total < amount)
            return false;

        consumedThisStep = true;

        if (current >= amount)
        {
            current -= amount;
            return true;
        }

        float remain = amount - current;
        current = 0f;
        extra = Mathf.Max(0f, extra - remain);
        return true;
    }

    public void Recover(float dt, float maxStamina, float recoverPerSecond, float recoverDelay, bool recoveryBlocked)
    {
        Clamp(maxStamina);

        if (recoveryBlocked || consumedThisStep)
        {
            recoverDelayTimer = recoverDelay;
            return;
        }

        if (recoverDelayTimer > 0f)
            return;

        if (current < maxStamina)
            current = Mathf.MoveTowards(current, maxStamina, recoverPerSecond * dt);
    }
}
