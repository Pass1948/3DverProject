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

    public void Initialize(float effectiveMaxStamina, float startExtraStamina)
    {
        current = Mathf.Max(0f, effectiveMaxStamina);
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

    // [수정] 현재 유효 최대 스태미나를 넘어가면 즉시 잘라냄
    public void ForceClampToEffectiveMax(float effectiveMaxStamina)
    {
        current = Mathf.Clamp(current, 0f, effectiveMaxStamina);
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

    // [수정] recover는 원래 최대치가 아니라 현재 패널티가 반영된 최대치까지만 진행
    public void Recover(float dt, float effectiveMaxStamina, float recoverPerSecond, float recoverDelay, bool recoveryBlocked)
    {
        ForceClampToEffectiveMax(effectiveMaxStamina);

        if (recoveryBlocked || consumedThisStep)
        {
            recoverDelayTimer = recoverDelay;
            return;
        }

        if (recoverDelayTimer > 0f)
            return;

        if (current < effectiveMaxStamina)
            current = Mathf.MoveTowards(current, effectiveMaxStamina, recoverPerSecond * dt);
    }
}
