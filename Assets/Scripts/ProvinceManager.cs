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
    // A focused pilot set of provinces with documented major floods, recurring river-flood
    // exposure, or official basin flood-management coverage. This is not a ranking or a
    // claim that other Turkish provinces are flood-free. Example official records:
    // AFAD 2021 Bartın/Kastamonu/Sinop, AFAD 2021 Rize/Artvin, and provincial IRAP/SYGM plans.
    private static readonly HashSet<int> FloodProvincePlates = new HashSet<int>
    {
        1, 7, 8, 16, 22, 28, 33, 34, 35, 37, 39, 52, 53, 55, 57, 59, 61, 67, 74, 81
    };
    private static readonly HashSet<int> LandslideTrainingPlates = new HashSet<int>
    {
        12, 24, 29, 30, 62, 69, 75
    };

    public static ProvinceManager Instance { get; private set; }

    private readonly Dictionary<int, ProvinceInfo> byPlate = new Dictionary<int, ProvinceInfo>();
    private readonly List<ProvinceInfo> provinces = new List<ProvinceInfo>();

    public IReadOnlyList<ProvinceInfo> Provinces => provinces;
    public ProvinceInfo SelectedProvince { get; private set; }

    public bool IsFloodProvince(int plateCode) => FloodProvincePlates.Contains(plateCode);
    public bool IsLandslideTrainingProvince(int plateCode) => LandslideTrainingPlates.Contains(plateCode);

    public GameManager.DisasterType GetPrimaryDisasterForProvince(int plateCode)
    {
        if (IsLandslideTrainingProvince(plateCode))
            return GameManager.DisasterType.Landslide;
        return IsFloodProvince(plateCode)
            ? GameManager.DisasterType.Flood
            : GameManager.DisasterType.Earthquake;
    }

    public int GetFirstLandslideTrainingProvincePlate()
    {
        foreach (ProvinceInfo province in provinces)
            if (IsLandslideTrainingProvince(province.plateCode))
                return province.plateCode;
        return 24;
    }

    public int GetFirstFloodProvincePlate()
    {
        foreach (ProvinceInfo province in provinces)
            if (IsFloodProvince(province.plateCode))
                return province.plateCode;
        return 74;
    }

    public bool IsScenarioCompleted(int plateCode, GameManager.DisasterType disaster)
    {
        if (GetPrimaryDisasterForProvince(plateCode) != disaster)
            return false;

        string name = GetDisasterSaveName(disaster);
        bool provinceComplete = PlayerPrefs.GetInt(ProgressKey("TurkiyeAfet." + name + ".Completed", plateCode), 0) == 1;
        return provinceComplete || HasAnyRecordedCompletion(disaster);
    }

    private bool HasAnyRecordedCompletion(GameManager.DisasterType disaster)
    {
        string name = GetDisasterSaveName(disaster);
        if (PlayerPrefs.GetInt("TurkiyeAfet." + name + ".Completed", 0) == 1)
            return true;

        // Migrate old province-specific saves into the hazard-wide completed state.
        for (int plate = 1; plate <= 81; plate++)
        {
            if (PlayerPrefs.GetInt(ProgressKey("TurkiyeAfet." + name + ".Completed", plate), 0) == 1)
                return true;
        }
        return disaster == GameManager.DisasterType.Earthquake
            && PlayerPrefs.GetInt("TurkiyeAfet.Deprem.Completed", 0) == 1;
    }

    public static string GetDisasterSaveName(GameManager.DisasterType disaster)
    {
        return disaster switch
        {
            GameManager.DisasterType.Flood => "Sel",
            GameManager.DisasterType.Landslide => "Heyelan",
            _ => "Deprem"
        };
    }

    public void ClearSelection() => SelectedProvince = null;

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
        GameManager.DisasterType selectedDisaster = GameManager.Instance != null
            ? GameManager.Instance.SelectedDisaster : GameManager.DisasterType.Earthquake;
        string completedKey = "TurkiyeAfet." + GetDisasterSaveName(selectedDisaster) + ".Completed";
        int plate = PlayerPrefs.GetInt("TurkiyeAfet.LastCompletedProvince", GetLastProvincePlate());
        ProvinceInfo recent = GetProvince(plate);
        if (recent != null && IsProvinceForDisaster(plate, selectedDisaster)
            && PlayerPrefs.GetInt(ProgressKey(completedKey, plate),
                selectedDisaster == GameManager.DisasterType.Earthquake && plate == 6 ? PlayerPrefs.GetInt("TurkiyeAfet.Deprem.Completed", 0) : 0) == 1)
            return plate;

        foreach (ProvinceInfo province in provinces)
        {
            if (IsProvinceForDisaster(province.plateCode, selectedDisaster)
                && PlayerPrefs.GetInt(ProgressKey(completedKey, province.plateCode),
                    selectedDisaster == GameManager.DisasterType.Earthquake && province.plateCode == 6 ? PlayerPrefs.GetInt("TurkiyeAfet.Deprem.Completed", 0) : 0) == 1)
                return province.plateCode;
        }
        return selectedDisaster switch
        {
            GameManager.DisasterType.Flood => GetFirstFloodProvincePlate(),
            GameManager.DisasterType.Landslide => GetFirstLandslideTrainingProvincePlate(),
            _ => 6
        };
    }

    private bool IsProvinceForDisaster(int plateCode, GameManager.DisasterType disaster)
    {
        return GetPrimaryDisasterForProvince(plateCode) == disaster;
    }

    public static string ProgressKey(string key, int plateCode)
    {
        return key + "." + plateCode.ToString("00");
    }

    public bool HasAnyCompletedScenario()
    {
        if (HasAnyCompletedScenario(GameManager.DisasterType.Earthquake))
            return true;
        return HasAnyCompletedScenario(GameManager.DisasterType.Flood)
            || HasAnyCompletedScenario(GameManager.DisasterType.Landslide);
    }

    public bool HasAnyCompletedScenario(GameManager.DisasterType disaster)
    {
        foreach (ProvinceInfo province in provinces)
        {
            if (GetPrimaryDisasterForProvince(province.plateCode) == disaster
                && IsScenarioCompleted(province.plateCode, disaster))
                return true;
        }

        // Preserve a completion saved by the original Ankara-only prototype.
        return disaster == GameManager.DisasterType.Earthquake
            && PlayerPrefs.GetInt("TurkiyeAfet.Deprem.Completed", 0) == 1;
    }

    public void ResetCompletionProgress()
    {
        for (int plate = 1; plate <= 81; plate++)
        {
            PlayerPrefs.DeleteKey(ProgressKey("TurkiyeAfet.Deprem.Completed", plate));
            PlayerPrefs.DeleteKey(ProgressKey("TurkiyeAfet.Sel.Completed", plate));
            PlayerPrefs.DeleteKey(ProgressKey("TurkiyeAfet.Heyelan.Completed", plate));
        }

        // Clear the older hazard-wide completion flags too; keep scores and other preferences.
        PlayerPrefs.DeleteKey("TurkiyeAfet.Deprem.Completed");
        PlayerPrefs.DeleteKey("TurkiyeAfet.Sel.Completed");
        PlayerPrefs.DeleteKey("TurkiyeAfet.Heyelan.Completed");
        PlayerPrefs.Save();
    }
}
