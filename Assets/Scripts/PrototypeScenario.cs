using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PrototypeScenario : MonoBehaviour
{
    public enum Phase { Preparation, Shaking, Blackout, Trapped, Aftershock, Completed }

    private const float QuakeDuration = 10f;
    private const float RequiredHoldDuration = 5f;
    private const float HoldZoneRadius = 0.72f;
    private const float CollapseChance = 0.5f;
    private const float BlackoutDuration = 2.5f;

    private static readonly string[] TrappedQuestions =
    {
        "Enkaz altında ağzını ve burnunu nasıl korursun?",
        "Sıkıştıysan veya yaralıysan ne yaparsın?",
        "Kurtarma ekibine yerini nasıl bildirirsin?"
    };

    private static readonly string[][] TrappedChoices =
    {
        new[] { "Yakındaki bez veya giysiyle ağzımı ve burnumu örterim.", "Kibrit yakıp çevreyi görmeye çalışırım.", "Hızlı hareket ederek etrafımdaki tozu dağıtırım." },
        new[] { "Üzerime düşen parçaları kuvvetle itmeye çalışırım.", "Durumumu sakinçe değerlendirir, riskli hareketlerden kaçınır ve enerjimi korurum.", "Kurtulmak için sürekli hareket ederim." },
        new[] { "Hiç durmadan bağırırım.", "Sesleri dinler, aralıklı vurarak veya düdükle varlığımı duyururum.", "Elektrik düğmelerini açıp kapatırım." }
    };

    private static readonly int[] TrappedCorrectChoices = { 0, 1, 1 };
    private static readonly string[] TrappedCorrectFeedback =
    {
        "Doğru. Bez veya giysi tozu solumayı azaltmaya yardımcı olur.",
        "Doğru. Durumunu kontrol et; tehlikeli hareketlerden kaçın ve enerjini koru.",
        "Doğru. Kurtarma ekiplerinin seslerini dinle; aralıklı ses ve enerjini koruyarak yerini bildir."
    };

    private readonly List<PrototypeInteractable> preparationTasks = new List<PrototypeInteractable>();
    private readonly List<Rigidbody> looseObjects = new List<Rigidbody>();
    private readonly Dictionary<Rigidbody, PrototypeInteractable> furniture = new Dictionary<Rigidbody, PrototypeInteractable>();

    private PrototypeBootstrap bootstrap;
    private PrototypePlayerController player;
    private Vector3 safeTablePosition;
    private float quakeTimer;
    private float holdTimer;
    private bool safeActionComplete;
    private bool gasValveClosed;
    private bool windowsOpened;
    private bool exitHintShown;
    private bool collapseOccurred;
    private int trappedQuestionIndex;

    public Phase CurrentPhase { get; private set; } = Phase.Preparation;
    public bool IsShaking => CurrentPhase == Phase.Shaking;
    public bool IsTrapped => CurrentPhase == Phase.Trapped;
    public bool IsPostQuake => CurrentPhase == Phase.Aftershock;
    public int Score { get; private set; }
    public float HoldProgress => Mathf.Clamp01(holdTimer / RequiredHoldDuration);
    public Vector3 HoldZonePosition => safeTablePosition + new Vector3(-0.45f, 0f, -0.75f);
    public bool CrouchConditionComplete => safeActionComplete
        || (player != null && player.IsCrouched && player.IsInSafeZone(HoldZonePosition, HoldZoneRadius));
    public bool TableHoldConditionComplete => safeActionComplete;
    public bool IsTableHoldActive => player != null && player.IsHoldingTable;
    public bool HasTableProtectionAnimation => player != null && player.HasTableProtectionAnimation;

    public string HoldGuidanceMessage
    {
        get
        {
            if (player == null)
                return "Sağlam masanın önündeki ışıklı halkaya ilerle.";
            if (player.IsHoldingTable)
                return "Masaya tutunuyorsun. Sarsıntı boyunca pozisyonunu koru; bırakmak için E.";
            if (!player.IsInSafeZone(HoldZonePosition, HoldZoneRadius))
            {
                Vector3 offset = player.transform.position - safeTablePosition;
                offset.y = 0f;
                return offset.magnitude < 2.6f
                    ? "Masanın diğer tarafına geç. Yerdeki ışıklı halkaya gir."
                    : "Sağlam masanın önündeki ışıklı halkaya yaklaş.";
            }
            if (!player.IsCrouched)
                return "Güvenli bölgedesin. Önce C ile çök; sonra masaya dönüp E ile tutun.";
            if (!player.IsFacing(safeTablePosition, 0.65f))
                return "Çökme tamamlandı. Masaya dön; sonra E ile tutun.";
            if (!player.HasTableProtectionAnimation)
                return "Çökme tamamlandı. Bu karakter için doğrulanmış masa tutunma animasyonu yok.";
            return "✓ Çökme tamamlandı. E ile masaya tutun ve sarsıntı boyunca kal.";
        }
    }

    public PrototypeInteractable CurrentPreparationTarget
    {
        get
        {
            if (CurrentPhase != Phase.Preparation)
                return null;
            for (int i = 0; i < preparationTasks.Count; i++)
            {
                if (preparationTasks[i] != null && !preparationTasks[i].IsComplete)
                    return preparationTasks[i];
            }
            return null;
        }
    }

    public void Initialize(PrototypeBootstrap owner, PrototypePlayerController playerController, Vector3 refugePosition)
    {
        bootstrap = owner;
        player = playerController;
        safeTablePosition = refugePosition;
        CurrentPhase = Phase.Preparation;
    }

    public void AddPreparationTask(PrototypeInteractable task)
    {
        if (task != null && !preparationTasks.Contains(task))
            preparationTasks.Add(task);
    }

    public void RegisterLooseObject(Rigidbody body)
    {
        if (body != null && !looseObjects.Contains(body))
            looseObjects.Add(body);
    }

    public void RegisterFurniture(Rigidbody body, PrototypeInteractable anchorTask)
    {
        if (body != null && anchorTask != null)
            furniture[body] = anchorTask;
    }

    public void TryInteract(PrototypeInteractable target)
    {
        if (CurrentPhase == Phase.Completed || IsShaking)
            return;

        if (target == null)
        {
            PrototypeInteractable active = CurrentPreparationTarget;
            bootstrap.ShowNotice(active != null
                ? "Burada görev hedefi yok. Aktif hedef işaretini izle: " + active.ActionName + "."
                : "Bu noktada etkileşimli bir görev hedefi yok.");
            return;
        }

        if (target.IsComplete)
        {
            bootstrap.ShowNotice("Bu görev tamamlandı. Sıradaki hedefi görev işaretinden bul.");
            return;
        }

        if (target.Action == PrototypeAction.ShutGasValve)
        {
            if (!IsPostQuake)
            {
                bootstrap.ShowNotice("Bu adım deprem sonrasında uygulanır.");
                return;
            }

            target.CompleteAction();
            gasValveClosed = true;
            Score += 100;
            bootstrap.ShowNotice("Gaz vanası kapatıldı. Kapı ve pencereleri aç; elektrik düğmelerine dokunmadan binayı terk et.");
            RefreshHud();
            return;
        }

        if (target.Action == PrototypeAction.OpenWindows)
        {
            if (!IsPostQuake)
            {
                bootstrap.ShowNotice("Pencereyi sarsıntı durduktan sonra aç.");
                return;
            }

            target.CompleteAction();
            windowsOpened = true;
            Score += 100;
            bootstrap.ShowNotice("Pencere açıldı. Elektrik düğmelerine dokunmadan binayı terk et.");
            RefreshHud();
            return;
        }

        if (CurrentPhase != Phase.Preparation)
        {
            bootstrap.ShowNotice("Bu nesne şu anki deprem sonrası görevin hedefi değil.");
            return;
        }

        if (!preparationTasks.Contains(target))
        {
            bootstrap.ShowNotice("Bu nesne hazırlık görevlerinden biri değil. Aktif hedef işaretini izle.");
            return;
        }

        PrototypeInteractable currentTarget = CurrentPreparationTarget;
        if (target != currentTarget)
        {
            bootstrap.ShowNotice(currentTarget != null
                ? "Önce aktif görevi tamamla: " + currentTarget.ActionName + "."
                : "Hazırlık görevleri tamamlandı.");
            return;
        }

        target.CompleteAction();
        switch (target.Action)
        {
            case PrototypeAction.SecureFurniture:
                Score += 100;
                bootstrap.ShowNotice("Rafı sabitledin. Büyük mobilyaları duvara veya zemine uygun biçimde sabitle.");
                break;
            case PrototypeAction.LowerHeavyItem:
                Score += 100;
                bootstrap.ShowNotice("Ağır eşyayı aşağı aldın. Ağır nesneleri alt raflarda tut.");
                break;
            case PrototypeAction.ClearExit:
                Score += 100;
                bootstrap.ShowNotice("Çıkış yolu açıldı. Geçişleri eşya ile kapatma.");
                break;
        }

        RefreshHud();
        if (PreparationComplete())
            bootstrap.ShowNotice("Hazırlık tamamlandı. Enter tuşuna basarak sarsıntı tatbikatını başlat.");
    }

    public bool PreparationComplete()
    {
        if (preparationTasks.Count == 0)
            return false;

        for (int i = 0; i < preparationTasks.Count; i++)
        {
            if (!preparationTasks[i].IsComplete)
                return false;
        }
        return true;
    }

    public void StartEarthquake()
    {
        if (!PreparationComplete() || CurrentPhase != Phase.Preparation)
        {
            bootstrap.ShowNotice("Önce üç hazırlık görevini tamamla.");
            return;
        }

        CurrentPhase = Phase.Shaking;
        quakeTimer = 0f;
        holdTimer = 0f;
        safeActionComplete = false;
        bootstrap.SetHoldZoneVisible(true);
        GameManager.Instance?.StartScenario();

        foreach (KeyValuePair<Rigidbody, PrototypeInteractable> pair in furniture)
        {
            if (pair.Key == null || (pair.Value != null && pair.Value.IsComplete))
                continue;

            pair.Key.isKinematic = false;
            pair.Key.AddTorque(Random.insideUnitSphere * 3.5f, ForceMode.Impulse);
            pair.Key.AddForce(new Vector3(Random.Range(-1.5f, 1.5f), 0.2f, Random.Range(-1f, 1f)), ForceMode.Impulse);
        }

        foreach (Rigidbody body in looseObjects)
        {
            if (body == null)
                continue;
            body.isKinematic = false;
            body.AddForce(new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(0.5f, 1.2f), Random.Range(-1.2f, 1.2f)), ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * 2.5f, ForceMode.Impulse);
        }

        bootstrap.ShowNotice("Deprem başladı. Çıkışa koşma. Işıklı güvenli bölgeye geç ve C ile çök. Bu karakterde masa tutunma animasyonu olmadığından E adımı devre dışı.");
        RefreshHud();
    }

    public void TryStartTableHold()
    {
        if (!IsShaking || player == null || player.IsHoldingTable)
            return;

        if (!player.IsInSafeZone(HoldZonePosition, HoldZoneRadius))
        {
            Vector3 offset = player.transform.position - safeTablePosition;
            offset.y = 0f;
            string message = offset.magnitude < 2.6f
                ? "Masanın diğer tarafına geç. Yerdeki ışıklı halkanın içine gir."
                : "Sağlam masanın önündeki ışıklı halkaya yaklaş.";
            bootstrap.ShowNotice(message);
            return;
        }

        if (!player.IsCrouched)
        {
            bootstrap.ShowNotice("Güvenli bölgedesin. Önce C ile çök; çökme ve tutunma ayrı adımlardır.");
            return;
        }

        if (!player.IsFacing(safeTablePosition, 0.65f))
        {
            bootstrap.ShowNotice("Masaya dön. Üçüncü şahıs kamerayı masaya çevir, sonra E'ye bas.");
            return;
        }

        holdTimer = 0f;
        // Masa üstünün ön kenarını geçip altına girecek kadar hizala; çarpışma
        // kapsülü bu durumda masa tablasının alt boşluğuna sığacak şekilde alçalır.
        Vector3 underTablePosition = safeTablePosition + new Vector3(-0.35f, 0f, -0.15f);
        player.BeginTableHold(underTablePosition, safeTablePosition);
        bootstrap.ShowNotice("Çökme tamamlandı. Masaya tutunuyorsun; sarsıntı boyunca burada kal.");
        RefreshHud();
    }

    public void CancelTableHold(string message)
    {
        if (player == null || !player.IsHoldingTable)
            return;

        player.EndTableHold();
        if (!safeActionComplete)
            holdTimer = 0f;
        bootstrap.ShowNotice(message);
        RefreshHud();
    }


    public void RegisterHoldInput()
    {
        if (!IsShaking || player == null)
            return;

        if (!player.IsHoldingTable || !player.IsCrouched
            || !player.IsInSafeZone(HoldZonePosition, HoldZoneRadius))
        {
            if (!safeActionComplete)
                holdTimer = 0f;

            Debug.Log($"Tutunma başarısız: Holding={player.IsHoldingTable}, Crouched={player.IsCrouched}, SafeZone={player.IsInSafeZone(HoldZonePosition, HoldZoneRadius)}, Timer={holdTimer:F2}");
            return;
        }

        if (!player.IsHoldPoseAligned)
        {
            Debug.Log($"Tutunma hizası başarısız. Timer={holdTimer:F2}");
            return;
        }

        holdTimer = Mathf.Min(RequiredHoldDuration, holdTimer + Time.deltaTime);
        Debug.Log($"Tutunma sayacı: {holdTimer:F2}/{RequiredHoldDuration:F2}");

        if (!safeActionComplete && holdTimer >= RequiredHoldDuration)
        {
            safeActionComplete = true;
            Score += 100;
            bootstrap.ShowNotice("Doğru: güvenli alanda çöktün ve masaya tutunarak pozisyonunu korudun.");
            RefreshHud();
        }
    }


    private void Update()
    {
        if (IsTrapped)
        {
            ReadTrappedAnswer();
            return;
        }

        if (IsShaking)
        {
            quakeTimer += Time.deltaTime;
            player.SetShake(0.014f + Mathf.Sin(quakeTimer * 2.1f) * 0.003f);
            if (quakeTimer >= QuakeDuration)
                EndEarthquake();
            else if (Time.frameCount % 10 == 0)
                RefreshHud();
        }
        else if (IsPostQuake && player != null)
        {
            Vector3 position = player.transform.position;
            if (position.x < -8.4f && position.z > -4.2f && position.z < -0.4f)
            {
                if (!gasValveClosed || !windowsOpened)
                {
                    if (!exitHintShown)
                    {
                        exitHintShown = true;
                        string missing = !gasValveClosed && !windowsOpened
                            ? "gaz vanasını kapatıp pencereyi aç"
                            : (!gasValveClosed ? "gaz vanasını kapat" : "pencereyi aç");
                        bootstrap.ShowNotice("Tatbikatta gaz kokusu var. Önce " + missing + "; elektrik düğmelerine dokunmadan çık.");
                    }
                }
                else if (position.x < -10.0f)
                {
                    FinishScenario();
                }
            }
        }
    }
    private void EndEarthquake()
    {
        player.SetShake(0f);
        player.EndTableHold();
        player.SetCrouched(false);
        bootstrap.SetHoldZoneVisible(false);

        collapseOccurred = Random.value < CollapseChance;
        if (collapseOccurred)
        {
            CurrentPhase = Phase.Blackout;
            bootstrap.SetPlayerControls(false);
            bootstrap.ShowBlackout();
            StartCoroutine(BeginTrappedScenario());
            return;
        }

        CurrentPhase = Phase.Aftershock;
        if (!safeActionComplete)
            bootstrap.ShowNotice("Sarsıntı bitti. Güvenli bölgede çöküp masaya 5 saniye tutunma koşulunu tamamlayamadın. Puanından kesinti yapılmadı.");
        else
            bootstrap.ShowNotice("Sarsıntı durdu. Önce çevreni kontrol et; gaz kokusu varsa vanayı güvenliyse kapat, kapı ve pencereleri aç ve elektrik düğmelerine dokunmadan çık.");

        RefreshHud();
    }

    private IEnumerator BeginTrappedScenario()
    {
        yield return new WaitForSecondsRealtime(BlackoutDuration);
        CurrentPhase = Phase.Trapped;
        trappedQuestionIndex = 0;
        bootstrap.ShowTrappedQuestion(1, TrappedQuestions[0], TrappedChoices[0], string.Empty);
        RefreshHud();
    }

    private void ReadTrappedAnswer()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            SelectTrappedAnswer(0);
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            SelectTrappedAnswer(1);
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            SelectTrappedAnswer(2);
    }

    public void SelectTrappedAnswer(int choiceIndex)
    {
        if (!IsTrapped || choiceIndex < 0 || choiceIndex >= 3)
            return;

        if (choiceIndex != TrappedCorrectChoices[trappedQuestionIndex])
        {
            bootstrap.SetTrappedFeedback("Bu seçim güvenli değil. Bilgiyi tekrar gözden geçir ve doğru adımı seç.");
            return;
        }

        Score += 100;
        trappedQuestionIndex++;
        if (trappedQuestionIndex >= TrappedQuestions.Length)
        {
            FinishScenario();
            return;
        }

        bootstrap.ShowTrappedQuestion(trappedQuestionIndex + 1,
            TrappedQuestions[trappedQuestionIndex], TrappedChoices[trappedQuestionIndex],
            TrappedCorrectFeedback[trappedQuestionIndex - 1] + "  +100 puan.");
        RefreshHud();
    }
    private void FinishScenario()
    {
        if (CurrentPhase == Phase.Completed)
            return;

        CurrentPhase = Phase.Completed;
        if (!collapseOccurred)
            Score += 100;
        ProvinceInfo province = ProvinceManager.Instance != null ? ProvinceManager.Instance.SelectedProvince : null;
        int plateCode = province != null ? province.plateCode : 6;
        string completedKey = ProvinceManager.ProgressKey("TurkiyeAfet.Deprem.Completed", plateCode);
        string lastScoreKey = ProvinceManager.ProgressKey("TurkiyeAfet.Deprem.LastScore", plateCode);
        string bestScoreKey = ProvinceManager.ProgressKey("TurkiyeAfet.Deprem.BestScore", plateCode);
        PlayerPrefs.SetInt(completedKey, 1);
        PlayerPrefs.SetInt(lastScoreKey, Score);
        PlayerPrefs.SetInt(bestScoreKey, Mathf.Max(Score, PlayerPrefs.GetInt(bestScoreKey, 0)));
        PlayerPrefs.SetInt("TurkiyeAfet.LastProvince", plateCode);
        PlayerPrefs.SetInt("TurkiyeAfet.LastCompletedProvince", plateCode);
        if (plateCode == 6)
        {
            // Keep the save keys from the first Ankara-only prototype readable.
            PlayerPrefs.SetInt("TurkiyeAfet.Deprem.Completed", 1);
            PlayerPrefs.SetInt("TurkiyeAfet.Deprem.LastScore", Score);
            PlayerPrefs.SetInt("TurkiyeAfet.Deprem.BestScore", Mathf.Max(Score, PlayerPrefs.GetInt("TurkiyeAfet.Deprem.BestScore", 0)));
        }
        PlayerPrefs.Save();

        GameManager.Instance?.ShowResults();
        string review = (province != null ? province.name + "  •  PLAKA " + province.PlateLabel + ". " : "") +
            "Hazırlık: " + preparationTasks.FindAll(t => t.IsComplete).Count + "/3 görev. " +
            "Deprem anı: " + (safeActionComplete ? "5 saniye çök-kapan-tutun tamamlandı. " : "5 saniyelik güvenli tutunma tamamlanmadı. ");
        if (collapseOccurred)
        {
            review += "Enkaz altında: " + trappedQuestionIndex + "/3 doğru güvenlik adımı. " +
                "Sakin kal, tozu azalt, tehlikeli hareketlerden kaçın, enerjini koru ve kurtarma ekiplerine kontrollü biçimde ses ver.";
        }
        else
        {
            review += "Deprem sonrası: " + (gasValveClosed ? "gaz vanası kapatıldı, " : "gaz vanası kapatılmadı; ") +
                (windowsOpened ? "pencere açıldı, " : "pencere açılmadı; ") +
                "toplanma alanına ulaşıldı. Hasarlı binaya geri dönme; resmî uyarıları izle.";
        }
        bootstrap.ShowResults(Score, review);
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (bootstrap == null)
            return;

        string tasks = "";
        for (int i = 0; i < preparationTasks.Count; i++)
        {
            PrototypeInteractable task = preparationTasks[i];
            bool active = task == CurrentPreparationTarget;
            task.SetGuidance(active);
            if (active)
                tasks += "<color=#5ED7C4>▶ AKTİF HEDEF: " + task.ActionName + "</color>\n";
            tasks += (task.IsComplete ? "✓ " : "○ ") + task.ActionName;
            if (i < preparationTasks.Count - 1)
                tasks += "\n";
        }
        string shelter = safeActionComplete ? "✓ Güvenli bölgede çök-kapan-tutun" : "○ Güvenli bölgede çök-kapan-tutun";
        string valve = gasValveClosed ? "✓ Gaz vanasını kapat" : "○ Gaz kokusu: güvenliyse vanayı kapat";
        string windows = windowsOpened ? "✓ Pencereyi aç" : "○ Kapı ve pencereleri aç";
        string exit = CurrentPhase == Phase.Completed ? "✓ Toplanma alanına ilerle" : "○ Binadan çık, toplanma alanına ilerle";
        string phaseText = CurrentPhase switch
        {
            Phase.Preparation => "DEPREM ÖNCESİ HAZIRLIK",
            Phase.Shaking => "DEPREM ANI  •  " + Mathf.CeilToInt(Mathf.Max(0f, QuakeDuration - quakeTimer)) + " sn",
            Phase.Blackout => "ENKAZ OLUŞTU",
            Phase.Trapped => "ENKAZ ALTINDA  •  ADIM " + Mathf.Min(3, trappedQuestionIndex + 1) + "/3",
            Phase.Aftershock => "DEPREM SONRASI",
            _ => "TATBİKAT TAMAMLANDI"
        };
        bootstrap.RefreshHud(phaseText, tasks, shelter, valve, windows, exit, Score, HoldProgress);
    }
}



