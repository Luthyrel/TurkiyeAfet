using System;
using System.Collections.Generic;
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
    private List<int>[] provincePixelIndices;
    private Rect[] provinceBounds;
    private bool[] completedCache;
    private int displayedSelectedPlate = -1;
    private int displayedHoveredPlate = -1;
    private bool hasDisplayState;
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
        {
            GameManager.DisasterType selectedDisaster = GameManager.Instance != null
                ? GameManager.Instance.SelectedDisaster : GameManager.DisasterType.Earthquake;
            focusedPlate = selectedDisaster switch
            {
                GameManager.DisasterType.Flood => ProvinceManager.Instance.GetFirstFloodProvincePlate(),
                GameManager.DisasterType.Landslide => ProvinceManager.Instance.GetFirstLandslideTrainingProvincePlate(),
                _ => ProvinceManager.Instance.GetLastProvincePlate()
            };
        }

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
        BuildProvincePixelCache();
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
        GameManager.DisasterType selectedDisaster = GameManager.Instance != null
            ? GameManager.Instance.SelectedDisaster : GameManager.DisasterType.Earthquake;
        focusedPlate = selectedDisaster switch
        {
            GameManager.DisasterType.Flood => ProvinceManager.Instance != null ? ProvinceManager.Instance.GetFirstFloodProvincePlate() : 74,
            GameManager.DisasterType.Landslide => ProvinceManager.Instance != null ? ProvinceManager.Instance.GetFirstLandslideTrainingProvincePlate() : 24,
            _ => ProvinceManager.Instance != null ? ProvinceManager.Instance.GetLastProvincePlate() : 6
        };
        ProvinceSelected?.Invoke(0);
        RefreshHighlight();
    }

    public void RefreshProgress()
    {
        hasDisplayState = false;
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
        return Mathf.Clamp(from + direction, 1, ProvinceCount);
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

        bool rebuildBase = !hasDisplayState;
        if (rebuildBase)
        {
            CacheCompletionState();
            for (int index = 0; index < maskPixels.Length; index++)
            {
                int plate = maskPixels[index].r;
                highlightPixels[index] = plate > 0 && completedCache[plate]
                    ? new Color32(116, 126, 136, 190)
                    : new Color32(0, 0, 0, 0);
            }
            displayedSelectedPlate = -1;
            displayedHoveredPlate = -1;
            hasDisplayState = true;
            highlightTexture.SetPixels32(highlightPixels);
            highlightTexture.Apply(false, false);
        }

        HashSet<int> changedPlates = new HashSet<int>();
        if (displayedSelectedPlate > 0) changedPlates.Add(displayedSelectedPlate);
        if (displayedHoveredPlate > 0) changedPlates.Add(displayedHoveredPlate);
        if (SelectedPlate > 0) changedPlates.Add(SelectedPlate);
        if (hoveredPlate > 0) changedPlates.Add(hoveredPlate);

        foreach (int plate in changedPlates)
            RedrawProvince(plate);

        if (changedPlates.Count > 0)
            highlightTexture.Apply(false, false);

        displayedSelectedPlate = SelectedPlate;
        displayedHoveredPlate = hoveredPlate;
    }

    private void BuildProvincePixelCache()
    {
        provincePixelIndices = new List<int>[ProvinceCount + 1];
        provinceBounds = new Rect[ProvinceCount + 1];
        int[] minX = new int[ProvinceCount + 1];
        int[] minY = new int[ProvinceCount + 1];
        int[] maxX = new int[ProvinceCount + 1];
        int[] maxY = new int[ProvinceCount + 1];
        for (int plate = 1; plate <= ProvinceCount; plate++)
        {
            provincePixelIndices[plate] = new List<int>();
            minX[plate] = maskTexture.width;
            minY[plate] = maskTexture.height;
            maxX[plate] = -1;
            maxY[plate] = -1;
        }

        for (int index = 0; index < maskPixels.Length; index++)
        {
            int plate = maskPixels[index].r;
            if (plate < 1 || plate > ProvinceCount) continue;
            provincePixelIndices[plate].Add(index);
            int x = index % maskTexture.width;
            int y = index / maskTexture.width;
            minX[plate] = Mathf.Min(minX[plate], x);
            minY[plate] = Mathf.Min(minY[plate], y);
            maxX[plate] = Mathf.Max(maxX[plate], x);
            maxY[plate] = Mathf.Max(maxY[plate], y);
        }

        for (int plate = 1; plate <= ProvinceCount; plate++)
            provinceBounds[plate] = maxX[plate] >= minX[plate]
                ? Rect.MinMaxRect(minX[plate], minY[plate], maxX[plate] + 1, maxY[plate] + 1)
                : new Rect();
    }

    private void CacheCompletionState()
    {
        completedCache = new bool[ProvinceCount + 1];
        if (ProvinceManager.Instance == null) return;
        for (int plate = 1; plate <= ProvinceCount; plate++)
        {
            GameManager.DisasterType disaster = ProvinceManager.Instance.GetPrimaryDisasterForProvince(plate);
            completedCache[plate] = ProvinceManager.Instance.IsScenarioCompleted(plate, disaster);
        }
    }

    private void RedrawProvince(int plate)
    {
        if (plate < 1 || plate > ProvinceCount || provincePixelIndices == null) return;
        bool selected = plate == SelectedPlate;
        bool hovered = plate == hoveredPlate;
        Color32 normalFill = new Color32(0, 0, 0, 0);
        Color32 completedFill = new Color32(116, 126, 136, 190);
        Color32 selectedFill = new Color32(51, 207, 177, 112);
        Color32 hoveredFill = new Color32(255, 196, 96, 118);
        Color32 selectedEdge = new Color32(171, 255, 231, 250);
        Color32 hoveredEdge = new Color32(255, 230, 168, 255);

        List<int> indices = provincePixelIndices[plate];
        if (indices.Count == 0) return;
        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            highlightPixels[index] = completedCache[plate] ? completedFill : normalFill;
            if (!selected && !hovered) continue;
            int x = index % maskTexture.width;
            int y = index / maskTexture.width;
            bool edge = ReadPlatePixel(x - 1, y) != plate || ReadPlatePixel(x + 1, y) != plate
                || ReadPlatePixel(x, y - 1) != plate || ReadPlatePixel(x, y + 1) != plate;
            highlightPixels[index] = edge
                ? (hovered ? hoveredEdge : selectedEdge)
                : completedCache[plate] ? completedFill : hovered ? hoveredFill : selectedFill;
        }

        Rect bounds = provinceBounds[plate];
        int xMin = (int)bounds.xMin;
        int yMin = (int)bounds.yMin;
        int width = (int)bounds.width;
        int height = (int)bounds.height;
        Color32[] patch = new Color32[width * height];
        for (int y = 0; y < height; y++)
            Array.Copy(highlightPixels, (yMin + y) * maskTexture.width + xMin, patch, y * width, width);
        highlightTexture.SetPixels32(xMin, yMin, width, height, patch);
    }

    private void OnDestroy()
    {
        if (maskTexture != null)
            Destroy(maskTexture);
        if (highlightTexture != null)
            Destroy(highlightTexture);
    }
}
