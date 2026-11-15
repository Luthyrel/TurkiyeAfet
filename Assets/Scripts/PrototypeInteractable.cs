using System.Collections;
using UnityEngine;

public enum PrototypeAction
{
    SecureFurniture,
    LowerHeavyItem,
    ClearExit,
    ShutGasValve,
    OpenWindows,
    PrepareDisasterBag,
    CollectBagItem,
    OpenExitDoor,
    CutFloodPower,
    RaiseFloodValuables,
    RaiseFloodElectronics,
    CallForFloodHelp,
    WaitForFloodAllClear,
    InspectLandslideWarnings,
    ReviewLandslideRoute,
    CallForLandslideHelp,
    ReportWildfire,
    CallForWildfireHelp,
    SecureStormLooseItems,
    CloseStormShutters,
    ToggleStormDoor,
    CallForStormHelp
}

public sealed class PrototypeInteractable : MonoBehaviour
{
    [SerializeField] private PrototypeAction action;
    [SerializeField] private string actionName = "Etkileşim";
    [SerializeField] private Transform objectToMove;
    [SerializeField] private Vector3 completedPosition;
    [SerializeField] private bool moveObjectOnComplete;
    [SerializeField] private GameObject closedBagLid;
    [SerializeField] private GameObject openBagInterior;
    [SerializeField] private Transform exitDoorPanel;
    [SerializeField] private Transform floodSwitchLever;
    [SerializeField] private Renderer floodSwitchIndicator;
    [SerializeField] private Transform floodPhoneHandset;
    private Transform stormDoorPivot;
    private Collider stormDoorCollider;
    private bool stormDoorOpen;

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool guidanceActive;
    private bool visualMetricsCached;
    private Vector3 originalVisualBoundsSize;
    private Vector3 originalVisualCenterOffset;
    private Vector3 heldGripAxis = Vector3.right;
    private Vector3 heldGripDirection = Vector3.up;
    private Vector3 heldGripFaceAxis = Vector3.back;
    private Vector3 floodPhoneRestPosition;
    private Quaternion floodPhoneRestRotation;
    private Coroutine floodPhoneMotion;

    public PrototypeAction Action => action;
    public string ActionName => actionName;
    public Vector3 HeldGripAxis => heldGripAxis;
    public Vector3 HeldGripDirection => heldGripDirection;
    public Vector3 HeldGripFaceAxis => heldGripFaceAxis;
    public string TargetDisplayName => action == PrototypeAction.CollectBagItem ? actionName : gameObject.name;
    public bool IsComplete { get; private set; }
    public Transform BagContentsRoot => openBagInterior != null ? openBagInterior.transform : transform;

    public void Configure(PrototypeAction newAction, string newActionName)
    {
        action = newAction;
        actionName = newActionName;
    }

    public void ConfigureHeldGrip(Vector3 localGripAxis, Vector3 palmDirection, Vector3 localFaceAxis)
    {
        // Eksenler model uzayında tanımlanır; hedef yön avuç çerçevesindedir.
        heldGripAxis = localGripAxis.sqrMagnitude > 0.0001f
            ? localGripAxis.normalized
            : Vector3.right;
        heldGripDirection = palmDirection.sqrMagnitude > 0.0001f
            ? palmDirection.normalized
            : Vector3.up;
        heldGripFaceAxis = localFaceAxis.sqrMagnitude > 0.0001f ? localFaceAxis.normalized : Vector3.back;
    }

    public void ConfigureMove(Transform target, Vector3 destination)
    {
        objectToMove = target;
        completedPosition = destination;
        moveObjectOnComplete = true;
    }

    public void ConfigureExitDoor(Transform panel)
    {
        exitDoorPanel = panel;
    }

    public void ConfigureStormDoor(Transform pivot, Collider doorCollider, bool initiallyOpen)
    {
        stormDoorPivot = pivot;
        stormDoorCollider = doorCollider;
        stormDoorOpen = initiallyOpen;
        ApplyStormDoorState();
    }

