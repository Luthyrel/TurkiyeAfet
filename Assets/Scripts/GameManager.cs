
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        MainMenu,
        ProvinceSelection,
        Scenario,
        Results
    }

    public enum DisasterType
    {
        Earthquake,
        Flood,
        Landslide,
        Wildfire,
        Storm,
        Tornado
    }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public DisasterType SelectedDisaster { get; private set; } = DisasterType.Earthquake;
    private bool startScenarioAfterReload;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log("Oyun durumu: " + CurrentState);
    }

    public void StartProvinceSelection()
    {
        ChangeState(GameState.ProvinceSelection);
    }

    public void StartScenario()
    {
        ChangeState(GameState.Scenario);
    }

    public void SelectDisaster(DisasterType disaster) => SelectedDisaster = disaster;

    public void RequestStartAfterReload() => startScenarioAfterReload = true;

    public bool ConsumeStartAfterReload()
    {
        bool shouldStart = startScenarioAfterReload;
        startScenarioAfterReload = false;
        return shouldStart;
    }

    public void ShowResults()
    {
        ChangeState(GameState.Results);
    }

    public void ReturnToMenu()
    {
        ChangeState(GameState.MainMenu);
    }
}
