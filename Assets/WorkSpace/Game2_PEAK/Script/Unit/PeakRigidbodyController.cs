using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class PeakRigidbodyController : MonoBehaviour
{
    [System.Serializable]
    private class ReferenceSettings
    {
        [Header("References")]
        public Transform cameraPivot;
        public Camera playerCamera;
        public Transform itemHoldPoint; // [수정] 아이템을 한번 집으면 손에 유지되도록 하는 기준 위치
    }

    [System.Serializable]
    private class LayerSettings
    {
        [Header("Layers")]
        public LayerMask groundMask;
        public LayerMask climbMask;
        public LayerMask itemMask;      // [수정] 아이템 집기 기능용 레이어
        public LayerMask obstacleMask;  // [수정] Mantle 도착 지점 공간 체크용 레이어
    }

    [System.Serializable]
    private class LookSettings
    {
        [Header("Look")]
        public float lookSensitivity = 0.08f;
        public float minPitch = -80f;
        public float maxPitch = 80f;
    }

    [System.Serializable]
    private class MoveSettings
    {
        [Header("Move")]
        public float walkSpeed = 2.4f;
        public float runSpeed = 4.8f;
        public float groundAcceleration = 35f;
        public float groundDeceleration = 40f;
        public float airAcceleration = 10f;
        public float airMaxSpeed = 3.0f;
        public float groundStickVelocity = -2f;
        public float jumpHeight = 1.25f;
        public float extraGravityMultiplier = 1.0f;   // 점프에서 상승이 끝나고 하강이 시작될 때 적용되는 추가 중력의 배수
    }

    [System.Serializable]
    private class JumpAssistSettings
    {
        [Header("Jump Assist")]
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
    }

    [System.Serializable]
    private class GroundCheckSettings
    {
        [Header("Ground Check")]
        public float maxGroundAngle = 50f;
        public float groundCheckRadiusScale = 0.9f;
        public float groundCheckDistance = 0.12f;
    }

    [System.Serializable]
    private class ClimbSettings
    {
        [Header("Climb")]
        public float minClimbAngle = 60f;
        public float climbCheckDistance = 0.9f;
        public float climbCheckRadius = 0.2f;
        public float climbEnterLookDot = 0.55f;
        public float climbEnterMoveDot = 0.2f;
        public float climbVerticalSpeed = 2.5f;
        public float climbHorizontalSpeed = 2.2f;
        public float climbHoldDistance = 0.38f;
        public float climbHoldSnapSpeed = 18f;
        public float climbMaxCorrectionSpeed = 4f;
        public float climbSurfaceLostGrace = 0.12f;
        public float climbReattachCooldown = 0.2f;
        public float wallDetachPush = 1.5f;
        public float climbLeapUpSpeed = 5.2f;
        public float climbLeapAwaySpeed = 4.0f;

        [Header("Ledge / Mantle")]
        public float mantleProbeUpOffset = 1.1f;          // [수정] 날카로운 정상 턱을 탐지하기 위한 상단 프로브 높이
        public float mantleProbeForwardOffset = 0.35f;    // [수정] 벽면을 넘겨서 윗면을 찾기 위한 전진 오프셋
        public float mantleProbeDownDistance = 1.8f;      // [수정] 정상 바닥을 아래로 찾는 거리
        public float mantleMinLedgeHeight = 0.15f;        // [수정] 너무 낮은 턱은 Mantle 하지 않음
        public float mantleMaxLedgeHeight = 1.3f;         // [수정] 너무 높은 턱은 Mantle 하지 않음
        public float mantleStandForwardOffset = 0.2f;     // [수정] 정상 위에 설 위치를 조금 안쪽으로 잡기
        public float mantleStandHeightOffset = 0.03f;     // [수정] 바닥과 미세하게 겹치지 않도록 높이 보정
        public float mantleDuration = 2f;              // [수정] 난간 올라가는 시간
        public float mantleMinUpInput = 0.1f;             // [수정] 위쪽 입력이 있을 때만 난간 오르기 시도
        public float mantleClearanceLift = 0.06f;         // [수정] 정상 위 공간 체크 시 바닥과의 오판을 줄이기 위한 미세 상승값
    }

    [System.Serializable]
    private class InteractSettings
    {
        [Header("Interact")]
        public float itemPickDistance = 0.9f;             // [수정] climbCheckDistance와 분리된 아이템 집기 거리
        public float itemPickRadius = 0.12f;              // [수정] 아이템 집기 보조 구체 반경
        public float dropForwardSpeed = 1.2f;             // [수정] 나중에 내려놓기/던지기 기능으로 확장할 때 사용할 기본 전방 속도
    }

    [System.Serializable]
    private class StaminaSettings
    {
        [Header("Stamina")]
        public float baseMaxStamina = 100f;
        public float startExtraStamina = 0f;  // 추가 스태미나
        public float staminaRecoverPerSecond = 18f;   // 스태미나 회복 속도
        public float staminaRecoverDelay = 0.2f; // 스태미나 회복이 시작되기 전 대기 시간
        public float sprintDrainPerSecond = 2f;  // 달리기 중 초당 소모되는 스태미나
        public float jumpStaminaCost = 5f; // 점프 시 소모되는 스태미나
        public float climbStartMinStamina = 2f;   // 클라이밍을 시작하기 위한 최소 스태미나
        public float climbHoldDrainPerSecond = 3f;  // 클라이밍을 유지하는 동안 초당 소모되는 스태미나
        public float climbMoveExtraDrainPerSecond = 6f; // 클라이밍 중 이동할 때 추가로 초당 소모되는 스태미나
        public float climbLeapCost = 10f; // 클라이밍 도약 시 소모되는 스태미나
    }

    [System.Serializable]
    private class PenaltySettings
    {
        [Header("Penalty Hooks")]
        public float hungerPenalty = 0f; // 배고픔으로 인한 스태미나 패널티
        public float weightPenalty = 0f; // 무거운 장비로 인한 스태미나 패널티
        public float injuryPenalty = 0f; // 부상으로 인한 스태미나 패널티
        public float statusPenalty = 0f; // 상태 이상(중독, 질병 등)으로 인한 스태미나 패널티
        public float revivalPenalty = 0f; // 부활 후 일시적으로 적용되는 스태미나 패널티
    }

    [System.Serializable]
    private class DebugSettings
    {
        [Header("Debug")]
        public bool debugLogState = false;
        public bool drawDebugGizmos = true;
    }

    [SerializeField] private ReferenceSettings references = new ReferenceSettings();
    [SerializeField] private LayerSettings layers = new LayerSettings();
    [SerializeField] private LookSettings look = new LookSettings();
    [SerializeField] private MoveSettings move = new MoveSettings();
    [SerializeField] private JumpAssistSettings jumpAssist = new JumpAssistSettings();
    [SerializeField] private GroundCheckSettings ground = new GroundCheckSettings();
    [SerializeField] private ClimbSettings climb = new ClimbSettings();
    [SerializeField] private InteractSettings interact = new InteractSettings();
    [SerializeField] private StaminaSettings staminaSettings = new StaminaSettings();
    [SerializeField] private PenaltySettings penalties = new PenaltySettings();
    [SerializeField] private DebugSettings debugOption = new DebugSettings();

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private PeakStaminaRuntime staminaRuntime = new PeakStaminaRuntime();

    private Vector2 moveInput;
    private Vector2 lookInput;

    // 입력 상태
    private bool runHeld;
    private bool runPressedThisStep;
    private bool grabHeld;
    private bool jumpPressedThisStep;
    private bool interactPressedThisStep; // [수정] 아이템 집기 기능 : 한번 클릭으로 집고 손에 든 상태는 버튼을 떼도 유지됨

    // 회전 상태
    private float yaw;
    private float pitch;

    // 지면 체크 관련 상태
    private bool isGrounded;
    private RaycastHit groundHit;

    // 클라이밍 관련 상태
    private bool hasClimbSurface;
    private RaycastHit climbHit;
    private float climbSurfaceLostTimer;
    private float climbReattachTimer;

    // 점프 어시스트 타이머
    private float coyoteTimer;
    private float jumpBufferTimer;

    // [수정] Mantle 관련 상태
    private Vector3 mantleStartPosition;
    private Vector3 mantleTargetPosition;
    private float mantleTimer;

    private LocomotionState state = LocomotionState.Airborne;

    // [수정] 아이템 관련 상태
    private PeakCarryable heldItem;
    private PeakCarryable currentInteractTarget;

    // [수정] 이벤트 중복 발행 방지용 캐시
    private float lastPublishedStamina = float.MinValue;
    private float lastPublishedMaxStamina = float.MinValue;
    private float lastPublishedExtraStamina = float.MinValue;
    private bool lastPublishedClimbSurface;
    private float lastPublishedClimbStamina = float.MinValue;
    private LocomotionState lastPublishedState;

    public event Action<float, float, float> OnStaminaChanged;
    public event Action<bool, LocomotionState, float> OnClimbContextChanged;
    public event Action<PeakCarryable> OnHeldItemChanged;

    // 스태미나 관련 프로퍼티
    public string CurrentStateName => state.ToString();
    public float CurrentMaxStamina => Mathf.Max(0f, staminaSettings.baseMaxStamina - penalties.hungerPenalty - penalties.weightPenalty - penalties.injuryPenalty - penalties.statusPenalty - penalties.revivalPenalty);
    public float CurrentStamina => staminaRuntime.Current;
    public float CurrentExtraStamina => staminaRuntime.Extra;
    public bool HasClimbSurface => hasClimbSurface;
    public bool IsGrounded => isGrounded;
    public bool IsClimbing => state == LocomotionState.Climbing;
    public bool IsMantling => state == LocomotionState.Mantling;
    public PeakCarryable HeldItem => heldItem;
    public PeakCarryable CurrentInteractTarget => currentInteractTarget;

    private Vector3 Velocity
    {
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
    }

    // --- 유니티 이벤트 함수 ---
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (references.cameraPivot == null)
            references.cameraPivot = transform;

        if (references.playerCamera == null)
            references.playerCamera = GetComponentInChildren<Camera>();

        if (references.itemHoldPoint == null)
            references.itemHoldPoint = references.cameraPivot != null ? references.cameraPivot : transform;

        yaw = transform.eulerAngles.y;

        float rawPitch = references.cameraPivot != null ? references.cameraPivot.localEulerAngles.x : 0f;
        pitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;

        staminaRuntime.Initialize(CurrentMaxStamina, staminaSettings.startExtraStamina);

        rb.useGravity = true;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // [수정] 시작 시점에 한 번만 UI 이벤트 발행
        NotifyRuntimeSignalsIfNeeded(true);
    }

    private void Update()
    {
        UpdateLook();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        staminaRuntime.BeginStep();
        TickTimers(dt);

        ProbeGround();
        ProbeClimbSurface();
        ProbeInteractTarget();
        UpdateCoyoteTimer();
        UpdateStateTransitions();
        ApplyBodyRotation();
        TickState();
        ProcessInteraction();
        RecoverStamina(dt);
        NotifyRuntimeSignalsIfNeeded(false);
        ClearStepInputs();
    }

    private void TickTimers(float dt)
    {
        if (climbReattachTimer > 0f)
            climbReattachTimer -= dt;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= dt;

        if (coyoteTimer > 0f)
            coyoteTimer -= dt;

        staminaRuntime.TickTimers(dt);
    }

    // --- 이동 및 회전 처리 ---
    private void UpdateLook()
    {
        yaw += lookInput.x * look.lookSensitivity;
        pitch -= lookInput.y * look.lookSensitivity;
        pitch = Mathf.Clamp(pitch, look.minPitch, look.maxPitch);

        if (references.cameraPivot != null)
            references.cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void ApplyBodyRotation()
    {
        Quaternion targetRot = Quaternion.Euler(0f, yaw, 0f);
        rb.MoveRotation(targetRot);
    }

    private void ProbeGround()
    {
        groundHit = default;
        isGrounded = false;

        float radius = Mathf.Max(0.05f, capsule.radius * ground.groundCheckRadiusScale);
        Vector3 origin = capsule.bounds.center + Vector3.up * 0.02f;
        float castDistance = capsule.bounds.extents.y - radius + ground.groundCheckDistance;

        if (!Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, castDistance, layers.groundMask, QueryTriggerInteraction.Ignore))
            return;

        float angle = Vector3.Angle(hit.normal, Vector3.up);
        if (angle > ground.maxGroundAngle)
            return;

        isGrounded = true;
        groundHit = hit;
    }

    private void ProbeClimbSurface()
    {
        hasClimbSurface = false;
        climbHit = default;

        if (layers.climbMask.value == 0)
            return;

        Transform view = GetViewTransform();

        Vector3 originA = view.position;
        Vector3 dirA = view.forward.normalized;

        Vector3 originB = capsule.bounds.center;
        Vector3 dirB = transform.forward.normalized;

        bool found = false;
        float bestDistance = float.MaxValue;

        if (Physics.SphereCast(originA, climb.climbCheckRadius, dirA, out RaycastHit hitA, climb.climbCheckDistance, layers.climbMask, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(hitA.normal, Vector3.up);
            if (angle >= climb.minClimbAngle)
            {
                found = true;
                climbHit = hitA;
                bestDistance = hitA.distance;
            }
        }

        if (Physics.SphereCast(originB, climb.climbCheckRadius, dirB, out RaycastHit hitB, climb.climbCheckDistance, layers.climbMask, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(hitB.normal, Vector3.up);
            if (angle >= climb.minClimbAngle && (!found || hitB.distance < bestDistance))
            {
                found = true;
                climbHit = hitB;
                bestDistance = hitB.distance;
            }
        }

        hasClimbSurface = found;
    }

    // [수정] 아이템 집기 기능 : climbCheckDistance와 분리된 itemPickDistance 기반 감지
    private void ProbeInteractTarget()
    {
        if (heldItem != null)
        {
            SetInteractTarget(null);
            return;
        }

        if (layers.itemMask.value == 0)
        {
            SetInteractTarget(null);
            return;
        }

        Ray viewRay = GetViewCenterRay();

        float checkDistance = interact.itemPickDistance > 0f
            ? interact.itemPickDistance
            : climb.climbCheckDistance;

        Vector3 rayOrigin = viewRay.origin + viewRay.direction * 0.05f;
        Vector3 rayDir = viewRay.direction;

        // 1차 : 정중앙 레이 우선
        if (Physics.Raycast(
            rayOrigin,
            rayDir,
            out RaycastHit rayHit,
            checkDistance,
            layers.itemMask,
            QueryTriggerInteraction.Collide))
        {
            PeakCarryable carryable = rayHit.collider.GetComponentInParent<PeakCarryable>();
            if (carryable != null)
            {
                SetInteractTarget(carryable);
                return;
            }
        }

        // 2차 : 약간 빗나간 경우 보조 구체 캐스트
        if (Physics.SphereCast(
            rayOrigin,
            interact.itemPickRadius,
            rayDir,
            out RaycastHit sphereHit,
            checkDistance,
            layers.itemMask,
            QueryTriggerInteraction.Collide))
        {
            PeakCarryable carryable = sphereHit.collider.GetComponentInParent<PeakCarryable>();
            if (carryable != null)
            {
                SetInteractTarget(carryable);
                return;
            }
        }

        // 감지 실패 시 UI 끄기
        SetInteractTarget(null);
    }
    private void SetInteractTarget(PeakCarryable newTarget) //아이템 감지 대상이 바뀔 때만 UI 이벤트를 발행
    {
        if (currentInteractTarget == newTarget)
            return;

        currentInteractTarget = newTarget;
        GameManager.Event.Publish(EventType.InteractText, currentInteractTarget);
    }

    // [수정] 실제 화면 중앙 조준점 기준 레이를 반환
    private Ray GetViewCenterRay()
    {
        if (references.playerCamera != null)
            return references.playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Transform view = GetViewTransform();
        return new Ray(view.position, view.forward);
    }

    private void UpdateCoyoteTimer()
    {
        if (isGrounded && state != LocomotionState.Climbing && state != LocomotionState.Mantling)
            coyoteTimer = jumpAssist.coyoteTime;
    }

    private void UpdateStateTransitions()
    {
        switch (state)
        {
            case LocomotionState.Climbing:
                // [수정] 날카로운 난간 정상 처리 : 클라이밍 도중 정상 바닥과 서 있을 공간이 있으면 Mantling으로 전이
                if (TryStartMantle())
                    return;

                if (!grabHeld)
                {
                    ExitClimb(false);
                    return;
                }

                if (!hasClimbSurface)
                {
                    climbSurfaceLostTimer -= Time.fixedDeltaTime;
                    if (climbSurfaceLostTimer <= 0f)
                    {
                        ExitClimb(false);
                        return;
                    }
                }
                else
                {
                    climbSurfaceLostTimer = climb.climbSurfaceLostGrace;
                }

                if (jumpPressedThisStep || runPressedThisStep)
                {
                    DoClimbLeap();
                    return;
                }
                break;

            case LocomotionState.Mantling:
                // [수정] Mantling은 실행 함수에서 완료될 때까지 별도 전이하지 않음
                break;

            default:
                if (CanEnterClimb())
                {
                    EnterClimb();
                    return;
                }

                if (TryStartGroundJump())
                    return;

                state = isGrounded ? LocomotionState.Grounded : LocomotionState.Airborne;
                break;
        }
    }

    private void TickState()
    {
        switch (state)
        {
            case LocomotionState.Grounded:
                TickGrounded();
                break;

            case LocomotionState.Airborne:
                TickAirborne();
                break;

            case LocomotionState.Climbing:
                TickClimbing();
                break;

            case LocomotionState.Mantling:
                TickMantling();
                break;
        }
    }

    private void TickGrounded()
    {
        rb.useGravity = true;

        Vector3 desiredDir = GetCameraRelativeMove();
        if (isGrounded)
            desiredDir = Vector3.ProjectOnPlane(desiredDir, groundHit.normal).normalized;

        Vector3 targetPlanar = desiredDir * GetTargetGroundSpeed();

        Vector3 currentPlanar = new Vector3(Velocity.x, 0f, Velocity.z);
        float accel = desiredDir.sqrMagnitude > 0.0001f ? move.groundAcceleration : move.groundDeceleration;
        Vector3 newPlanar = Vector3.MoveTowards(currentPlanar, targetPlanar, accel * Time.fixedDeltaTime);

        float y = Velocity.y;
        if (y < move.groundStickVelocity)
            y = move.groundStickVelocity;

        Velocity = new Vector3(newPlanar.x, y, newPlanar.z);
    }

    private void TickAirborne()
    {
        rb.useGravity = true;

        Vector3 desiredDir = GetCameraRelativeMove();
        Vector3 currentPlanar = new Vector3(Velocity.x, 0f, Velocity.z);
        Vector3 targetPlanar = desiredDir * move.airMaxSpeed;
        Vector3 newPlanar = Vector3.MoveTowards(currentPlanar, targetPlanar, move.airAcceleration * Time.fixedDeltaTime);

        Vector3 vel = Velocity;
        vel.x = newPlanar.x;
        vel.z = newPlanar.z;
        Velocity = vel;

        ApplyExtraGravity();    // 점프에서 상승이 끝나고 하강이 시작될 때 추가 중력 적용
        PreventWallStickWhileAirborne();
    }

    private void TickClimbing()
    {
        rb.useGravity = false;

        if (!hasClimbSurface)
            return;

        Vector3 wallNormal = climbHit.normal;

        Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, wallNormal);
        if (wallUp.sqrMagnitude < 0.0001f)
            wallUp = Vector3.up;
        wallUp.Normalize();

        Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal).normalized;

        Vector3 climbMove = (wallRight * moveInput.x) + (wallUp * moveInput.y);
        if (climbMove.sqrMagnitude > 1f)
            climbMove.Normalize();

        float drain = staminaSettings.climbHoldDrainPerSecond;
        if (climbMove.sqrMagnitude > 0.0001f)
            drain += staminaSettings.climbMoveExtraDrainPerSecond;

        if (!TryConsumeStamina(drain * Time.fixedDeltaTime))
        {
            ExitClimb(false);
            return;
        }

        float distanceError = climbHit.distance - climb.climbHoldDistance;
        float correctionSpeed = Mathf.Clamp(distanceError * climb.climbHoldSnapSpeed, -climb.climbMaxCorrectionSpeed, climb.climbMaxCorrectionSpeed);

        Vector3 towardWallVelocity = -wallNormal * correctionSpeed;
        Vector3 moveVelocity =
            (wallRight * moveInput.x * climb.climbHorizontalSpeed) +
            (wallUp * moveInput.y * climb.climbVerticalSpeed);

        Velocity = moveVelocity + towardWallVelocity;
    }

    // [수정] 날카로운 난간을 타고 올라가는 Mantle 실행 함수
    private void TickMantling()
    {
        rb.useGravity = false;
        Velocity = Vector3.zero;

        mantleTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(mantleTimer / climb.mantleDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        Vector3 nextPosition = Vector3.Lerp(mantleStartPosition, mantleTargetPosition, eased);
        rb.MovePosition(nextPosition);

        if (t >= 1f)
            FinishMantle();
    }

    private Vector3 GetCameraRelativeMove()
    {
        Transform basis = GetViewTransform();

        Vector3 forward = basis.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;
        else
            forward.Normalize();

        Vector3 right = basis.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.0001f)
            right = transform.right;
        else
            right.Normalize();

        Vector3 desired = forward * moveInput.y + right * moveInput.x;
        if (desired.sqrMagnitude > 1f)
            desired.Normalize();

        return desired;
    }

    private float GetTargetGroundSpeed()
    {
        if (runHeld && moveInput.sqrMagnitude > 0.0001f)
        {
            if (TryConsumeStamina(staminaSettings.sprintDrainPerSecond * Time.fixedDeltaTime))
                return move.runSpeed;
        }

        return move.walkSpeed;
    }

    private bool TryStartGroundJump()
    {
        if (jumpBufferTimer <= 0f)
            return false;

        if (coyoteTimer <= 0f)
            return false;

        if (!TryConsumeStamina(staminaSettings.jumpStaminaCost))
            return false;

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        Vector3 vel = Velocity;
        vel.y = Mathf.Sqrt(move.jumpHeight * -2f * Physics.gravity.y);
        Velocity = vel;

        state = LocomotionState.Airborne;
        LogState("Jump");

        return true;
    }

    // --- 클라이밍 함수 ---
    private bool CanEnterClimb()
    {
        if (!grabHeld)
            return false;

        if (climbReattachTimer > 0f)
            return false;

        if (CurrentTotalStamina() < staminaSettings.climbStartMinStamina)
            return false;

        if (!hasClimbSurface)
            return false;

        Vector3 towardWall = -climbHit.normal.normalized;
        Transform view = GetViewTransform();

        float lookDot = Vector3.Dot(view.forward.normalized, towardWall);
        if (lookDot < climb.climbEnterLookDot)
            return false;

        Vector3 desiredMove = GetCameraRelativeMove();
        bool pushingToWall = false;

        if (desiredMove.sqrMagnitude > 0.0001f)
        {
            float moveDot = Vector3.Dot(desiredMove.normalized, towardWall);
            pushingToWall = moveDot >= climb.climbEnterMoveDot;
        }

        if (!isGrounded)
            return true;

        return pushingToWall;
    }

    private void EnterClimb()
    {
        state = LocomotionState.Climbing;
        climbSurfaceLostTimer = climb.climbSurfaceLostGrace;
        rb.useGravity = false;
        Velocity = Vector3.zero;
        LogState("Climb");
    }

    private void ExitClimb(bool keepCurrentHorizontal)
    {
        rb.useGravity = true;
        state = LocomotionState.Airborne;
        climbReattachTimer = climb.climbReattachCooldown;

        Vector3 vel = Velocity;

        if (!keepCurrentHorizontal && hasClimbSurface)
        {
            Vector3 push = climbHit.normal;
            push.y = 0f;

            if (push.sqrMagnitude > 0.0001f)
            {
                push.Normalize();
                vel.x = push.x * climb.wallDetachPush;
                vel.z = push.z * climb.wallDetachPush;
            }
        }

        if (vel.y > -1f)
            vel.y = -1f;

        Velocity = vel;
        LogState("Fall");
    }

    private void DoClimbLeap()
    {
        if (!TryConsumeStamina(staminaSettings.climbLeapCost))
        {
            ExitClimb(false);
            return;
        }

        Vector3 away = climbHit.normal;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = -transform.forward;
        away.Normalize();

        Vector3 vel = Velocity;
        vel.x = away.x * climb.climbLeapAwaySpeed;
        vel.z = away.z * climb.climbLeapAwaySpeed;
        vel.y = climb.climbLeapUpSpeed;

        rb.useGravity = true;
        state = LocomotionState.Airborne;
        climbReattachTimer = climb.climbReattachCooldown;
        Velocity = vel;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        LogState("ClimbLeap");
    }

    // [수정] 날카로운 난간 상단으로 자연스럽게 올라가기 위한 진입 조건
    private bool TryStartMantle()
    {
        if (state != LocomotionState.Climbing)
            return false;

        if (!hasClimbSurface)
            return false;

        if (moveInput.y < climb.mantleMinUpInput)
            return false;

        if (!TryFindMantleTarget(out Vector3 targetPosition))
            return false;

        mantleStartPosition = rb.position;
        mantleTargetPosition = targetPosition;
        mantleTimer = 0f;
        state = LocomotionState.Mantling;
        Velocity = Vector3.zero;
        LogState("Mantle");

        return true;
    }

    // [수정] 상단 정상 바닥과 서 있을 공간이 있는지 검사
    private bool TryFindMantleTarget(out Vector3 targetRootPosition)
    {
        targetRootPosition = default;

        if (!hasClimbSurface)
            return false;

        Vector3 wallNormal = climbHit.normal;
        Vector3 forwardOverLedge = -wallNormal;
        forwardOverLedge.y = 0f;

        if (forwardOverLedge.sqrMagnitude < 0.0001f)
            forwardOverLedge = transform.forward;
        else
            forwardOverLedge.Normalize();

        Vector3 topProbeOrigin =
            climbHit.point +
            Vector3.up * climb.mantleProbeUpOffset +
            forwardOverLedge * climb.mantleProbeForwardOffset;

        if (!Physics.Raycast(topProbeOrigin, Vector3.down, out RaycastHit topHit, climb.mantleProbeDownDistance, layers.groundMask, QueryTriggerInteraction.Ignore))
            return false;

        float topAngle = Vector3.Angle(topHit.normal, Vector3.up);
        if (topAngle > ground.maxGroundAngle)
            return false;

        float currentFeetY = GetFeetPointWorld(transform.position).y;
        float ledgeHeight = topHit.point.y - currentFeetY;
        if (ledgeHeight < climb.mantleMinLedgeHeight || ledgeHeight > climb.mantleMaxLedgeHeight)
            return false;

        Vector3 standGroundPoint =
            topHit.point +
            forwardOverLedge * climb.mantleStandForwardOffset +
            Vector3.up * climb.mantleStandHeightOffset;

        targetRootPosition = BuildRootPositionFromGroundPoint(standGroundPoint);

        if (!HasStandingRoom(targetRootPosition))
            return false;

        return true;
    }

    private void FinishMantle()
    {
        rb.useGravity = true;
        state = LocomotionState.Grounded;
        Velocity = Vector3.zero;
        LogState("Grounded");
    }

    private Transform GetViewTransform()
    {
        if (references.playerCamera != null)
            return references.playerCamera.transform;

        if (references.cameraPivot != null)
            return references.cameraPivot;

        return transform;
    }

    // [수정] 아이템 집기 기능 : 클릭 한 번으로 손에 유지, 사용 버튼은 추후 추가
    private void ProcessInteraction()
    {
        if (!interactPressedThisStep)
            return;
        if (state == LocomotionState.Climbing || state == LocomotionState.Mantling)
            return;

        if (heldItem != null)
            return; // 사용 버튼은 내가 새롭게 추가할꺼니 그건 빼두고, 집은 상태만 유지

        if (currentInteractTarget == null)
            return;

        PickItem(currentInteractTarget);
    }

    private void PickItem(PeakCarryable targetItem)
    {
        if (targetItem == null)
            return;

        heldItem = targetItem;
        heldItem.Pickup(references.itemHoldPoint != null ? references.itemHoldPoint : transform);
        // [수정] 아이템을 집은 순간 상호작용 텍스트 비활성화
        SetInteractTarget(null);

        OnHeldItemChanged?.Invoke(heldItem);
    }

    // [수정] 외부 버튼에서 나중에 연결할 수 있도록 public 유지
    public void DropHeldItem()
    {
        if (heldItem == null)
            return;

        PeakCarryable itemToDrop = heldItem;
        heldItem = null;

        Vector3 releaseVelocity = Velocity + GetViewTransform().forward * interact.dropForwardSpeed;
        itemToDrop.Drop(releaseVelocity);
        // [수정] 내려놓은 직후 현재 상호작용 타겟 초기화
        SetInteractTarget(null);

        OnHeldItemChanged?.Invoke(null);
    }

    private void PreventWallStickWhileAirborne()
    {
        if (state == LocomotionState.Climbing || state == LocomotionState.Mantling)
            return;

        if (isGrounded)
            return;

        if (!hasClimbSurface)
            return;

        Vector3 vel = Velocity;
        Vector3 intoWallDir = -climbHit.normal;
        float intoWallSpeed = Vector3.Dot(vel, intoWallDir);

        if (intoWallSpeed > 0f)
            vel -= intoWallDir * intoWallSpeed;

        Velocity = vel;
    }

    private void ApplyExtraGravity()    // 점프에서 상승이 끝나고 하강이 시작될 때 추가 중력 적용
    {
        if (move.extraGravityMultiplier <= 1f)
            return;

        if (Velocity.y >= 0f)
            return;

        Vector3 extraGravity = Physics.gravity * (move.extraGravityMultiplier - 1f);
        rb.AddForce(extraGravity, ForceMode.Acceleration);
    }

    // --- 스태미나 처리 ---
    private void RecoverStamina(float dt)
    {
        bool recoveryBlocked = state == LocomotionState.Climbing || state == LocomotionState.Mantling;
        staminaRuntime.Recover(dt, CurrentMaxStamina, staminaSettings.staminaRecoverPerSecond, staminaSettings.staminaRecoverDelay, recoveryBlocked);
    }

    private bool TryConsumeStamina(float amount)
    {
        return staminaRuntime.Consume(amount);
    }

    private float CurrentTotalStamina()
    {
        return staminaRuntime.Total;
    }

    // [수정] 값이 바뀔 때만 이벤트 발행하도록 최적화
    private void NotifyRuntimeSignalsIfNeeded(bool force)
    {
        bool staminaDirty =
            force ||
            !Mathf.Approximately(lastPublishedStamina, CurrentStamina) ||
            !Mathf.Approximately(lastPublishedMaxStamina, CurrentMaxStamina) ||
            !Mathf.Approximately(lastPublishedExtraStamina, CurrentExtraStamina);

        if (staminaDirty)
        {
            OnStaminaChanged?.Invoke(CurrentStamina, CurrentMaxStamina, CurrentExtraStamina);
            GameManager.Event.Publish(EventType.StaminaChanged, CurrentStamina, CurrentMaxStamina);

            lastPublishedStamina = CurrentStamina;
            lastPublishedMaxStamina = CurrentMaxStamina;
            lastPublishedExtraStamina = CurrentExtraStamina;
        }

        bool climbDirty =
            force ||
            lastPublishedClimbSurface != hasClimbSurface ||
            lastPublishedState != state ||
            !Mathf.Approximately(lastPublishedClimbStamina, CurrentStamina);

        if (climbDirty)
        {
            OnClimbContextChanged?.Invoke(hasClimbSurface, state, CurrentStamina);
            GameManager.Event.Publish(EventType.ClimbCheck, hasClimbSurface, state, CurrentStamina);

            lastPublishedClimbSurface = hasClimbSurface;
            lastPublishedState = state;
            lastPublishedClimbStamina = CurrentStamina;
        }
    }

    // 디버그 로그
    private void LogState(string next)
    {
        if (debugOption.debugLogState)
            Debug.Log($"State -> {next}", this);
    }

    private void ClearStepInputs()
    {
        runPressedThisStep = false;
        jumpPressedThisStep = false;
        interactPressedThisStep = false;
    }

    private int GetObstacleMask()
    {
        if (layers.obstacleMask.value != 0)
            return layers.obstacleMask.value;

        return layers.groundMask.value | layers.climbMask.value;
    }

    private float GetWorldCapsuleRadius()
    {
        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        return capsule.radius * scale;
    }

    private float GetWorldCapsuleHeight()
    {
        float scaleY = Mathf.Abs(transform.lossyScale.y);
        float scaledHeight = capsule.height * scaleY;
        return Mathf.Max(scaledHeight, GetWorldCapsuleRadius() * 2f);
    }

    private Vector3 GetWorldCapsuleCenterOffset()
    {
        return transform.rotation * Vector3.Scale(capsule.center, transform.lossyScale);
    }

    private Vector3 GetFeetPointWorld(Vector3 rootPosition)
    {
        float halfHeight = GetWorldCapsuleHeight() * 0.5f;
        float radius = GetWorldCapsuleRadius();
        return rootPosition + GetWorldCapsuleCenterOffset() + Vector3.down * (halfHeight - radius);
    }

    private Vector3 BuildRootPositionFromGroundPoint(Vector3 groundPoint)
    {
        float halfHeight = GetWorldCapsuleHeight() * 0.5f;
        return groundPoint + Vector3.up * halfHeight - GetWorldCapsuleCenterOffset();
    }

    private void GetCapsulePoints(Vector3 rootPosition, float shrink, out Vector3 bottom, out Vector3 top, out float radius)
    {
        radius = Mathf.Max(0.01f, GetWorldCapsuleRadius() - shrink);
        float halfBody = Mathf.Max(0f, GetWorldCapsuleHeight() * 0.5f - radius);

        Vector3 center = rootPosition + GetWorldCapsuleCenterOffset();
        bottom = center + Vector3.down * halfBody;
        top = center + Vector3.up * halfBody;
    }

    private bool HasStandingRoom(Vector3 candidateRootPosition)
    {
        GetCapsulePoints(candidateRootPosition + Vector3.up * climb.mantleClearanceLift, 0.02f, out Vector3 bottom, out Vector3 top, out float radius);
        return !Physics.CheckCapsule(bottom, top, radius, GetObstacleMask(), QueryTriggerInteraction.Ignore);
    }

    // --- Input System Callbacks ---
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnRun(InputValue value)
    {
        bool pressed = value.isPressed;
        runPressedThisStep = pressed && !runHeld;
        runHeld = pressed;
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed)
            return;

        jumpPressedThisStep = true;
        jumpBufferTimer = jumpAssist.jumpBufferTime;
    }

    // [수정] 아이템 집기 기능 : 한번 클릭으로 집고 손에 든 상태는 버튼을 떼도 유지됨
    public void OnInteract(InputValue value)
    {
        if (!value.isPressed)
            return;

        interactPressedThisStep = true;
    }

    public void OnDrop(InputValue value)
    {
        if (!value.isPressed)
            return;

        DropHeldItem();
    }

    public void OnLookOff(InputValue value)
    {
        bool pressed = value.isPressed;
        if (pressed)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // 디버그용 지오메트리 그리기
    private void OnDrawGizmosSelected()
    {
        if (!debugOption.drawDebugGizmos)
            return;

        CapsuleCollider cc = GetComponent<CapsuleCollider>();
        if (cc == null)
            return;

        Transform view = references.playerCamera != null ? references.playerCamera.transform : (references.cameraPivot != null ? references.cameraPivot : transform);

        Gizmos.color = Color.green;
        float groundRadius = Mathf.Max(0.05f, cc.radius * ground.groundCheckRadiusScale);
        Vector3 groundOrigin = cc.bounds.center + Vector3.up * 0.02f;
        float groundLen = cc.bounds.extents.y - groundRadius + ground.groundCheckDistance;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * groundLen);
        Gizmos.DrawWireSphere(groundOrigin + Vector3.down * groundLen, groundRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(view.position, view.position + view.forward * climb.climbCheckDistance);
        Gizmos.DrawWireSphere(view.position + view.forward * climb.climbCheckDistance, climb.climbCheckRadius);

        Gizmos.color = Color.yellow;
        Vector3 chest = cc.bounds.center;
        Gizmos.DrawLine(chest, chest + transform.forward * climb.climbCheckDistance);
        Gizmos.DrawWireSphere(chest + transform.forward * climb.climbCheckDistance, climb.climbCheckRadius);

        Gizmos.color = Color.magenta;
        Vector3 mantleProbeOrigin = chest + Vector3.up * climb.mantleProbeUpOffset + transform.forward * climb.mantleProbeForwardOffset;
        Gizmos.DrawLine(mantleProbeOrigin, mantleProbeOrigin + Vector3.down * climb.mantleProbeDownDistance);

        // [수정] 아이템 감지 레이 디버그 : 화면 중앙 조준점 기준
        if (references.playerCamera != null)
        {
            Ray itemRay = references.playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 rayOrigin = itemRay.origin + itemRay.direction * 0.05f;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(rayOrigin, rayOrigin + itemRay.direction * interact.itemPickDistance);
            Gizmos.DrawWireSphere(rayOrigin + itemRay.direction * interact.itemPickDistance, interact.itemPickRadius);
        }
    }
}