    public bool ToggleStormDoor()
    {
        if (stormDoorPivot == null) return false;
        stormDoorOpen = !stormDoorOpen;
        ApplyStormDoorState();
        actionName = stormDoorOpen ? "E ile kapıyı kapat" : "E ile kapıyı aç";
        return stormDoorOpen;
    }

    private void ApplyStormDoorState()
    {
        if (stormDoorPivot != null)
            stormDoorPivot.localRotation = Quaternion.Euler(0f, stormDoorOpen ? -90f : 0f, 0f);
        if (stormDoorCollider != null)
            // A swung-open door remains a solid object along the jamb, and its broad panel
            // collider also keeps it targetable so the player can close it again.
            stormDoorCollider.enabled = true;
    }

    public void ConfigureFloodSwitchVisual(Transform lever, Renderer indicator)
    {
        floodSwitchLever = lever;
        floodSwitchIndicator = indicator;
    }

    public void ConfigureFloodPhoneVisual(Transform handset)
    {
        floodPhoneHandset = handset;
        if (floodPhoneHandset != null)
        {
            floodPhoneRestPosition = floodPhoneHandset.localPosition;
            floodPhoneRestRotation = floodPhoneHandset.localRotation;
        }
    }

    public void SetFloodPhoneRaised(bool raised)
    {
        if (floodPhoneHandset == null)
            return;

        if (floodPhoneMotion != null)
            StopCoroutine(floodPhoneMotion);
        floodPhoneMotion = StartCoroutine(AnimateFloodPhoneHandset(raised));
    }

    private IEnumerator AnimateFloodPhoneHandset(bool raised)
    {
        Vector3 startPosition = floodPhoneHandset.localPosition;
        Quaternion startRotation = floodPhoneHandset.localRotation;
        Vector3 targetPosition = raised
            ? floodPhoneRestPosition + new Vector3(0f, 0.18f, 0.04f)
            : floodPhoneRestPosition;
        Quaternion targetRotation = raised
            ? floodPhoneRestRotation * Quaternion.Euler(0f, 0f, -26f)
            : floodPhoneRestRotation;
        const float duration = 0.24f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            floodPhoneHandset.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            floodPhoneHandset.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        floodPhoneHandset.localPosition = targetPosition;
        floodPhoneHandset.localRotation = targetRotation;
        floodPhoneMotion = null;
    }

