using JetBrains.Annotations;
using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Cinemachine.IInputAxisOwner.AxisDescriptor;

public class Controller : MonoBehaviour
{
    [Header("파티클")]
    [SerializeField] GameObject shootParticle;

    [Header("오브젝트 각도")]
    [SerializeField] Transform yawPivot; // 좌우  회전
    [SerializeField] Transform pitchPivot; // 상하 회전

    [Header("총구")]
    [SerializeField] Transform muzzle; // 총구 위치 (흡입 방향)
    [Header("빨려들어가는 지점")]
    [SerializeField] Transform suctionPoint; // 빨려들어가는 지점 (흡입 방향)
    [Header("흡입가능한 레이어")]
    [SerializeField] LayerMask suctionLayer; // 흡입 가능한 레이어

    [Header("회전각도 제한")]
    [SerializeField] float minPitch = -30f;
    [SerializeField] float maxPitch = 30f;
    [SerializeField] float minYaw = -60f;
    [SerializeField] float maxYaw = 60f;

    [Header("마우스 민감도")]
    [SerializeField] private float mouseSensitivity = 0.10f; //보통 0.05~0.2 사이로 튜닝

    [Header("움직임 속도")]
    [SerializeField] private float smooth = 18f; // 0이면 즉시 반영, 10~25 추천

    [Header("흡입 길이&각도")]
    [SerializeField] float range = 12f; // 흡입 최대 거리
    [SerializeField, Range(1f, 120f)] float coneAngle = 35f; // 흡입 각도 (원뿔 형태)

    [Header("흡입력")]
    [Tooltip("축 방향(앞쪽)으로 빨아들이는 힘")]
    [SerializeField] private float axisPullStrength = 25f;
    [Tooltip("원뿔 중심축으로 모으는 힘(이게 높을수록 '중심으로 말려 들어감'이 강해짐)")]
    [SerializeField] private float centerStrength = 55f;
    [Tooltip("흡입 시 회전력 세기")]
    [SerializeField] float swirlStrength = 18f; // 흡입 시 회전력 세기
    [Tooltip("흡입 시 속도 감쇠 세기")]
    [SerializeField] float damping = 6f; // 흡입 시 속도 감쇠 세기
    [Tooltip("흡입 시 최대 속도")]
    [SerializeField] float maxSpeed = 20f; // 흡입 시 최대 속도

    [Tooltip("중심파워")]
    [SerializeField] private float swirlNearCenterBoost = 1.5f; // 오브젝트가 빨려들어가는 지점에 가까울수록 회전력이 더 강해지는 효과 (1이면 변화 없음, 1보다 크면 가까울수록 회전력 증가)

    [SerializeField] float captureRadius = 0.5f; // 흡입 대상이 빨려들어가는 지점에 도달했을 때의 반경
    [SerializeField] bool enableCapture = false; // 흡입 대상이 빨려들어가는 지점에 도달했을 때 완전히 흡수되는 기능 활성화 여부

    [Header("배출")]
    [Tooltip("배출력")]
    [SerializeField] private float ejectImpulse = 8f;
    [Tooltip("배출간격")]
    [SerializeField] private float ejectInterval = 0.15f; // 0.15초마다 1개

    [Header("옵션")]
    [SerializeField] private bool lockCursor = true;    // 마우스 커서 잠금 여부


    // 마우스 입력값
    Vector2 lookDelta;
    float yaw;
    float pitch;

    // 초기 회전값 저장
    private Quaternion baseYawRot;
    private Quaternion basePitchRot;

    // 버튼 입력값
    bool isAttacking;
    bool isLookOff;
    bool isRelease;

    // (안정용) Attack 폴링
    private PlayerInput playerInput;
    private InputAction attackAction;
    private InputAction lookOffAction;
    private InputAction releaseAction;

    // 오버랩 스피어 버퍼
    private readonly Collider[] overlapBuffer = new Collider[100]; // 최대 16개까지 감지

