using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PrototypePlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 4.3f;
    [SerializeField] private float runSpeed = 6.2f;
    [SerializeField] private float crouchSpeed = 1.8f;
    [SerializeField] private float lookSensitivity = 0.12f;
    [SerializeField] private float interactionDistance = 2.7f;
    [SerializeField] private float cameraDistance = 3.7f;
    [SerializeField] private float cameraFollowSharpness = 12f;
    [SerializeField] private float cameraCollisionRadius = 0.22f;
    [SerializeField] private float firstPersonEyeHeight = 1.58f;
    [SerializeField] private float firstPersonCollisionRadius = 0.14f;

    private CharacterController character;
    private Camera playerCamera;
    private PrototypeScenario scenario;
    private Animator characterAnimator;
    private Transform characterVisual;
    private Renderer[] characterRenderers;
    private float cameraYaw;
    private float cameraPitch = 12f;
    private float cameraHeight;
    private float cameraHeightVelocity;
    private float cameraDistanceCurrent;
    private float verticalVelocity;
    private float shakeStrength;
    private float shakeTime;
    private float cameraShakeOffset;
    private bool controlsEnabled;
    private bool crouched;
    private bool holdingTable;
    private Vector3 holdPosition;
    private Vector3 tablePosition;
    public Vector3 TablePosition => tablePosition;
    private string activeAnimation;
    private const float StandingHeight = 1.76f;
    private const float CrouchedHeight = 0.82f;
    private const float HoldingHeight = 0.68f;

    public bool IsCrouched => crouched;
    public bool IsHoldingTable => holdingTable;
    public bool HasTableProtectionAnimation
    {
        get
        {
            if (characterAnimator == null || characterAnimator.runtimeAnimatorController == null)
                return false;

            AnimationClip[] clips = characterAnimator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;

                string name = clips[i].name;
                if (name.IndexOf("protect", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("cover", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("guard", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("hold", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }


    public bool IsHoldPoseAligned
    {
        get
        {
            Vector3 offset = transform.position - holdPosition;
            offset.y = 0f;

            Vector3 toTable = tablePosition - transform.position;
            toTable.y = 0f;

            float distance = offset.magnitude;
            float angle = toTable.sqrMagnitude > 0.001f
                ? Vector3.Angle(transform.forward, toTable)
                : 999f;

            bool aligned = holdingTable
                && distance <= 0.35f
                && toTable.sqrMagnitude > 0.001f
                && angle <= 180f;

            if (holdingTable && !aligned)
            {
                Debug.Log(
                    $"Tutunma hizası: Mesafe={distance:F2}, " +
                    $"Açı={angle:F1}, " +
                    $"Tutunuyor={holdingTable}"
                );
            }

            return aligned;
        }
    }

    public Vector3 CameraPosition => playerCamera != null ? playerCamera.transform.position : transform.position;

    public void Initialize(PrototypeScenario activeScenario, Vector3 spawnPosition, float yaw)
    {
        scenario = activeScenario;
        character = GetComponent<CharacterController>();
        transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, yaw, 0f));
        character.height = StandingHeight;
        character.center = new Vector3(0f, StandingHeight * 0.5f, 0f);
        character.radius = 0.32f;
        character.slopeLimit = 45f;
        character.stepOffset = 0.32f;

        cameraYaw = yaw;
        cameraHeight = 1.22f;
        cameraDistanceCurrent = cameraDistance;
        GameObject cameraRig = new GameObject("Oyuncu Kamera Rig'i");
        GameObject cameraObject = new GameObject("Oyuncu Kamerası");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(cameraRig.transform, false);
        playerCamera = cameraObject.AddComponent<Camera>();
        playerCamera.fieldOfView = 70f;
        playerCamera.nearClipPlane = 0.05f;
        playerCamera.farClipPlane = 120f;
        playerCamera.allowHDR = true;
        cameraObject.AddComponent<AudioListener>();
        playerCamera.transform.SetPositionAndRotation(
            transform.position + Vector3.up * firstPersonEyeHeight,
            Quaternion.Euler(cameraPitch, cameraYaw, 0f));

        LoadCharacter();
    }

    private void LoadCharacter()
    {
        GameObject characterPrefab = Resources.Load<GameObject>("PlayerCharacter/Models/CivilianMale");
        if (characterPrefab == null)
        {
            Debug.LogError("Oyuncu modeli bulunamadı: Resources/PlayerCharacter/Models/CivilianMale.fbx");
            return;
        }

        GameObject instance = Instantiate(characterPrefab, transform, false);
        instance.name = "Oyuncu Karakteri";
        characterVisual = instance.transform;
        characterVisual.localPosition = Vector3.zero;
        characterVisual.localRotation = Quaternion.identity;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        characterRenderers = renderers;
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float scale = bounds.size.y > 0.01f ? 1.75f / bounds.size.y : 1f;
            characterVisual.localScale = Vector3.one * scale;
            characterVisual.localPosition = new Vector3(0f, -bounds.min.y * scale, 0f);
        }
        else
        {
            Debug.LogError("Oyuncu modelinde görünür mesh bulunamadı.");
        }

        characterAnimator = instance.GetComponentInChildren<Animator>(true);
        if (characterAnimator == null)
            characterAnimator = instance.AddComponent<Animator>();

        RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>(
            "PlayerCharacter/PlayerCharacterController");
        if (controller != null)
            characterAnimator.runtimeAnimatorController = controller;
        else
            Debug.LogError("Oyuncu animasyon denetleyicisi bulunamadı. Unity Editor içe aktarımı tamamlamalı.");

        if (characterAnimator.avatar == null || !characterAnimator.avatar.isValid || !characterAnimator.avatar.isHuman)
        {
            Avatar[] avatars = Resources.LoadAll<Avatar>("PlayerCharacter/Models/CivilianMale");
            for (int i = 0; i < avatars.Length; i++)
            {
                if (avatars[i] != null && avatars[i].isValid && avatars[i].isHuman)
                {
                    characterAnimator.avatar = avatars[i];
                    break;
                }
            }
        }

        if (characterAnimator.avatar == null || !characterAnimator.avatar.isValid || !characterAnimator.avatar.isHuman)
            Debug.LogError("Sivil karakter için geçerli Humanoid Avatar bulunamadı. Animasyonlar çalıştırılamaz.");
        else if (characterAnimator.runtimeAnimatorController != null
            && !characterAnimator.HasState(0, Animator.StringToHash("Crouch")))
            Debug.LogError("Animator Controller içinde Crouch durumu bulunamadı.");

        characterAnimator.applyRootMotion = false;
        characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        TableHoldIK tableHoldIK = characterAnimator.gameObject.GetComponent<TableHoldIK>();

        if (tableHoldIK == null)
            tableHoldIK = characterAnimator.gameObject.AddComponent<TableHoldIK>();

        tableHoldIK.player = this;
        activeAnimation = null;
        SetAnimation("Idle");
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
    }

    public void SetShake(float strength)
    {
        shakeStrength = strength;
    }

    public bool IsInSafeZone(Vector3 center, float radius)
    {
        Vector3 offset = transform.position - center;
        offset.y = 0f;
        return offset.sqrMagnitude <= radius * radius;
    }

    public bool IsFacing(Vector3 point, float minimumDot)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return true;
        return Vector3.Dot(transform.forward, direction.normalized) >= minimumDot;
    }


    public void BeginTableHold(Vector3 alignPosition, Vector3 safeTable)
    {
        holdingTable = true;
        holdPosition = alignPosition;
        tablePosition = safeTable;

        SetCharacterControllerHeight(HoldingHeight);

        // Karakteri tutunma hedefine hizala.
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        transform.position = alignPosition;

        if (controller != null)
            controller.enabled = true;

        // Masaya bak.
        Vector3 toTable = safeTable - transform.position;
        toTable.y = 0f;

        if (toTable.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(toTable);

        if (characterAnimator != null)
            SetAnimation("Crouch");
    }

    public void EndTableHold()
    {
        holdingTable = false;
    }

    public void SetCrouched(bool value)
    {
        if (holdingTable)
            EndTableHold();
        crouched = value;
    }

    private void Update()
    {
        if (playerCamera == null)
            return;

        if (controlsEnabled)
        {
            ReadLook();
            ReadActions();
            ReadMovement();
            UpdateInteractionPrompt();
            UpdateTargetGuidance();
        }
    }

    private void LateUpdate()
    {
        if (playerCamera == null)
            return;

        shakeTime += Time.deltaTime;
        cameraShakeOffset = Mathf.Sin(shakeTime * 39f) * shakeStrength;
        shakeStrength = Mathf.MoveTowards(shakeStrength, 0f, Time.deltaTime * 0.045f);

        bool thirdPerson = crouched || holdingTable;
        // FPV'de kameranın içinden görünen tüm model render'larını kapat;
        // çömelme/tutunma kamerasında aynı render'ları tekrar aç.
        SetCharacterVisibility(thirdPerson);

        float desiredHeight = holdingTable ? 0.72f : 0.98f;
        cameraHeight = Mathf.SmoothDamp(cameraHeight, desiredHeight, ref cameraHeightVelocity,
            1f / Mathf.Max(1f, cameraFollowSharpness));

        Quaternion desiredRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Vector3 cameraPosition;
        if (thirdPerson)
        {
            Vector3 target = transform.position + Vector3.up * cameraHeight;
            Vector3 backward = -(desiredRotation * Vector3.forward);
            float desiredDistance = cameraDistance;
            if (Physics.SphereCast(target, cameraCollisionRadius, backward, out RaycastHit hit,
                cameraDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                desiredDistance = Mathf.Clamp(hit.distance - 0.12f, 0.48f, cameraDistance);
            }

            if (cameraDistanceCurrent <= 0f)
                cameraDistanceCurrent = desiredDistance;
            else if (desiredDistance < cameraDistanceCurrent)
                cameraDistanceCurrent = desiredDistance;
            else
                cameraDistanceCurrent = Mathf.MoveTowards(cameraDistanceCurrent, desiredDistance, Time.deltaTime * 3f);

            cameraPosition = target + backward * cameraDistanceCurrent
                + Vector3.right * cameraShakeOffset
                + Vector3.up * (Mathf.Sin(shakeTime * 51f) * shakeStrength * 0.3f);
        }
        else
        {
            Vector3 eye = transform.position + Vector3.up * firstPersonEyeHeight;
            Vector3 forward = desiredRotation * Vector3.forward;
            Vector3 collisionOrigin = transform.position + Vector3.up * 0.95f;
            Vector3 toEye = eye - collisionOrigin;
            float eyeDistance = toEye.magnitude;
            cameraPosition = eye + forward * 0.04f
                + Vector3.right * cameraShakeOffset
                + Vector3.up * (Mathf.Sin(shakeTime * 51f) * shakeStrength * 0.3f);
            if (eyeDistance > 0.01f && Physics.SphereCast(collisionOrigin, firstPersonCollisionRadius,
                toEye / eyeDistance, out RaycastHit hit, eyeDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                cameraPosition = collisionOrigin + (toEye / eyeDistance) * Mathf.Max(0.08f, hit.distance - 0.04f);
            }
        }

        playerCamera.transform.position = cameraPosition;
        playerCamera.transform.rotation = desiredRotation;
    }

    private void ReadLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        cameraYaw += delta.x * lookSensitivity;
        bool thirdPerson = crouched || holdingTable;
        cameraPitch = Mathf.Clamp(cameraPitch - delta.y * lookSensitivity,
            thirdPerson ? -18f : -75f, thirdPerson ? 68f : 78f);
    }

    private void ReadActions()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PrototypeBootstrap.Instance.TogglePause();
            return;
        }

        if (scenario != null && scenario.CurrentPhase == PrototypeScenario.Phase.Preparation
            && scenario.PreparationComplete()
            && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            scenario.StartEarthquake();
            return;
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (holdingTable && scenario != null)
                scenario.CancelTableHold("Tutunmayı bıraktın. Sarsıntı sürerken yeniden güvenli konuma geçip E'ye bas.");
            crouched = !crouched;
            return;
        }

        if (scenario != null && scenario.IsShaking)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (holdingTable)
                    scenario.CancelTableHold("Tutunmayı bıraktın. Güvenli pozisyonu 4 saniye korumazsan görev tamamlanmaz.");
                else
                    scenario.TryStartTableHold();
            }
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame && scenario != null)
        {
            PrototypeInteractable target = FindLookTarget();
            scenario.TryInteract(target);
        }
    }

    private void ReadMovement()
    {
        if (Keyboard.current == null || character == null)
            return;

        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 cameraForward = Quaternion.Euler(0f, cameraYaw, 0f) * Vector3.forward;
        Vector3 cameraRight = Quaternion.Euler(0f, cameraYaw, 0f) * Vector3.right;
        Vector3 moveDirection = cameraForward * input.y + cameraRight * input.x;
        moveDirection.y = 0f;
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        float targetHeight = holdingTable ? HoldingHeight : crouched ? CrouchedHeight : StandingHeight;
        SetCharacterControllerHeight(Mathf.Lerp(character.height, targetHeight, Time.deltaTime * 12f));


        if (holdingTable)
        {
            Vector3 toHold = holdPosition - transform.position;
            toHold.y = 0f;

            if (toHold.sqrMagnitude > 0.0001f)
                character.Move(Vector3.ClampMagnitude(toHold, 2.2f * Time.deltaTime));

            Vector3 toLeg = tablePosition + new Vector3(-0.94f, 0f, -0.53f)
                - transform.position;
            toLeg.y = 0f;

            if (toLeg.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toLeg.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRotation, 360f * Time.deltaTime);
            }
        }

        else
        {
            bool running = !crouched && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            float speed = crouched ? crouchSpeed : running ? runSpeed : walkSpeed;
            Vector3 horizontalVelocity = moveDirection * speed;

            if (moveDirection.sqrMagnitude > 0.01f)
                RotatePlayer(moveDirection);
            else
                RotatePlayer(cameraForward);

            if (character.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            character.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            bool moving = input.sqrMagnitude > 0.01f;
            if (crouched)
                SetAnimation(moving ? "CrouchWalk" : "Crouch");
            else if (running && moving)
                SetAnimation("Run");
            else if (moving)
                SetAnimation("Walk");
            else
                SetAnimation("Idle");
        }

        if (holdingTable)
        {
            SetAnimation("Crouch");
        }

        if (scenario != null && scenario.IsShaking)
            scenario.RegisterHoldInput();
    }

    private void SetCharacterControllerHeight(float height)
    {
        if (character == null)
            return;
        character.height = height;
        character.radius = height <= HoldingHeight + 0.001f ? 0.28f : 0.32f;
        character.center = new Vector3(0f, height * 0.5f, 0f);
    }

    private void RotatePlayer(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;
        Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, 720f * Time.deltaTime);
    }

    private void SetAnimation(string state)
    {
        if (characterAnimator == null || characterAnimator.runtimeAnimatorController == null || activeAnimation == state)
            return;
        activeAnimation = state;
        characterAnimator.CrossFadeInFixedTime(state, 0.18f, 0);
    }

    private void SetCharacterVisibility(bool visible)
    {
        if (characterRenderers == null)
            return;

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] != null && characterRenderers[i].enabled != visible)
                characterRenderers[i].enabled = visible;
        }
    }

    private void UpdateInteractionPrompt()
    {
        if (scenario == null || PrototypeBootstrap.Instance == null)
            return;

        if (scenario.IsShaking)
        {
            PrototypeBootstrap.Instance.SetPrompt(scenario.HoldGuidanceMessage);
            return;
        }

        PrototypeInteractable target = FindLookTarget();
        if (target == null || target.IsComplete)
        {
            PrototypeBootstrap.Instance.SetPrompt(string.Empty);
            return;
        }

        if (target.Action == PrototypeAction.ShutGasValve || target.Action == PrototypeAction.OpenWindows)
        {
            PrototypeBootstrap.Instance.SetPrompt(scenario.IsPostQuake
                ? target.TargetDisplayName + "  •  [E] " + target.ActionName
                : "Bu görev sarsıntı durduktan sonra açılır.");
            return;
        }

        PrototypeInteractable active = scenario.CurrentPreparationTarget;
        string action = target == active
            ? target.TargetDisplayName + "  •  [E] " + target.ActionName
            : active != null ? "Önce aktif hedef: " + active.TargetDisplayName + "  •  " + active.ActionName
            : "Bu nesne şu an görev hedefi değil.";
        PrototypeBootstrap.Instance.SetPrompt(action);
    }

    private void UpdateTargetGuidance()
    {
        if (scenario == null || PrototypeBootstrap.Instance == null || playerCamera == null)
            return;

        Vector3 targetPosition;
        string targetName;
        string action;
        if (scenario.IsShaking)
        {
            targetPosition = scenario.HoldZonePosition;
            targetName = "Sağlam masa";
            action = scenario != null && scenario.HasTableProtectionAnimation
                ? "Güvenli bölge • C ile çök • E ile tutun"
                : "Güvenli bölge • C ile çök";
        }
        else
        {
            PrototypeInteractable target = scenario.CurrentPreparationTarget;
            if (target == null)
            {
                PrototypeBootstrap.Instance.HideTargetGuide();
                return;
            }

            Collider targetCollider = target.GetComponentInChildren<Collider>();
            targetPosition = targetCollider != null ? targetCollider.bounds.center : target.transform.position;
            targetName = target.TargetDisplayName;
            action = target.ActionName;
        }

        Vector3 direction = targetPosition - playerCamera.transform.position;
        float distance = direction.magnitude;
        bool unobstructed = true;
        if (distance > 0.05f && Physics.Raycast(playerCamera.transform.position, direction / distance,
            out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (scenario.IsShaking)
                unobstructed = hit.distance >= distance - 0.30f;
            else
            {
                PrototypeInteractable hitTask = hit.collider.GetComponentInParent<PrototypeInteractable>();
                unobstructed = hitTask != null && hitTask == scenario.CurrentPreparationTarget;
            }
        }

        PrototypeBootstrap.Instance.UpdateTargetGuide(playerCamera, targetPosition, unobstructed,
            targetName, action, distance);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!controlsEnabled)
            return;

        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    private PrototypeInteractable FindLookTarget()
    {
        if (playerCamera == null)
            return null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            return hit.collider.GetComponentInParent<PrototypeInteractable>();

        return null;
    }
}