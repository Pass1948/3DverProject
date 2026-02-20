using UnityEngine;

public class EnterMuzzle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Slime"))
            return;
        if (!other.TryGetComponent<Slime>(out var item))
            return;
        var inv = GameManager.Unit.game1Player.inventory;
        if (inv.TryAdd(item.id, item.amount))
        {
            // 흡수되는 효과음
            GameManager.Sound.PlaySFX("Game1/SuctionSFX");
            Destroy(other.gameObject);
        }
        else
        {
            // 흡수 실패 효과음
            GameManager.Sound.PlaySFX("Game1/FoulSFX");
            other.attachedRigidbody?.AddForce(-transform.forward * 5f, ForceMode.VelocityChange);
        }
    }
}
