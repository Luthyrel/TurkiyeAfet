using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class ProvinceMapView : MonoBehaviour
{
    private const int ProvinceCount = 81;
    private RectTransform mapRect;
    private RawImage highlightImage;
    private Texture2D maskTexture;
    private Texture2D highlightTexture;
    private Color32[] maskPixels;
    private Color32[] highlightPixels;
    private int hoveredPlate;
    private int focusedPlate = 6;
    private bool pointerInside;

    public event Action<int> ProvinceHovered;
    public event Action<int> ProvinceSelected;
    public event Action ContinueRequested;

    public int SelectedPlate { get; private set; }

    public void Initialize(RectTransform rect, RawImage overlay)
    {
        mapRect = rect;
        highlightImage = overlay;
        if (ProvinceManager.Instance != null)
            focusedPlate = GameManager.Instance != null
                && GameManager.Instance.SelectedDisaster == GameManager.DisasterType.Flood
                ? ProvinceManager.Instance.GetFirstFloodProvincePlate()
                : ProvinceManager.Instance.GetLastProvincePlate();

        TextAsset maskAsset = Resources.Load<TextAsset>("ProvinceMap/province-id-mask");
        if (maskAsset == null)
        {
            Debug.LogError("İl haritası seçim verisi yüklenemedi: Resources/ProvinceMap/province-id-mask.bytes");
            return;
        }

        maskTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        if (!ImageConversion.LoadImage(maskTexture, maskAsset.bytes, false))
        {
            Debug.LogError("İl haritası seçim verisi çözümlenemedi.");
            Destroy(maskTexture);
            maskTexture = null;
            return;
        }

        maskPixels = maskTexture.GetPixels32();
        highlightTexture = new Texture2D(maskTexture.width, maskTexture.height, TextureFormat.RGBA32, false, true)
        {
            name = "İl Haritası Vurgu Katmanı",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        highlightPixels = new Color32[maskPixels.Length];
        highlightImage.texture = highlightTexture;
        highlightImage.raycastTarget = false;
        RefreshHighlight();
    }

    public void FocusAndSelect(int plateCode)
    {
        if (ProvinceManager.Instance == null || !ProvinceManager.Instance.SelectProvince(plateCode))
            return;

        focusedPlate = plateCode;
        SelectedPlate = plateCode;
        ProvinceSelected?.Invoke(plateCode);
        RefreshHighlight();
    }

    public void OnDisasterTypeChanged()
    {
        ProvinceManager.Instance?.ClearSelection();
        SelectedPlate = 0;
        hoveredPlate = 0;
        focusedPlate = GameManager.Instance != null
            && GameManager.Instance.SelectedDisaster == GameManager.DisasterType.Flood
            ? ProvinceManager.Instance != null ? ProvinceManager.Instance.GetFirstFloodProvincePlate() : 74
            : ProvinceManager.Instance != null ? ProvinceManager.Instance.GetLastProvincePlate() : 6;
        ProvinceSelected?.Invoke(0);
        RefreshHighlight();
    }

    private void Update()
    {
        if (mapRect == null || maskPixels == null)
            return;

        ReadKeyboard();
        ReadPointer();
    }

    private void ReadKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
        {
            PrototypeAudio.PlayUiSelect();
            FocusAndSelect(FindSelectablePlate(SelectedPlate > 0 ? SelectedPlate : focusedPlate, -1));
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
        {
            PrototypeAudio.PlayUiSelect();
            FocusAndSelect(FindSelectablePlate(SelectedPlate > 0 ? SelectedPlate : focusedPlate, 1));
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            PrototypeAudio.PlayUiSelect();
            if (SelectedPlate == 0)
                FocusAndSelect(focusedPlate);
            if (SelectedPlate > 0)
                ContinueRequested?.Invoke();
        }
    }

    private int FindSelectablePlate(int from, int direction)
    {
        int plate = from;
        for (int attempt = 0; attempt < ProvinceCount; attempt++)
        {
            plate = Mathf.Clamp(plate + direction, 1, ProvinceCount);
            if (ProvinceManager.Instance == null
                || GameManager.Instance == null
                || GameManager.Instance.SelectedDisaster != GameManager.DisasterType.Flood
                || ProvinceManager.Instance.IsFloodProvince(plate))
                return plate;
            if (plate == 1 && direction < 0 || plate == ProvinceCount && direction > 0)
                break;
        }
        return from;
    }

    private void ReadPointer()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 screenPosition = mouse.position.ReadValue();
        bool inside = RectTransformUtility.RectangleContainsScreenPoint(mapRect, screenPosition, null);
        int nextHover = 0;
        if (inside && RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, screenPosition, null, out Vector2 local))
        {
            Rect rect = mapRect.rect;
            float u = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
            float v = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
            int x = Mathf.Clamp(Mathf.FloorToInt(u * maskTexture.width), 0, maskTexture.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(v * maskTexture.height), 0, maskTexture.height - 1);
            nextHover = FindProvinceAt(x, y);
        }

        if (inside != pointerInside || nextHover != hoveredPlate)
        {
            pointerInside = inside;
            hoveredPlate = nextHover;
            ProvinceHovered?.Invoke(hoveredPlate);
            RefreshHighlight();
        }

        if (inside && mouse.leftButton.wasPressedThisFrame)
        {
            if (hoveredPlate > 0)
            {
                if (ProvinceManager.Instance != null && GameManager.Instance != null
                    && GameManager.Instance.SelectedDisaster == GameManager.DisasterType.Flood
                    && !ProvinceManager.Instance.IsFloodProvince(hoveredPlate))
                {
                    ProvinceHovered?.Invoke(-2);
                    return;
                }
                PrototypeAudio.PlayUiSelect();
                FocusAndSelect(hoveredPlate);
            }
            else
                ProvinceHovered?.Invoke(-1);
        }
    }

    private int FindProvinceAt(int x, int y)
    {
        int center = ReadPlatePixel(x, y);
        if (center > 0)
            return center;

        // Province borders are antialiased in the display texture; search the data mask near a border click.
        for (int radius = 1; radius <= 6; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int top = ReadPlatePixel(x + dx, y + radius);
                if (top > 0) return top;
                int bottom = ReadPlatePixel(x + dx, y - radius);
                if (bottom > 0) return bottom;
            }
            for (int dy = -radius + 1; dy < radius; dy++)
            {
                int right = ReadPlatePixel(x + radius, y + dy);
                if (right > 0) return right;
                int left = ReadPlatePixel(x - radius, y + dy);
                if (left > 0) return left;
            }
        }
        return 0;
    }

    private int ReadPlatePixel(int x, int y)
    {
        if (x < 0 || x >= maskTexture.width || y < 0 || y >= maskTexture.height)
            return 0;
        int plate = maskPixels[y * maskTexture.width + x].r;
        return plate >= 1 && plate <= ProvinceCount ? plate : 0;
    }

    private void RefreshHighlight()
    {
        if (highlightTexture == null || maskPixels == null)
            return;

        Color32 selectedFill = new Color32(51, 207, 177, 112);
        Color32 hoveredFill = new Color32(255, 196, 96, 118);
        Color32 selectedEdge = new Color32(171, 255, 231, 250);
        Color32 hoveredEdge = new Color32(255, 230, 168, 255);

        for (int y = 0; y < maskTexture.height; y++)
        for (int x = 0; x < maskTexture.width; x++)
        {
            int index = y * maskTexture.width + x;
            int plate = maskPixels[index].r;
            bool selected = plate == SelectedPlate && SelectedPlate > 0;
            bool hovered = plate == hoveredPlate && hoveredPlate > 0;
            bool completed = plate > 0 && ProvinceManager.Instance != null
                && GameManager.Instance != null
                && ProvinceManager.Instance.IsScenarioCompleted(plate, GameManager.Instance.SelectedDisaster);
            if (!selected && !hovered)
            {
                bool floodMode = GameManager.Instance != null
                    && GameManager.Instance.SelectedDisaster == GameManager.DisasterType.Flood;
                bool floodProvince = floodMode && plate > 0 && ProvinceManager.Instance != null
                    && ProvinceManager.Instance.IsFloodProvince(plate);
                highlightPixels[index] = completed
                    ? new Color32(116, 126, 136, 190)
                    : floodMode && plate > 0
                        ? floodProvince ? new Color32(38, 155, 143, 78) : new Color32(55, 65, 76, 92)
                        : new Color32(0, 0, 0, 0);
                continue;
            }

            bool edge = x == 0 || y == 0 || x == maskTexture.width - 1 || y == maskTexture.height - 1
                || ReadPlatePixel(x - 1, y) != plate || ReadPlatePixel(x + 1, y) != plate
                || ReadPlatePixel(x, y - 1) != plate || ReadPlatePixel(x, y + 1) != plate;
            highlightPixels[index] = edge
                ? (hovered ? hoveredEdge : selectedEdge)
                : completed ? new Color32(116, 126, 136, 190)
                : (hovered ? hoveredFill : selectedFill);
        }

        highlightTexture.SetPixels32(highlightPixels);
        highlightTexture.Apply(false, false);
    }

    private void OnDestroy()
    {
        if (maskTexture != null)
            Destroy(maskTexture);
        if (highlightTexture != null)
            Destroy(highlightTexture);
    }
}
