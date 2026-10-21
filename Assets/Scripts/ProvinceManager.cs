using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ProvinceInfo
{
    public int plateCode;
    public string name;
    public string boundaryISO;
    public string trainingScenarioId;
    public string trainingScenarioTitle;
    public string trainingScenarioNote;

    public string PlateLabel => plateCode.ToString("00");
}

[Serializable]
public sealed class ProvinceCatalogDocument
{
    public ProvinceInfo[] provinces;
}

public sealed class ProvinceManager : MonoBehaviour
{
    public static ProvinceManager Instance { get; private set; }

    private readonly Dictionary<int, ProvinceInfo> byPlate = new Dictionary<int, ProvinceInfo>();
    private readonly List<ProvinceInfo> provinces = new List<ProvinceInfo>();

    public IReadOnlyList<ProvinceInfo> Provinces => provinces;
    public ProvinceInfo SelectedProvince { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadCatalog();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LoadCatalog()
    {
        TextAsset catalogAsset = Resources.Load<TextAsset>("ProvinceMap/provinces");
        if (catalogAsset == null)
        {
            Debug.LogError("İl kataloğu yüklenemedi: Resources/ProvinceMap/provinces.json");
            return;
        }

        ProvinceCatalogDocument document = JsonUtility.FromJson<ProvinceCatalogDocument>(catalogAsset.text);
        if (document == null || document.provinces == null)
        {
            Debug.LogError("İl kataloğu okunamadı; JSON biçimini kontrol edin.");
            return;
        }

        foreach (ProvinceInfo province in document.provinces)
        {
            if (province == null || province.plateCode < 1 || province.plateCode > 81 || string.IsNullOrWhiteSpace(province.name))
            {
                Debug.LogError("İl kataloğunda geçersiz il kaydı bulundu.");
                byPlate.Clear();
                provinces.Clear();
                return;
            }

            if (province.boundaryISO != "TR-" + province.PlateLabel || byPlate.ContainsKey(province.plateCode))
            {
                Debug.LogError("Plaka kodu ile harita sınır kimliği eşleşmiyor: " + province.name);
                byPlate.Clear();
                provinces.Clear();
                return;
            }

            byPlate.Add(province.plateCode, province);
            provinces.Add(province);
        }

        if (provinces.Count != 81 || byPlate.Count != 81)
        {
            Debug.LogError("İl kataloğu 81 benzersiz plaka kodunun tümünü içermelidir.");
            byPlate.Clear();
            provinces.Clear();
            return;
        }

        provinces.Sort((a, b) => a.plateCode.CompareTo(b.plateCode));
    }

    public ProvinceInfo GetProvince(int plateCode)
    {
        return byPlate.TryGetValue(plateCode, out ProvinceInfo province) ? province : null;
    }

    public bool SelectProvince(int plateCode)
    {
        ProvinceInfo province = GetProvince(plateCode);
        if (province == null)
            return false;

        SelectedProvince = province;
        PlayerPrefs.SetInt("TurkiyeAfet.LastProvince", plateCode);
        return true;
    }

    public int GetLastProvincePlate()
    {
        int plate = PlayerPrefs.GetInt("TurkiyeAfet.LastProvince", 6);
        return GetProvince(plate) != null ? plate : 6;
    }

    public int GetLastCompletedProvincePlate()
    {
        int plate = PlayerPrefs.GetInt("TurkiyeAfet.LastCompletedProvince", GetLastProvincePlate());
        ProvinceInfo recent = GetProvince(plate);
        if (recent != null && PlayerPrefs.GetInt(ProgressKey("TurkiyeAfet.Deprem.Completed", plate),
            plate == 6 ? PlayerPrefs.GetInt("TurkiyeAfet.Deprem.Completed", 0) : 0) == 1)
            return plate;

        foreach (ProvinceInfo province in provinces)
        {
            if (PlayerPrefs.GetInt(ProgressKey("TurkiyeAfet.Deprem.Completed", province.plateCode),
                province.plateCode == 6 ? PlayerPrefs.GetInt("TurkiyeAfet.Deprem.Completed", 0) : 0) == 1)
                return province.plateCode;
        }
        return 6;
    }

    public static string ProgressKey(string key, int plateCode)
    {
        return key + "." + plateCode.ToString("00");
    }

    public bool HasAnyCompletedScenario()
    {
        foreach (ProvinceInfo province in provinces)
        {
            if (PlayerPrefs.GetInt(ProgressKey("TurkiyeAfet.Deprem.Completed", province.plateCode), 0) == 1)
                return true;
        }

        // Preserve a completion saved by the original Ankara-only prototype.
        return PlayerPrefs.GetInt("TurkiyeAfet.Deprem.Completed", 0) == 1;
    }
}
