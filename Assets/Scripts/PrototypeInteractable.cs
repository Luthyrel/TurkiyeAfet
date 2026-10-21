using UnityEngine;

public enum PrototypeAction
{
    SecureFurniture,
    LowerHeavyItem,
    ClearExit,
    ShutGasValve,
    OpenWindows
}

public sealed class PrototypeInteractable : MonoBehaviour
{
    [SerializeField] private PrototypeAction action;
    [SerializeField] private string actionName = "Etkileşim";
    [SerializeField] private Transform objectToMove;
    [SerializeField] private Vector3 completedPosition;
    [SerializeField] private bool moveObjectOnComplete;

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool guidanceActive;

    public PrototypeAction Action => action;
    public string ActionName => actionName;
    public string TargetDisplayName => gameObject.name;
    public bool IsComplete { get; private set; }

    public void Configure(PrototypeAction newAction, string newActionName)
    {
        action = newAction;
        actionName = newActionName;
    }

    public void ConfigureMove(Transform target, Vector3 destination)
    {
        objectToMove = target;
        completedPosition = destination;
        moveObjectOnComplete = true;
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

        CacheRenderers();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                SetMaterialColor(renderers[i].material, new Color(0.20f, 0.68f, 0.51f));
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

