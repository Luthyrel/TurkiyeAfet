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
    private Transform firstPersonItemAnchor;
    private GameObject firstPersonHandVisual;
    private Transform firstPersonWristBone;
    private Transform firstPersonIndexRoot;
    private Transform firstPersonMiddleRoot;
    private Transform firstPersonRingRoot;
    private Transform firstPersonPinkyRoot;
    private Transform rightHandBone;
    private Transform rightLowerArmBone;
    private PrototypeInteractable heldBagItem;
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

    public void HoldBagItem(PrototypeInteractable item)
    {
        if (item == null)
            return;

        item.CacheOriginalVisualSize();
        item.SetHeld(true);
        heldBagItem = item;
        UpdateHeldItemPresentation(true);
    }

    public void ReleaseBagItem(PrototypeInteractable item)
    {
        if (heldBagItem != item)
            return;

        heldBagItem = null;
        if (firstPersonHandVisual != null)
            firstPersonHandVisual.SetActive(false);
    }

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
        CacheRightHandBone();
        SetupFirstPersonItemAnchor();
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
        crouched = true;
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
        UpdateHeldItemPresentation(false);
    }

    private void CacheRightHandBone()
    {
        rightHandBone = characterAnimator != null && characterAnimator.avatar != null
            && characterAnimator.avatar.isHuman
            ? characterAnimator.GetBoneTransform(HumanBodyBones.RightHand)
            : null;
        rightLowerArmBone = characterAnimator != null && characterAnimator.avatar != null
            && characterAnimator.avatar.isHuman
            ? characterAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm)
            : null;
    }

    private void SetupFirstPersonItemAnchor()
    {
        if (playerCamera == null)
            return;

        GameObject itemAnchor = new GameObject("Birinci Şahıs Eşya Yuvası");
        firstPersonItemAnchor = itemAnchor.transform;
        firstPersonItemAnchor.SetParent(playerCamera.transform, false);
        firstPersonItemAnchor.localPosition = new Vector3(0.72f, -0.42f, 0.69f);

        GameObject armPrefab = Resources.Load<GameObject>("WRADRightArm");
        if (armPrefab == null)
        {
            Debug.LogError("WRADRightArm modeli Resources içinde bulunamadı.");
            return;
        }

        firstPersonHandVisual = Instantiate(armPrefab, playerCamera.transform, false);
        firstPersonHandVisual.name = "WRAD Tek Parça Sağ El ve Ön Kol";
        firstPersonHandVisual.transform.localPosition = Vector3.zero;
        firstPersonHandVisual.transform.localRotation = Quaternion.identity;
        firstPersonHandVisual.transform.localScale = Vector3.one * 0.12f;
        ApplyFirstPersonSkinTexture(firstPersonHandVisual);

        firstPersonWristBone = FindHandBone(firstPersonHandVisual.transform, "wrist.r");
        Transform forearmBone = FindHandBone(firstPersonHandVisual.transform, "forearm.r");
        firstPersonIndexRoot = FindHandBone(firstPersonHandVisual.transform, "finger_index1.r");
        firstPersonMiddleRoot = FindHandBone(firstPersonHandVisual.transform, "finger_middle1.r");
        firstPersonRingRoot = FindHandBone(firstPersonHandVisual.transform, "finger_ring1.r");
        firstPersonPinkyRoot = FindHandBone(firstPersonHandVisual.transform, "finger_pinky1.r");
        if (firstPersonWristBone == null || forearmBone == null || firstPersonIndexRoot == null
            || firstPersonMiddleRoot == null || firstPersonRingRoot == null || firstPersonPinkyRoot == null)
        {
            Debug.LogError("WRAD sağ kol modelinde bilek veya parmak kemikleri bulunamadı.");
            firstPersonHandVisual.SetActive(false);
            return;
        }

        PoseFirstPersonHandGrip(firstPersonHandVisual.transform);

        Vector3 wristRoot = firstPersonHandVisual.transform.InverseTransformPoint(firstPersonWristBone.position);
        Vector3 elbowRoot = firstPersonHandVisual.transform.InverseTransformPoint(forearmBone.position);
        Vector3 armAxis = (elbowRoot - wristRoot).normalized;
        Vector3 wristTarget = new Vector3(0.72f, -0.52f, 0.84f);
        Vector3 elbowTarget = new Vector3(1.15f, -0.94f, 0.68f);
        Vector3 targetArmAxis = (elbowTarget - wristTarget).normalized;
        Quaternion alignArm = Quaternion.FromToRotation(armAxis, targetArmAxis);

        Vector3 indexLocal = firstPersonHandVisual.transform.InverseTransformPoint(firstPersonIndexRoot.position);
        Vector3 middleLocal = firstPersonHandVisual.transform.InverseTransformPoint(firstPersonMiddleRoot.position);
        Vector3 pinkyLocal = firstPersonHandVisual.transform.InverseTransformPoint(firstPersonPinkyRoot.position);
        Vector3 palmNormal = Vector3.Cross(middleLocal - wristRoot, indexLocal - pinkyLocal).normalized;
        Vector3 alignedPalmNormal = alignArm * palmNormal;
        Vector3 cameraFacingNormal = Vector3.ProjectOnPlane(-Vector3.forward, targetArmAxis).normalized;
        float palmTwist = Vector3.SignedAngle(alignedPalmNormal, cameraFacingNormal, targetArmAxis);
        firstPersonHandVisual.transform.localRotation = Quaternion.AngleAxis(palmTwist, targetArmAxis) * alignArm;

        Vector3 wristBefore = playerCamera.transform.InverseTransformPoint(firstPersonWristBone.position);
        firstPersonHandVisual.transform.localPosition += wristTarget - wristBefore;

        RefreshFirstPersonGripAnchor();
        firstPersonHandVisual.SetActive(false);
    }

    private void RefreshFirstPersonGripAnchor(float itemDepthOffset = 0f, float palmAdvance = 0f)
    {
        if (firstPersonItemAnchor == null || playerCamera == null || firstPersonWristBone == null
            || firstPersonIndexRoot == null || firstPersonMiddleRoot == null
            || firstPersonRingRoot == null || firstPersonPinkyRoot == null)
            return;

        Vector3 knuckleCenter = (firstPersonIndexRoot.position + firstPersonMiddleRoot.position
            + firstPersonRingRoot.position + firstPersonPinkyRoot.position) * 0.25f;
        Vector3 wristToKnuckles = knuckleCenter - firstPersonWristBone.position;
        Vector3 fingerAxis = wristToKnuckles.normalized;
        Vector3 acrossPalm = Vector3.ProjectOnPlane(
            firstPersonIndexRoot.position - firstPersonPinkyRoot.position, fingerAxis).normalized;
        Vector3 palmNormal = Vector3.Cross(acrossPalm, fingerAxis).normalized;
        Vector3 palmCenter = Vector3.Lerp(firstPersonWristBone.position, knuckleCenter, 0.68f)
            + fingerAxis * wristToKnuckles.magnitude * palmAdvance;
        Vector3 towardViewer = (playerCamera.transform.position - palmCenter).normalized;
        if (Vector3.Dot(palmNormal, towardViewer) < 0f)
            palmNormal = -palmNormal;

        // The palm frame comes from wrist/knuckle geometry. Local +Y follows the fingers,
        // and local -Z faces out through the palm toward the held object and camera.
        Quaternion palmFrame = Quaternion.LookRotation(-palmNormal, fingerAxis);
        firstPersonItemAnchor.position = palmCenter + palmNormal * (0.006f + itemDepthOffset);
        firstPersonItemAnchor.rotation = palmFrame;
    }

    private void ApplyFirstPersonSkinTexture(GameObject handRoot)
    {
        Texture2D skinTexture = Resources.Load<Texture2D>("WRADArmTanBase");
        if (skinTexture == null)
        {
            Debug.LogWarning("WRAD kol ten dokusu Resources içinde bulunamadı.");
            return;
        }

        SkinnedMeshRenderer[] renderers = handRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Material[] materials = renderers[rendererIndex].materials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;
                material.mainTexture = skinTexture;
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", skinTexture);
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", skinTexture);
                Color warmSkinTint = new Color(1.08f, 1.04f, 0.96f, 1f);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", warmSkinTint);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", warmSkinTint);
            }
            renderers[rendererIndex].SetPropertyBlock(null);
            renderers[rendererIndex].materials = materials;
        }
    }

    private Transform FindHandBone(Transform handRoot, string boneName)
    {
        Transform[] bones = handRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i].name == boneName)
                return bones[i];
        }
        return null;
    }
    private void PoseFirstPersonHandGrip(Transform handRoot)
    {
        // Varlığın iskeletindeki dört parmak ve başparmak, avuç içine kapanır.
        ApplyFingerCurl(handRoot, "finger_index", 18f, 30f, 16f);
        ApplyFingerCurl(handRoot, "finger_middle", 20f, 34f, 18f);
        ApplyFingerCurl(handRoot, "finger_ring", 22f, 36f, 20f);
        ApplyFingerCurl(handRoot, "finger_pinky", 24f, 38f, 22f);
        ApplyFingerCurl(handRoot, "finger_thumb", 18f, 26f, 14f);
    }

    private void ApplyFingerCurl(Transform handRoot, string fingerName, float first, float second, float third)
    {
        ApplyBoneCurl(handRoot, fingerName + "1.r", first);
        ApplyBoneCurl(handRoot, fingerName + "2.r", second);
        ApplyBoneCurl(handRoot, fingerName + "3.r", third);
    }

    private void ApplyBoneCurl(Transform handRoot, string boneName, float degrees)
    {
        Transform[] bones = handRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i].name != boneName)
                continue;

            bones[i].localRotation *= Quaternion.Euler(degrees, 0f, 0f);
            return;
        }
    }

    private void UpdateHeldItemPresentation(bool forceReparent)
    {
        if (heldBagItem == null || playerCamera == null)
            return;

        bool thirdPerson = crouched || holdingTable;
        bool useWorldHandPosition = thirdPerson && rightHandBone != null;
        bool showFirstPersonHand = !useWorldHandPosition;
        if (firstPersonHandVisual != null && firstPersonHandVisual.activeSelf != showFirstPersonHand)
            firstPersonHandVisual.SetActive(showFirstPersonHand);

        float originalSize = heldBagItem.CacheOriginalVisualSize();
        if (useWorldHandPosition)
        {
            if (heldBagItem.transform.parent != null || forceReparent)
                heldBagItem.transform.SetParent(null, true);

            // Keep the item at the animated wrist in world units. Hand-bone scale varies
            // between imported rigs, so the item itself stays outside that scaled hierarchy.
            Vector3 towardCamera = (playerCamera.transform.position - rightHandBone.position).normalized;
            Quaternion facingCamera = Quaternion.LookRotation(-towardCamera, Vector3.up);
            float worldScale = 0.18f / originalSize;
            heldBagItem.transform.rotation = facingCamera;
            heldBagItem.transform.localScale = Vector3.one * worldScale;
            Vector3 forearmDirection = rightLowerArmBone != null
                ? rightHandBone.position - rightLowerArmBone.position
                : transform.forward;
            if (forearmDirection.sqrMagnitude < 0.0001f)
                forearmDirection = transform.forward;
            forearmDirection.Normalize();
            Vector3 handCenter = rightHandBone.position
                + forearmDirection * 0.15f
                + towardCamera * 0.035f;
            Vector3 visualCenterOffset = facingCamera * (heldBagItem.OriginalVisualCenterOffset * worldScale);
            heldBagItem.transform.position = handCenter - visualCenterOffset;
            return;
        }

        Transform holder = firstPersonItemAnchor != null ? firstPersonItemAnchor : playerCamera.transform;
        if (forceReparent || heldBagItem.transform.parent != holder)
            heldBagItem.transform.SetParent(holder, false);

        heldBagItem.transform.localRotation = Quaternion.identity;
        Vector3 inherited = holder.lossyScale;
        float inheritedScale = Mathf.Max(Mathf.Abs(inherited.x), Mathf.Abs(inherited.y), Mathf.Abs(inherited.z), 0.001f);
        float desiredSize = GetFirstPersonHeldItemSize(heldBagItem.ActionName);
        float localScale = desiredSize / (originalSize * inheritedScale);
        heldBagItem.transform.localScale = Vector3.one * localScale;

        Quaternion gripCorrection = Quaternion.identity;
        if (firstPersonItemAnchor != null)
        {
            gripCorrection = GetGripAxisCorrection(heldBagItem.HeldGripAxis, heldBagItem.HeldGripDirection,
                heldBagItem.HeldGripFaceAxis);
            Vector3 itemBounds = heldBagItem.OriginalVisualBoundsSize;
            float itemDepth = GetGripFrameDepth(itemBounds, gripCorrection) * localScale * inheritedScale;
            float palmAdvanceRatio = 0f;
            if (string.Equals(heldBagItem.ActionName, "Acil durum radyosu", System.StringComparison.OrdinalIgnoreCase))
                palmAdvanceRatio = 0.25f;
            else if (string.Equals(heldBagItem.ActionName, "Cep telefonu", System.StringComparison.OrdinalIgnoreCase))
                palmAdvanceRatio = 0.10f;
            else if (string.Equals(heldBagItem.ActionName, "Düdük", System.StringComparison.OrdinalIgnoreCase))
                palmAdvanceRatio = 0.16f;
            RefreshFirstPersonGripAnchor(itemDepth * 0.5f + 0.006f, palmAdvanceRatio);
            if (string.Equals(heldBagItem.ActionName, "Acil durum radyosu", System.StringComparison.OrdinalIgnoreCase))
                firstPersonItemAnchor.position += playerCamera.transform.right * 0.04f;
            firstPersonItemAnchor.localRotation *= gripCorrection;
        }

        // The item is a child of the corrected anchor, so this local offset stays in model axes.
        Vector3 firstPersonVisualCenterOffset = heldBagItem.OriginalVisualCenterOffset;
        heldBagItem.transform.localPosition = -firstPersonVisualCenterOffset * localScale;
    }

    private float GetGripFrameDepth(Vector3 modelBoundsSize, Quaternion gripCorrection)
    {
        Vector3 correctedX = gripCorrection * Vector3.right;
        Vector3 correctedY = gripCorrection * Vector3.up;
        Vector3 correctedZ = gripCorrection * Vector3.forward;
        return Mathf.Abs(correctedX.z) * modelBoundsSize.x
            + Mathf.Abs(correctedY.z) * modelBoundsSize.y
            + Mathf.Abs(correctedZ.z) * modelBoundsSize.z;
    }

    private Quaternion GetGripAxisCorrection(Vector3 localGripAxis, Vector3 targetPalmDirection, Vector3 localFaceAxis)
    {
        // Uzun eksen tek başına yüzün hangi tarafa bakacağını belirlemez; iki ekseni birlikte hizala.
        Vector3 sourceUp = localGripAxis.sqrMagnitude > 0.0001f ? localGripAxis.normalized : Vector3.up;
        Vector3 sourceForward = Vector3.ProjectOnPlane(localFaceAxis, sourceUp).normalized;
        Vector3 targetUp = targetPalmDirection.sqrMagnitude > 0.0001f ? targetPalmDirection.normalized : Vector3.up;
        Vector3 targetForward = Vector3.ProjectOnPlane(Vector3.back, targetUp).normalized;
        if (sourceForward.sqrMagnitude < 0.0001f || targetForward.sqrMagnitude < 0.0001f)
            return Quaternion.FromToRotation(sourceUp, targetUp);

        Quaternion sourceFrame = Quaternion.LookRotation(sourceForward, sourceUp);
        Quaternion targetFrame = Quaternion.LookRotation(targetForward, targetUp);
        return targetFrame * Quaternion.Inverse(sourceFrame);
    }

    private float GetFirstPersonHeldItemSize(string actionName)
    {
        if (string.Equals(actionName, "Cep telefonu", System.StringComparison.OrdinalIgnoreCase))
            return 0.15f;
        if (string.Equals(actionName, "Su şişesi", System.StringComparison.OrdinalIgnoreCase))
            return 0.12f;
        if (string.Equals(actionName, "Düdük", System.StringComparison.OrdinalIgnoreCase))
            return 0.13f;
        if (string.Equals(actionName, "İlk yardım çantası", System.StringComparison.OrdinalIgnoreCase))
            return 0.10f;
        if (string.Equals(actionName, "Acil durum radyosu", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(actionName, "Uzun ömürlü gıda", System.StringComparison.OrdinalIgnoreCase))
            return 0.11f;
        return 0.12f;
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
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            PrototypeBootstrap.Instance.TogglePause();
            return;
        }

        if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
        {
            if (holdingTable && scenario != null)
                scenario.CancelTableHold("Tutunmayı bıraktın. Sarsıntı sürerken yeniden güvenli konuma geçip E veya sol tıkla tutun.");
            crouched = !crouched;
            return;
        }

        bool interactPressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (!interactPressed || scenario == null)
            return;

        if (scenario.IsShaking)
        {
            if (holdingTable)
                scenario.CancelTableHold("Tutunmayı bıraktın. Güvenli pozisyonu 5 saniye korumazsan görev tamamlanmaz.");
            else
                scenario.TryStartTableHold();
            return;
        }

        scenario.TryInteract(FindLookTarget());
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

        if (target.Action == PrototypeAction.PrepareDisasterBag)
        {
            string bagPrompt = !scenario.IsDisasterBagOpen
                ? "Afet çantası  •  [E / Sol tık] Aç"
                : scenario.HasItemInHand
                    ? "Afet çantası  •  [E / Sol tık] İçine koy"
                    : "Afet çantası  •  Eşya koy veya tekrar etkileşip bonusu atla";
            PrototypeBootstrap.Instance.SetPrompt(bagPrompt);
            return;
        }

        if (target.Action == PrototypeAction.CollectBagItem)
        {
            string itemPrompt = scenario.HasItemInHand
                ? "Önce elindekini çantaya koy."
                : !scenario.IsDisasterBagOpen
                    ? target.TargetDisplayName + "  •  Önce afet çantasını aç"
                    : target.TargetDisplayName + "  •  [E / Sol tık] Al";
            PrototypeBootstrap.Instance.SetPrompt(itemPrompt);
            return;
        }

        if (target.Action == PrototypeAction.OpenExitDoor)
        {
            PrototypeBootstrap.Instance.SetPrompt(scenario.IsPostQuake
                ? target.TargetDisplayName + "  •  [E / Sol tık] Aç"
                : "Kapı deprem bittikten sonra açılabilir.");
            return;
        }

        if (target.Action == PrototypeAction.ShutGasValve || target.Action == PrototypeAction.OpenWindows)
        {
            PrototypeBootstrap.Instance.SetPrompt(scenario.IsPostQuake
                ? target.TargetDisplayName + "  •  [E / Sol tık] " + target.ActionName
                : "Bu görev sarsıntı durduktan sonra açılır.");
            return;
        }

        PrototypeInteractable active = scenario.CurrentPreparationTarget;
        string action = target == active
            ? target.TargetDisplayName + "  •  [E / Sol tık] " + target.ActionName
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
            action = "Yeşil alanın içindeyken E veya sol tıkla tutun";
        }
        else
        {
            if (scenario.IsDisasterBagOpen)
            {
                PrototypeBootstrap.Instance.HideTargetGuide();
                return;
            }

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