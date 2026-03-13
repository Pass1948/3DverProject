using UnityEngine;

public class PeakCarryable : MonoBehaviour
{
    [Header("아이템 집기 기능 : 한번 클릭으로 집고 손에 든 상태는 버튼을 떼도 유지됨")]
    [SerializeField] private Vector3 heldLocalPosition = new Vector3(0.25f, -0.18f, 0.55f);
    [SerializeField] private Vector3 heldLocalEuler = Vector3.zero;
    [SerializeField] private bool disableCollisionWhileHeld = true;

    private Rigidbody itemRigidbody;
    private Collider[] itemColliders;
    private Transform cachedParent;

    private bool isHeld = false;

    private void Awake()
    {
        itemRigidbody = GetComponent<Rigidbody>();
        itemColliders = GetComponentsInChildren<Collider>(true);
    }

    public void Pickup(Transform holdPoint)
    {
        if (holdPoint == null)
            return;

        cachedParent = transform.parent;
        transform.SetParent(holdPoint, false);
        transform.localPosition = heldLocalPosition;
        transform.localRotation = Quaternion.Euler(heldLocalEuler);

        if (itemRigidbody != null)
        {
            itemRigidbody.useGravity = false;
            itemRigidbody.isKinematic = true;
            itemRigidbody.linearVelocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
        }

        if (disableCollisionWhileHeld)
            SetCollidersEnabled(false);
        isHeld = true;
        GameManager.Event.Publish(EventType.InfoButtonText, isHeld);
    }

    public void Drop(Vector3 releaseVelocity)
    {
        transform.SetParent(cachedParent, true);

        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = false;
            itemRigidbody.useGravity = true;
            itemRigidbody.linearVelocity = releaseVelocity;
            itemRigidbody.angularVelocity = Vector3.zero;
        }

        if (disableCollisionWhileHeld)
            SetCollidersEnabled(true);

        isHeld = false;
        GameManager.Event.Publish(EventType.InfoButtonText, isHeld);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (itemColliders == null)
            return;

        for (int i = 0; i < itemColliders.Length; i++)
            itemColliders[i].enabled = enabled;
    }
}