    [Header("Debug Draw")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawForceRays = false; // 플레이 중 힘 방향 표시
    [SerializeField, Range(8, 64)] private int gizmoSegments = 24;

    [SerializeField] private float helixRadius = 0.6f;
    [SerializeField] private float helixLength = 4.0f;     // range보다 크면 range로 자동 제한
    [SerializeField, Range(1, 6)] private int helixTurns = 2;

    [SerializeField] private float forceRayScale = 0.03f;  // Debug.DrawRay 길이 스케일
    [SerializeField, Range(1, 20)] private int maxForceRaysPerFrame = 8;

    // 인벤토리 슬롯 선택 (0~3)
    private int selectedSlot = 0; // 0~3
    private float nextEjectTime = 0f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        attackAction = playerInput.actions["Attack"];
        lookOffAction = playerInput.actions["LookOff"];
        releaseAction = playerInput.actions["Release"];
    }

    private void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        // base 회전값 저장
        baseYawRot = yawPivot.localRotation;
        if (pitchPivot != null)
            basePitchRot = pitchPivot.localRotation;
    }

    private void Update()
    {
        CheckButtons();
        UpdateSuctionVfx();
        LockOff();
        HandleSlotHotkeys();
        Release(); 
        MouseRotate(); 
    }

    private void LateUpdate()
    {
        MoveMouse();
    }

    private void FixedUpdate()
    {
        if (isLookOff) return;
        if (!isAttacking) return;
        Fire();
    }
    //============= 입력 처리 ================
    private void CheckButtons()
    {
        isAttacking = attackAction.IsPressed(); 
        isLookOff = lookOffAction.IsPressed();
        isRelease = releaseAction.IsPressed();
    }

    //============= 총회전 ================
    public void OnLook(InputValue value)
    {
        lookDelta = value.Get<Vector2>();
    }

    private void MouseRotate()
    {
        if(isLookOff) { return; } // 록온 상태가 아닐 때는 회전하지 않음
        if (yawPivot == null || pitchPivot == null) { return; }
        
        yaw += lookDelta.x * mouseSensitivity; // 마우스 입력에 민감도 적용
        pitch -= lookDelta.y * mouseSensitivity;  

        // 회전각도 제한
        yaw = Mathf.Clamp(yaw, minYaw, maxYaw); // 좌우 회전 제한
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch); // 상하 회전 제한
    }

    private void MoveMouse()
    {
        if (isLookOff) { return; } // 록온 상태가 아닐 때는 회전하지 않음
        if (yawPivot == null || pitchPivot == null) { return; }
        float t = (smooth <= 0f) ? 1f : (1f - Mathf.Exp(-smooth * Time.deltaTime));

        Quaternion targetYaw = Quaternion.AngleAxis(yaw, Vector3.up) * baseYawRot;
        Quaternion targetPitch = Quaternion.AngleAxis(pitch, Vector3.right) * basePitchRot;

        yawPivot.localRotation = Quaternion.Slerp(yawPivot.localRotation, targetYaw, t);
        pitchPivot.localRotation = Quaternion.Slerp(pitchPivot.localRotation, targetPitch, t);

    }

    //============= 흡입 기능 ================

    private void Fire() 
    {
        if (muzzle == null || suctionPoint == null) return;

        Vector3 origin = muzzle.position;
        Vector3 axis = muzzle.forward.normalized;  // 흡입 방향 (총구의 정면)

        float cosLimit = Mathf.Cos(coneAngle * Mathf.Deg2Rad); // 원뿔 체크를 위한 코사인 값

        int count = Physics.OverlapSphereNonAlloc(origin, range, overlapBuffer, suctionLayer, QueryTriggerInteraction.Ignore);
        int drawn = 0;

        // 범위 내의 콜라이더들을 감지하여 흡입 처리
        for (int i = 0; i < count; i++)
        {
            Collider col = overlapBuffer[i]; 
            if (!col) continue;

            Rigidbody rb = col.attachedRigidbody; // Rigidbody가 없는 오브젝트는 흡입하지 않음
            if (!rb || rb.isKinematic) continue;

            //  최대 선속도 제한(폭주 방지)
            if (rb.maxLinearVelocity < maxSpeed)
                rb.maxLinearVelocity = maxSpeed;

            Vector3 objPos = rb.worldCenterOfMass;

            // 원뿔(콘) 체크
            Vector3 toObj = objPos - origin;// 총구에서 오브젝트까지의 벡터
            float dist = toObj.magnitude; // 총구에서 오브젝트까지의 거리
            if (dist < 0.0001f) continue;

            Vector3 dir = toObj / dist;// 총구에서 오브젝트까지의 방향(단위 벡터)   
            float align = Vector3.Dot(axis, dir); // 흡입 방향과 오브젝트 방향의 정렬 정도 (1에 가까울수록 정렬)
            if (align < cosLimit) continue;

            // 흡입 방향(목표점)
            Vector3 toSuck = (suctionPoint.position - objPos); // 오브젝트에서 빨려들어가는 지점까지의 벡터
            float d = toSuck.magnitude + 0.001f;

            // 거리/각도 기반 가중치
            float falloff = Mathf.Clamp01(1f - (d / range)); // 거리가 멀어질수록, 각도가 벌어질수록 힘이 약해짐
            float angleWeight = Mathf.InverseLerp(cosLimit, 1f, align); // 원뿔의 가장자리에서는 힘이 약해지고, 중앙에서는 최대가 됨

            float w = falloff * angleWeight;
            if (w <= 0f) continue;
            // --- 토네이도 핵심: "축 중심으로 말려 들어오기 + CCW 회전" ---
            // 1) 축 위의 최근접 점(원뿔 중심선의 단면 중심)
            float axial = Vector3.Dot(objPos - origin, axis);  // 축 방향 거리
            axial = Mathf.Clamp(axial, 0f, range);
            Vector3 axisPoint = origin + axis * axial;

            // 2) 축으로 모으는 힘(센터링)
            Vector3 radialFromAxis = objPos - axisPoint;       // 축 -> 오브젝트
            float radialDist = radialFromAxis.magnitude;

            Vector3 toAxisDir = (radialDist < 0.0001f) ? Vector3.zero : (-radialFromAxis / radialDist);

            // 원뿔 반경(해당 axial 위치에서)
            float coneRadiusAtAxial = Mathf.Tan(coneAngle * Mathf.Deg2Rad) * (axial + 0.001f);
            float centerW = (coneRadiusAtAxial <= 0.0001f) ? 1f : Mathf.Clamp01(1f - (radialDist / coneRadiusAtAxial));

            Vector3 centerForce = toAxisDir * (centerStrength * w);

            // 3) 축 방향으로 빨아들이기(앞쪽 진행감)
            Vector3 toSuction = suctionPoint.position - objPos;
            float f = toSuction.magnitude + 0.001f;
            Vector3 pullDir = toSuction / f;

            Vector3 axisPullForce = pullDir * (axisPullStrength * w);

            // 4) 반시계(CCW) 스월: tangent = Cross(axis, radialFromAxis)
            // (만약 현장에서 방향이 반대로 느껴지면 Cross 순서를 반대로 바꾸면 됨)
            Vector3 tangent = (radialDist < 0.0001f) ? Vector3.zero : Vector3.Cross(axis, radialFromAxis).normalized;

            float swirlBoost = Mathf.Lerp(1f, swirlNearCenterBoost, centerW);
            Vector3 swirlForce = tangent * (swirlStrength * w * swirlBoost);

            // 5) 안정화(댐핑)
            Vector3 dampForce = -rb.linearVelocity * damping;

            rb.AddForce(centerForce + axisPullForce + swirlForce + dampForce, ForceMode.Acceleration);


            // 힘 시각화(플레이 중 Scene 뷰)
            if (drawForceRays && drawn < maxForceRaysPerFrame)
            {
                DrawForceRays(objPos, centerForce, axisPullForce, swirlForce, dampForce);
                drawn++;
            }
        }
    }

    private void UpdateSuctionVfx() // 흡입시 파티클 활성화와 커서잠금
    {
        bool shouldOn = (!isLookOff) && isAttacking;
        if (shootParticle.activeSelf != shouldOn)
            shootParticle.SetActive(shouldOn);
    }



    //============= 마우스 록온 ================
    private void LockOff()
    {
        if (isLookOff)
        {
            shootParticle.SetActive(false); // 록온이 풀릴 때 공격 상태도 해제
            lockCursor = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            lockCursor = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    //============= 배출 기능 ================

    private void Release()
    {
        if(!isRelease) return;
        if (isLookOff) return;
        if (Time.time < nextEjectTime) return;
        if (GameManager.Unit.game1Player.inventory.TryEject(selectedSlot, muzzle.position, muzzle.forward, ejectImpulse))
            nextEjectTime = Time.time + ejectInterval;
        else
            nextEjectTime = Time.time + 0.05f; // 배출 실패 시 재시도 간격 (조정 가능)
    }

    //============= 인벤토리 슬롯선택 기능 ================
    private void HandleSlotHotkeys()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)selectedSlot = 0;
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)selectedSlot = 1;
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) selectedSlot = 2;
        else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) selectedSlot = 3;

        GameManager.Event.Publish(EventType.SelectSlot, selectedSlot); // 슬롯 선택 이벤트 발행
    }

    //==================== Debug Draw ====================

    private void DrawForceRays(Vector3 pos, Vector3 center, Vector3 axisPull, Vector3 swirl, Vector3 damp)
    {
        Debug.DrawRay(pos, center * forceRayScale, Color.green);    // 축으로 모으기
        Debug.DrawRay(pos, axisPull * forceRayScale, Color.cyan);   // 축 방향 끌림
        Debug.DrawRay(pos, swirl * forceRayScale, Color.magenta);   // 회전
        Debug.DrawRay(pos, damp * forceRayScale, Color.yellow);     // 감쇠
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (muzzle == null) return;

        Vector3 origin = muzzle.position;
        Vector3 axis = muzzle.forward;

        DrawRangeGizmo(origin, range);
        DrawConeGizmo(origin, axis, range, coneAngle, gizmoSegments);

        float len = Mathf.Min(range, helixLength);
        DrawHelixGizmo(origin, axis, len, helixRadius, helixTurns, gizmoSegments);
    }

    private void DrawRangeGizmo(Vector3 origin, float r)
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, r);
    }

    private void DrawConeGizmo(Vector3 origin, Vector3 axis, float length, float angleDeg, int segments)
    {
        GetOrthonormalBasis(axis, out Vector3 right, out Vector3 up);

        float radius = Mathf.Tan(angleDeg * Mathf.Deg2Rad) * length;
        Vector3 endCenter = origin + axis * length;

        Gizmos.color = Color.green;
        DrawCircleGizmo(endCenter, right, up, radius, segments);

        int step = Mathf.Max(1, segments / 8);
        for (int i = 0; i < segments; i += step)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = endCenter + right * Mathf.Cos(a) * radius + up * Mathf.Sin(a) * radius;
            Gizmos.DrawLine(origin, p);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawLine(origin, endCenter);
    }

    private void DrawCircleGizmo(Vector3 center, Vector3 right, Vector3 up, float radius, int segments)
    {
        Vector3 prev = center + right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private void DrawHelixGizmo(Vector3 origin, Vector3 axis, float length, float radius, int turns, int segments)
    {
        // 반시계 헬릭스(시각 디버그용): CCW가 기본
        GetOrthonormalBasis(axis, out Vector3 right, out Vector3 up);

        Gizmos.color = Color.magenta;

        int steps = Mathf.Max(12, segments * turns);
        Vector3 prev = origin;

        for (int i = 0; i <= steps; i++)
        {
            float s = i / (float)steps;
            float ang = s * turns * Mathf.PI * 2f;
            float z = s * length;

            Vector3 center = origin + axis * z;
            Vector3 offset = (right * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * radius;
            Vector3 p = center + offset;

            if (i > 0) Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }

    private void GetOrthonormalBasis(Vector3 axis, out Vector3 right, out Vector3 up)
    {
        Vector3 tmpUp = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
        right = Vector3.Normalize(Vector3.Cross(tmpUp, axis));
        up = Vector3.Normalize(Vector3.Cross(axis, right));
    }
}