    public void OpenExitDoor()
    {
        if (IsComplete || exitDoorPanel == null)
            return;

        exitDoorPanel.localRotation = Quaternion.Euler(0f, -90f, 0f);
        Collider[] panelColliders = exitDoorPanel.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < panelColliders.Length; i++)
            panelColliders[i].enabled = false;
        IsComplete = true;
    }

    public void ConfigureBagVisual(GameObject closedLid, GameObject openInterior)
    {
        closedBagLid = closedLid;
        openBagInterior = openInterior;
        SetBagOpenVisual(false);
    }

    public void SetBagOpenVisual(bool isOpen)
    {
        if (closedBagLid != null)
            closedBagLid.SetActive(!isOpen);
        if (openBagInterior != null)
            openBagInterior.SetActive(isOpen);
    }

    public void SetHeld(bool held)
    {
        SetGuidance(false);
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = !held;
        }
    }

    public Vector3 OriginalVisualCenterOffset
    {
        get
        {
            CacheOriginalVisualSize();
            return originalVisualCenterOffset;
        }
    }

    public Vector3 OriginalVisualBoundsSize
    {
        get
        {
            CacheOriginalVisualSize();
            return originalVisualBoundsSize;
        }
    }

    public float CacheOriginalVisualSize()
    {
        if (!visualMetricsCached)
        {
            Renderer[] itemRenderers = GetComponentsInChildren<Renderer>(true);
            bool foundRenderer = false;
            Bounds combined = new Bounds(transform.position, Vector3.zero);
            for (int i = 0; i < itemRenderers.Length; i++)
            {
                if (itemRenderers[i] == null)
                    continue;
                if (!foundRenderer)
                {
                    combined = itemRenderers[i].bounds;
                    foundRenderer = true;
                }
                else
                {
                    combined.Encapsulate(itemRenderers[i].bounds);
                }
            }

            originalVisualBoundsSize = foundRenderer ? combined.size : Vector3.one * 0.1f;
            originalVisualCenterOffset = foundRenderer ? combined.center - transform.position : Vector3.zero;
            visualMetricsCached = true;
        }

        return Mathf.Max(originalVisualBoundsSize.x, originalVisualBoundsSize.y, originalVisualBoundsSize.z, 0.01f);
    }

    public void PackBagItem(Transform bag, Vector3 floorSlot, float targetWorldSize)
    {
        if (bag == null)
        {
            gameObject.SetActive(false);
            IsComplete = true;
            return;
        }

        SetHeld(true);
        transform.SetParent(bag, false);
        transform.localRotation = Quaternion.identity;
        Vector3 bagScale = bag.lossyScale;
        float parentScale = Mathf.Max(Mathf.Abs(bagScale.x), Mathf.Abs(bagScale.y), Mathf.Abs(bagScale.z), 0.001f);
        float visualSize = CacheOriginalVisualSize();
        float scaleFactor = targetWorldSize / (visualSize * parentScale);
        transform.localScale = Vector3.one * scaleFactor;

        float verticalScale = scaleFactor * Mathf.Max(Mathf.Abs(bagScale.y), 0.001f);
        float visualHeight = originalVisualBoundsSize.y * verticalScale;
        float visualCenterY = originalVisualCenterOffset.y * verticalScale;
        transform.localPosition = floorSlot + Vector3.up * (visualHeight * 0.5f - visualCenterY);
        IsComplete = true;
    }

    public void SetGuidance(bool active)
    {
        if (guidanceActive == active || IsComplete)
            return;

        CacheRenderers();
        guidanceActive = active;
        Color guideColor = new Color(0.30f, 0.92f, 0.78f, 1f);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Material material = renderers[i].material;
            if (active)
            {
                SetMaterialColor(material, guideColor);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", guideColor * 0.22f);
                }
            }
            else
            {
                SetMaterialColor(material, originalColors[i]);
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    public void CompleteAction()
    {
        if (IsComplete)
            return;

        SetGuidance(false);
        IsComplete = true;
        if (moveObjectOnComplete && objectToMove != null)
            objectToMove.position = completedPosition;

        if (action == PrototypeAction.CutFloodPower && floodSwitchLever != null)
            floodSwitchLever.localRotation = Quaternion.Euler(0f, 0f, 35f);

        if (action == PrototypeAction.CutFloodPower && floodSwitchIndicator != null)
        {
            Material indicatorMaterial = floodSwitchIndicator.material;
            SetMaterialColor(indicatorMaterial, new Color(0.18f, 0.82f, 0.43f));
            if (indicatorMaterial.HasProperty("_EmissionColor"))
            {
                indicatorMaterial.EnableKeyword("_EMISSION");
                indicatorMaterial.SetColor("_EmissionColor", new Color(0.12f, 0.9f, 0.36f) * 0.7f);
            }
        }

        CacheRenderers();
        bool floodAction = action == PrototypeAction.CutFloodPower
            || action == PrototypeAction.RaiseFloodValuables
            || action == PrototypeAction.RaiseFloodElectronics
            || action == PrototypeAction.CallForFloodHelp
            || action == PrototypeAction.CallForWildfireHelp
            || action == PrototypeAction.WaitForFloodAllClear;
        if (action != PrototypeAction.OpenExitDoor && action != PrototypeAction.PrepareDisasterBag && !floodAction)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    SetMaterialColor(renderers[i].material, new Color(0.20f, 0.68f, 0.51f));
            }
        }

        if (action == PrototypeAction.SecureFurniture)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == "SabitlemeKayışı")
                    child.gameObject.SetActive(true);
            }
        }
    }

    private void CacheRenderers()
    {
        if (renderers != null)
            return;

        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                originalColors[i] = renderers[i].material.color;
        }
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
    }
}

