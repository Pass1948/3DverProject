using UnityEngine;
using UnityEngine.InputSystem;

public class PeakRigidbodyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera playerCamera;

    [Header("Layers")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask climbMask;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.08f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Move")]
    [SerializeField] private float walkSpeed = 2.4f;
    [SerializeField] private float runSpeed = 4.8f;
    [SerializeField] private float groundAcceleration = 35f;
    [SerializeField] private float groundDeceleration = 40f;
    [SerializeField] private float airAcceleration = 10f;
    [SerializeField] private float airMaxSpeed = 3.0f;
    [SerializeField] private float groundStickVelocity = -2f;
    [SerializeField] private float jumpHeight = 1.25f;
    [SerializeField] private float extraGravityMultiplier = 1.0f;   // 점프에서 상승이 끝나고 하강이 시작될 때 적용되는 추가 중력의 배수

    [Header("Jump Assist")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [SerializeField] private float maxGroundAngle = 50f;
    [SerializeField] private float groundCheckRadiusScale = 0.9f;
    [SerializeField] private float groundCheckDistance = 0.12f;

    [Header("Climb")]
    [SerializeField] private float minClimbAngle = 60f;
    [SerializeField] private float climbCheckDistance = 0.9f;
    [SerializeField] private float climbCheckRadius = 0.2f;
    [SerializeField] private float climbEnterLookDot = 0.55f;
    [SerializeField] private float climbEnterMoveDot = 0.2f;
    [SerializeField] private float climbVerticalSpeed = 2.5f;
    [SerializeField] private float climbHorizontalSpeed = 2.2f;
    [SerializeField] private float climbHoldDistance = 0.38f;
    [SerializeField] private float climbHoldSnapSpeed = 18f;
    [SerializeField] private float climbMaxCorrectionSpeed = 4f;
    [SerializeField] private float climbSurfaceLostGrace = 0.12f;
    [SerializeField] private float climbReattachCooldown = 0.2f;
    [SerializeField] private float wallDetachPush = 1.5f;
    [SerializeField] private float climbLeapUpSpeed = 5.2f;
    [SerializeField] private float climbLeapAwaySpeed = 4.0f;

    [Header("Stamina")]
    [SerializeField] private float baseMaxStamina = 100f;
    [SerializeField] private float startExtraStamina = 0f;  // 추가 스태미나
    [SerializeField] private float staminaRecoverPerSecond = 18f;   // 스태미나 회복 속도
    [SerializeField] private float staminaRecoverDelay = 0.2f; // 스태미나 회복이 시작되기 전 대기 시간
    [SerializeField] private float sprintDrainPerSecond = 2f;  // 달리기 중 초당 소모되는 스태미나
    [SerializeField] private float jumpStaminaCost = 5f; // 점프 시 소모되는 스태미나
    [SerializeField] private float climbStartMinStamina = 2f;   // 클라이밍을 시작하기 위한 최소 스태미나
    [SerializeField] private float climbHoldDrainPerSecond = 3f;  // 클라이밍을 유지하는 동안 초당 소모되는 스태미나
    [SerializeField] private float climbMoveExtraDrainPerSecond = 6f; // 클라이밍 중 이동할 때 추가로 초당 소모되는 스태미나
    [SerializeField] private float climbLeapCost = 10f; // 클라이밍 도약 시 소모되는 스태미나

    [Header("Penalty Hooks")]
    [SerializeField] private float hungerPenalty = 0f; // 배고픔으로 인한 스태미나 패널티
    [SerializeField] private float weightPenalty = 0f; // 무거운 장비로 인한 스태미나 패널티
    [SerializeField] private float injuryPenalty = 0f; // 부상으로 인한 스태미나 패널티
    [SerializeField] private float statusPenalty = 0f; // 상태 이상(중독, 질병 등)으로 인한 스태미나 패널티
    [SerializeField] private float revivalPenalty = 0f; // 부활 후 일시적으로 적용되는 스태미나 패널티

    [Header("Debug")]
    [SerializeField] private bool debugLogState = false;
    [SerializeField] private bool drawDebugGizmos = true;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector2 moveInput;
    private Vector2 lookInput;
    // 입력 상태
    private bool runHeld;
    private bool runPressedThisStep;
    private bool grabHeld;
    private bool jumpPressedThisStep;

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


    // 스태미나 시스템 관련 변수
    private float staminaRecoverDelayTimer;
    private float currentStamina;
    private float currentExtraStamina;
    private bool consumedStaminaThisStep;

    private LocomotionState state = LocomotionState.Airborne;

    // 스태미나 관련 프로퍼티
    public string CurrentStateName => state.ToString();
    public float CurrentMaxStamina => Mathf.Max(0f, baseMaxStamina - hungerPenalty - weightPenalty - injuryPenalty - statusPenalty - revivalPenalty);
    public float CurrentStamina => currentStamina;
    public float CurrentExtraStamina => currentExtraStamina;

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

        if (cameraPivot == null)
            cameraPivot = transform;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        yaw = transform.eulerAngles.y;

        float rawPitch = cameraPivot.localEulerAngles.x;
        pitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;

        currentStamina = CurrentMaxStamina;
        currentExtraStamina = startExtraStamina;

        rb.useGravity = true;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void Update()
    {
        float dt = Time.deltaTime;

        GameManager.Event.Publish(EventType.StaminaChanged, currentStamina, baseMaxStamina);
        GameManager.Event.Publish(EventType.ClimbCheck, hasClimbSurface, state, currentStamina);

        if (climbReattachTimer > 0f)
            climbReattachTimer -= dt;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= dt;

        if (coyoteTimer > 0f)
            coyoteTimer -= dt;

        if (staminaRecoverDelayTimer > 0f)
            staminaRecoverDelayTimer -= dt;

        UpdateLook();
    }

    private void FixedUpdate()
    {
        consumedStaminaThisStep = false;

        ProbeGround();
        ProbeClimbSurface();
        UpdateCoyoteTimer();
        UpdateStateTransitions();
        ApplyBodyRotation();
        TickState();
        RecoverStamina(Time.fixedDeltaTime);

        runPressedThisStep = false;
        jumpPressedThisStep = false;
    }


    // --- 이동 및 회전 처리 ---
    private void UpdateLook()
    {
        yaw += lookInput.x * lookSensitivity;
        pitch -= lookInput.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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

        float radius = Mathf.Max(0.05f, capsule.radius * groundCheckRadiusScale);
        Vector3 origin = capsule.bounds.center + Vector3.up * 0.02f;
        float castDistance = capsule.bounds.extents.y - radius + groundCheckDistance;

        if (!Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, castDistance, groundMask, QueryTriggerInteraction.Ignore))
            return;

        float angle = Vector3.Angle(hit.normal, Vector3.up);
        if (angle > maxGroundAngle)
            return;

        isGrounded = true;
        groundHit = hit;
    }

    private void ProbeClimbSurface()
    {
        hasClimbSurface = false;
        climbHit = default;

        if (climbMask.value == 0)
            return;

        Transform view = playerCamera != null ? playerCamera.transform : cameraPivot;
        if (view == null)
            view = transform;

        Vector3 originA = view.position;
        Vector3 dirA = view.forward.normalized;

        Vector3 originB = capsule.bounds.center;
        Vector3 dirB = transform.forward.normalized;

        bool found = false;
        float bestDistance = float.MaxValue;

        if (Physics.SphereCast(originA, climbCheckRadius, dirA, out RaycastHit hitA, climbCheckDistance, climbMask, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(hitA.normal, Vector3.up);
            if (angle >= minClimbAngle)
            {
                found = true;
                climbHit = hitA;
                bestDistance = hitA.distance;
            }
        }

        if (Physics.SphereCast(originB, climbCheckRadius, dirB, out RaycastHit hitB, climbCheckDistance, climbMask, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(hitB.normal, Vector3.up);
            if (angle >= minClimbAngle && (!found || hitB.distance < bestDistance))
            {
                found = true;
                climbHit = hitB;
                bestDistance = hitB.distance;
            }
        }

        hasClimbSurface = found;
    }

    private void UpdateCoyoteTimer()
    {
        if (isGrounded && state != LocomotionState.Climbing)
            coyoteTimer = coyoteTime;
    }

    private void UpdateStateTransitions()
    {
        switch (state)
        {
            case LocomotionState.Climbing:
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
                    climbSurfaceLostTimer = climbSurfaceLostGrace;
                }

                if (jumpPressedThisStep || runPressedThisStep)
                {
                    DoClimbLeap();
                    return;
                }
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
        }
    }

    private void TickGrounded()
    {
        rb.useGravity = true;

        Vector3 desiredDir = GetCameraRelativeMove();
        Vector3 targetPlanar = desiredDir * GetTargetGroundSpeed();

        Vector3 currentPlanar = new Vector3(Velocity.x, 0f, Velocity.z);
        float accel = desiredDir.sqrMagnitude > 0.0001f ? groundAcceleration : groundDeceleration;
        Vector3 newPlanar = Vector3.MoveTowards(currentPlanar, targetPlanar, accel * Time.fixedDeltaTime);

        float y = Velocity.y;
        if (y < groundStickVelocity)
            y = groundStickVelocity;

        Velocity = new Vector3(newPlanar.x, y, newPlanar.z);
    }

    private void TickAirborne()
    {
        rb.useGravity = true;

        Vector3 desiredDir = GetCameraRelativeMove();
        Vector3 currentPlanar = new Vector3(Velocity.x, 0f, Velocity.z);
        Vector3 targetPlanar = desiredDir * airMaxSpeed;

        Vector3 newPlanar = Vector3.MoveTowards(currentPlanar, targetPlanar, airAcceleration * Time.fixedDeltaTime);

        Vector3 vel = Velocity;
        vel.x = newPlanar.x;
        vel.z = newPlanar.z;
        Velocity = vel;

        ApplyExtraGravity();
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

        float drain = climbHoldDrainPerSecond;
        if (climbMove.sqrMagnitude > 0.0001f)
            drain += climbMoveExtraDrainPerSecond;

        if (!TryConsumeStamina(drain * Time.fixedDeltaTime))
        {
            ExitClimb(false);
            return;
        }

        float distanceError = climbHit.distance - climbHoldDistance;
        float correctionSpeed = Mathf.Clamp(distanceError * climbHoldSnapSpeed, -climbMaxCorrectionSpeed, climbMaxCorrectionSpeed);

        Vector3 towardWallVelocity = -wallNormal * correctionSpeed;

        Vector3 moveVelocity =
            (wallRight * moveInput.x * climbHorizontalSpeed) +
            (wallUp * moveInput.y * climbVerticalSpeed);

        Velocity = moveVelocity + towardWallVelocity;
    }

    private Vector3 GetCameraRelativeMove()
    {
        Transform basis = playerCamera != null ? playerCamera.transform : cameraPivot;
        if (basis == null)
            basis = transform;

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
            if (TryConsumeStamina(sprintDrainPerSecond * Time.fixedDeltaTime))
                return runSpeed;
        }

        return walkSpeed;
    }

    private bool TryStartGroundJump()
    {
        if (jumpBufferTimer <= 0f)
            return false;

        if (coyoteTimer <= 0f)
            return false;

        if (!TryConsumeStamina(jumpStaminaCost))
            return false;

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        Vector3 vel = Velocity;
        vel.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
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

        if (CurrentTotalStamina() < climbStartMinStamina)
            return false;

        if (!hasClimbSurface)
            return false;

        Vector3 towardWall = -climbHit.normal.normalized;
        Transform view = playerCamera != null ? playerCamera.transform : cameraPivot;
        if (view == null)
            view = transform;

        float lookDot = Vector3.Dot(view.forward.normalized, towardWall);
        if (lookDot < climbEnterLookDot)
            return false;

        Vector3 desiredMove = GetCameraRelativeMove();
        bool pushingToWall = false;

        if (desiredMove.sqrMagnitude > 0.0001f)
        {
            float moveDot = Vector3.Dot(desiredMove.normalized, towardWall);
            pushingToWall = moveDot >= climbEnterMoveDot;
        }

        if (!isGrounded)
            return true;

        return pushingToWall;
    }

    private void EnterClimb()
    {
        state = LocomotionState.Climbing;
        climbSurfaceLostTimer = climbSurfaceLostGrace;
        rb.useGravity = false;
        Velocity = Vector3.zero;
        LogState("Climb");
    }

    private void ExitClimb(bool keepCurrentHorizontal)
    {
        rb.useGravity = true;
        state = LocomotionState.Airborne;
        climbReattachTimer = climbReattachCooldown;

        Vector3 vel = Velocity;

        if (!keepCurrentHorizontal && hasClimbSurface)
        {
            Vector3 push = climbHit.normal;
            push.y = 0f;

            if (push.sqrMagnitude > 0.0001f)
            {
                push.Normalize();
                vel.x = push.x * wallDetachPush;
                vel.z = push.z * wallDetachPush;
            }
        }

        if (vel.y > -1f)
            vel.y = -1f;

        Velocity = vel;
        LogState("Fall");
    }

    private void DoClimbLeap()
    {
        if (!TryConsumeStamina(climbLeapCost))
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
        vel.x = away.x * climbLeapAwaySpeed;
        vel.z = away.z * climbLeapAwaySpeed;
        vel.y = climbLeapUpSpeed;

        rb.useGravity = true;
        state = LocomotionState.Airborne;
        climbReattachTimer = climbReattachCooldown;
        Velocity = vel;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        LogState("ClimbLeap");
    }


    private void PreventWallStickWhileAirborne()
    {
        if (state == LocomotionState.Climbing)
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
        if (extraGravityMultiplier <= 1f)
            return;

        Vector3 extraGravity = Physics.gravity * (extraGravityMultiplier - 1f);
        rb.AddForce(extraGravity, ForceMode.Acceleration);
    }

    // --- 스태미나 처리 ---
    private void RecoverStamina(float dt)
    {
        float maxStamina = CurrentMaxStamina;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        currentExtraStamina = Mathf.Max(0f, currentExtraStamina);
        if (consumedStaminaThisStep || state == LocomotionState.Climbing)
        {
            staminaRecoverDelayTimer = staminaRecoverDelay;
            return;
        }

        if (staminaRecoverDelayTimer > 0f)
            return;

        if (currentStamina < maxStamina)
            currentStamina = Mathf.MoveTowards(currentStamina, maxStamina, staminaRecoverPerSecond * dt);
    }

    private bool TryConsumeStamina(float amount)
    {
        if (amount <= 0f)
            return true;

        float total = currentStamina + currentExtraStamina;
        if (total < amount)
            return false;

        consumedStaminaThisStep = true;

        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            return true;
        }

        float remain = amount - currentStamina;
        currentStamina = 0f;
        currentExtraStamina = Mathf.Max(0f, currentExtraStamina - remain);
        return true;
    }


    private float CurrentTotalStamina()
    {
        return currentStamina + currentExtraStamina;
    }

    // 디버그 로그
    private void LogState(string next)
    {
        if (debugLogState)
            Debug.Log($"State -> {next}", this);
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
        jumpBufferTimer = jumpBufferTime;
    }

    public void OnGrab(InputValue value)
    {
        grabHeld = value.isPressed;
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
        if (!drawDebugGizmos)
            return;

        CapsuleCollider cc = GetComponent<CapsuleCollider>();
        if (cc == null)
            return;

        Transform view = playerCamera != null ? playerCamera.transform : (cameraPivot != null ? cameraPivot : transform);

        Gizmos.color = Color.green;
        float groundRadius = Mathf.Max(0.05f, cc.radius * groundCheckRadiusScale);
        Vector3 groundOrigin = cc.bounds.center + Vector3.up * 0.02f;
        float groundLen = cc.bounds.extents.y - groundRadius + groundCheckDistance;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * groundLen);
        Gizmos.DrawWireSphere(groundOrigin + Vector3.down * groundLen, groundRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(view.position, view.position + view.forward * climbCheckDistance);
        Gizmos.DrawWireSphere(view.position + view.forward * climbCheckDistance, climbCheckRadius);

        Gizmos.color = Color.yellow;
        Vector3 chest = cc.bounds.center;
        Gizmos.DrawLine(chest, chest + transform.forward * climbCheckDistance);
        Gizmos.DrawWireSphere(chest + transform.forward * climbCheckDistance, climbCheckRadius);
    }
}
