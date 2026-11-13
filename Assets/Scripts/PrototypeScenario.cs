using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PrototypeScenario : MonoBehaviour
{
    public enum Phase { Preparation, Shaking, Flooding, Blackout, Trapped, Aftershock, Completed }

    private const float QuakeDuration = 10f;
    private const float RequiredHoldDuration = 5f;
    private const float HoldZoneRadius = 0.72f;
    private const float CollapseChance = 0.5f;
    private const float BlackoutDuration = 2.5f;
    private const int RequiredBagItemCount = 6;
    private const float PreparationCountdownDuration = 5f;
    private const int TrappedQuestionPoints = 50;
    private const float FloodRiseDuration = 18f;

    private static readonly string[] TrappedQuestions =
    {
        "Toz yoğunlaşıyor ve yanında ince bir kumaş parçası var. En güvenli ilk tepki hangisi?",
        "Bacağında ağrı ve hareket kısıtlılığı fark ettin. Kendini kurtarmak için ne yapmalısın?",
        "Yakınlarda arama kurtarma sesleri duydun. Yerini bildirirken nasıl davranırsın?",
        "Elin yakındaki bir boruya erişiyor; çevredeki parçalar gevşek görünüyor. En güvenli sinyal yöntemi hangisi?",
        "Telefonun yanında ama pil seviyesi düşük. En uygun kullanım biçimi hangisi?",
        "Üzerindeki küçük bir parçayı oynatınca üst taraftan toz ve ufak parçalar dökülüyor. Ne yapmalısın?"
    };

    private static readonly string[][] TrappedChoices =
    {
        new[] { "Ağzımı ve burnumu kumaşla kapatır, tozu artıracak gereksiz hareketten kaçınırım.", "Kumaşı ıslatmak için enkaz içinde su aramaya başlarım.", "Tozun dağılması için bulunduğum yerde hızla hareket ederim." },
        new[] { "Bacağımı birkaç kez oynatıp ne kadar hasarlı olduğunu sınarım.", "Ağrıyan bölgeyi mümkün olduğunca sabit tutar, tehlikeli kurtulma hareketlerinden kaçınırım.", "Üzerimdeki parçaları bütün gücümle iterek alan açarım." },
        new[] { "Sürekli bağırırım; böylece sesim kesilmez.", "Ekiplerin yaklaşmasını beklemeden yer değiştirmeye çalışırım.", "Sesleri dinler, uygun aralıklarla seslenip enerjimi korurum." },
        new[] { "Boruyu aralıksız ve olabildiğince sert döverim.", "Güvenli biçimde erişebiliyorsam aralıklı vurur, sonra durup ekipleri dinlerim.", "Daha iyi duyulmak için boruyu çekip yerinden oynatırım." },
        new[] { "Yalnızca acil yardım/konum bildirmek için kullanır, ekranı ve pili gereksiz tüketmem.", "Sürekli fenerini ve sesini açarak çevreyi araştırırım.", "Şebeke gelene kadar art arda arama yapıp telefonu açık tutarım." },
        new[] { "Parçayı çekip alanı genişletmeye devam ederim.", "Düşen tozu temizlemek için çevremi hızla silkerim.", "Hareketi durdurur, parçayı zorlamaz ve yeniden sesleri dinlerim." }
    };

    private static readonly int[] TrappedCorrectChoices = { 0, 1, 2, 1, 0, 2 };
    private static readonly string[] TrappedCorrectFeedback =
    {
        "Doğru. Kumaş toz solumayı azaltmaya yardımcı olur; gereksiz hareket tozu artırabilir.",
        "Doğru. Ağrıyan bölgeyi zorlamamak ve riskli kurtulma hareketlerinden kaçınmak daha güvenlidir.",
        "Doğru. Ekipleri dinlemek ve aralıklı sinyal vermek enerjiyi korumaya yardımcı olur.",
        "Doğru. Aralıklı vuruşlar ve ardından dinlemek hem sinyal verir hem enerjiyi korur.",
        "Doğru. Telefonu acil iletişim için saklamak pilin korunmasına yardımcı olur.",
        "Doğru. Gevşek parçaları zorlamak yeni düşme ve toz riskini artırabilir."
    };

    private readonly List<PrototypeInteractable> preparationTasks = new List<PrototypeInteractable>();
    private readonly List<PrototypeInteractable> floodPreparationTasks = new List<PrototypeInteractable>();
    private readonly List<PrototypeInteractable> floodAfterTasks = new List<PrototypeInteractable>();
    private readonly List<Rigidbody> looseObjects = new List<Rigidbody>();
    private readonly Dictionary<Rigidbody, PrototypeInteractable> furniture = new Dictionary<Rigidbody, PrototypeInteractable>();

    private PrototypeBootstrap bootstrap;
    private PrototypePlayerController player;
    private PrototypeInteractable disasterBag;
    private PrototypeInteractable heldBagItem;
    private Vector3 safeTablePosition;
    private float quakeTimer;
    private float holdTimer;
    private float preparationCountdownRemaining;
    private int baggedItemCount;
    private bool disasterBagOpen;
    private bool bagTaskSkipped;
    private bool preparationCountdownStarted;
    private bool safeActionComplete;
    private bool gasValveClosed;
    private bool windowsOpened;
    private bool exitDoorOpened;
    private bool exitHintShown;
    private bool collapseOccurred;
    private int trappedQuestionIndex;
    private bool floodScenario;
    private bool floodEvacuated;
    private Transform floodWater;
    private float floodTimer;
    private float floodWaterStartY;
    private float floodWaterEndY = 1.65f;
    private Vector3 floodSafeCenter = new Vector3(-7f, 2.8f, -2f);
    private int floodPreparationIndex;
    private int floodAfterIndex;

    public Phase CurrentPhase { get; private set; } = Phase.Preparation;
    public bool IsShaking => CurrentPhase == Phase.Shaking;
    public bool IsFloodScenario => floodScenario;
    public bool IsFlooding => floodScenario && CurrentPhase == Phase.Flooding;
    public bool IsFloodEvacuated => floodEvacuated;
    public Vector3 FloodSafePosition => floodSafeCenter;
    public bool IsTrapped => CurrentPhase == Phase.Trapped;
    public int TrappedQuestionCount => TrappedQuestions.Length;
    public bool IsPostQuake => CurrentPhase == Phase.Aftershock;
    public bool IsDisasterBagOpen => disasterBagOpen;
    public bool HasItemInHand => heldBagItem != null;
    public int BaggedItemCount => baggedItemCount;
    public bool IsBagTaskSkipped => bagTaskSkipped;
    public float PreparationCountdownRemaining => preparationCountdownStarted
        ? Mathf.Max(0f, preparationCountdownRemaining) : 0f;
    public int Score { get; private set; }
    public float HoldProgress => Mathf.Clamp01(holdTimer / RequiredHoldDuration);
    public Vector3 HoldZonePosition => safeTablePosition + new Vector3(-0.35f, 0f, -0.15f);
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
            return "Yeşil alanın içindesin. E veya sol tıkla tutun; merkeze yerleşeceksin.";
        }
    }

    public PrototypeInteractable CurrentPreparationTarget
    {
        get
        {
            if (floodScenario)
            {
                if (CurrentPhase == Phase.Preparation)
                    return floodPreparationIndex < floodPreparationTasks.Count
                        ? floodPreparationTasks[floodPreparationIndex] : null;
                if (CurrentPhase == Phase.Aftershock)
                    return floodAfterIndex < floodAfterTasks.Count
                        ? floodAfterTasks[floodAfterIndex] : null;
                return null;
            }

            if (CurrentPhase != Phase.Preparation)
                return null;
            for (int i = 0; i < preparationTasks.Count; i++)
            {
                if (preparationTasks[i] != null && !preparationTasks[i].IsComplete
                    && !(preparationTasks[i].Action == PrototypeAction.PrepareDisasterBag && bagTaskSkipped))
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

    public void ConfigureFlood(Transform water, Vector3 safeCenter)
    {
        floodScenario = true;
        floodWater = water;
        floodWaterStartY = water != null ? water.position.y : 0f;
        floodSafeCenter = safeCenter;
        CurrentPhase = Phase.Preparation;
    }

    public void AddFloodPreparationTask(PrototypeInteractable task)
    {
        if (task != null && !floodPreparationTasks.Contains(task))
            floodPreparationTasks.Add(task);
    }

    public void AddFloodAfterTask(PrototypeInteractable task)
    {
        if (task != null && !floodAfterTasks.Contains(task))
            floodAfterTasks.Add(task);
    }

    public void AddPreparationTask(PrototypeInteractable task)
    {
        if (task != null && !preparationTasks.Contains(task))
            preparationTasks.Add(task);
    }

    public void SetDisasterBag(PrototypeInteractable bag)
    {
        disasterBag = bag;
    }

    public void RegisterLooseObject(Rigidbody body)
    {
        if (body == null)
            return;

        if (!looseObjects.Contains(body))
            looseObjects.Add(body);
        if (body.GetComponent<PrototypeImpactAudio>() == null)
            body.gameObject.AddComponent<PrototypeImpactAudio>();
    }

    public void RegisterFurniture(Rigidbody body, PrototypeInteractable anchorTask)
    {
        if (body != null && anchorTask != null)
        {
            furniture[body] = anchorTask;
            if (body.GetComponent<PrototypeImpactAudio>() == null)
                body.gameObject.AddComponent<PrototypeImpactAudio>();
        }
    }

    public void TryInteract(PrototypeInteractable target)
    {
        if (CurrentPhase == Phase.Completed || IsShaking)
            return;

        if (floodScenario)
        {
            TryInteractFlood(target);
            return;
        }

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

        if (target.Action == PrototypeAction.PrepareDisasterBag)
        {
            InteractWithDisasterBag(target);
            return;
        }

        if (target.Action == PrototypeAction.CollectBagItem)
        {
            InteractWithBagItem(target);
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
            PrototypeAudio.PlayGasValve();
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
            PrototypeAudio.PlayWindowSlide();
            Score += 100;
            bootstrap.ShowNotice("Pencere açıldı. Elektrik düğmelerine dokunmadan binayı terk et.");
            RefreshHud();
            return;
        }

        if (target.Action == PrototypeAction.OpenExitDoor)
        {
            if (!IsPostQuake)
            {
                bootstrap.ShowNotice("Kapı deprem öncesinde ve sarsıntı sırasında açılmaz. Deprem bittikten sonra yeniden dene.");
                return;
            }

            target.OpenExitDoor();
            exitDoorOpened = true;
            PrototypeAudio.PlayDoorOpen();
            bootstrap.ShowNotice("Kapı açıldı. Gaz ve pencere görevlerini tamamlayıp toplanma alanına ilerle.");
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
        PrototypeAudio.PlayFurnitureSound(target.Action);
        PrototypeAudio.PlayTaskComplete();
        switch (target.Action)
        {
            case PrototypeAction.SecureFurniture:
                Score += 100;
                bootstrap.ShowNotice("Rafı sabitledin. Büyük mobilyaları duvara veya zemine uygun biçimde sabitle. +100 puan.");
                break;
            case PrototypeAction.LowerHeavyItem:
                Score += 100;
                bootstrap.ShowNotice("Ağır eşyayı aşağı aldın. Ağır nesneleri alt raflarda tut. +100 puan.");
                break;
            case PrototypeAction.ClearExit:
                Score += 100;
                bootstrap.ShowNotice("Çıkış yolu açıldı. Geçişleri eşya ile kapatma. +100 puan.");
                break;
        }

        RefreshHud();
        if (PreparationComplete())
            StartPreparationCountdown();
    }

    private void TryInteractFlood(PrototypeInteractable target)
    {
        if (target == null)
        {
            bootstrap.ShowNotice(CurrentPhase == Phase.Flooding
                ? "Su yükseliyor. Evden çıkmaya çalışma; üst kata, güvenli alana çık."
                : "Aktif sel görevi hedefini bul ve E veya sol tıkla etkileş.");
            return;
        }

        PrototypeInteractable expected = CurrentPreparationTarget;
        if (target != expected)
        {
            bootstrap.ShowNotice(expected != null
                ? "Önce aktif görevi tamamla: " + expected.ActionName + "."
                : CurrentPhase == Phase.Flooding
                    ? "Su yükseliyor. Eşyalarla oyalanma; üst kata çık."
                    : "Bu hedef şu an aktif değil.");
            return;
        }

        target.CompleteAction();
        if (CurrentPhase == Phase.Preparation)
        {
            Score += 75;
            floodPreparationIndex++;
            bootstrap.ShowNotice("Sel hazırlık adımı tamamlandı. +75 puan.");
            if (floodPreparationIndex >= floodPreparationTasks.Count)
                StartPreparationCountdown();
        }
        else if (CurrentPhase == Phase.Aftershock)
        {
            Score += 75;
            floodAfterIndex++;
            bootstrap.ShowNotice("Sel sonrası adım tamamlandı. +75 puan.");
            if (floodAfterIndex >= floodAfterTasks.Count)
                FinishScenario();
        }
        RefreshHud();
    }

    private void InteractWithDisasterBag(PrototypeInteractable target)
    {
        if (CurrentPhase != Phase.Preparation || CurrentPreparationTarget != target)
        {
            PrototypeInteractable active = CurrentPreparationTarget;
            bootstrap.ShowNotice(active != null
                ? "Önce aktif görevi tamamla: " + active.ActionName
                : "Hazırlık görevleri tamamlandı.");
            return;
        }

        if (!disasterBagOpen)
        {
            disasterBagOpen = true;
            target.SetBagOpenVisual(true);
            PrototypeAudio.PlayBagOpen();
            bootstrap.ShowNotice("Çantayı açtın. Evin içindeki 6 eşyayı bulup teker teker çantaya koy.");
            RefreshHud();
            return;
        }

        if (heldBagItem == null)
        {
            if (!CorePreparationTasksComplete())
            {
                bootstrap.ShowNotice("Önce diğer hazırlık görevlerini tamamla.");
                return;
            }

            disasterBagOpen = false;
            target.SetBagOpenVisual(false);
            PrototypeAudio.PlayBagClose();
            bagTaskSkipped = true;
            bootstrap.ShowNotice("Çanta bonus görevini atladın. Deprem 5 saniye içinde başlayacak.");
            RefreshHud();
            StartPreparationCountdown();
            return;
        }

        Vector3[] bagSlots =
        {
            new Vector3(-0.23f, 0.37f, -0.10f), new Vector3(0f, 0.37f, -0.10f), new Vector3(0.23f, 0.37f, -0.10f),
            new Vector3(-0.23f, 0.37f, 0.10f), new Vector3(0f, 0.37f, 0.10f), new Vector3(0.23f, 0.37f, 0.10f)
        };
        player.ReleaseBagItem(heldBagItem);
        heldBagItem.PackBagItem(disasterBag.BagContentsRoot, bagSlots[baggedItemCount], 0.14f);
        PrototypeAudio.PlayBagStore();
        heldBagItem = null;
        baggedItemCount++;

        if (baggedItemCount >= RequiredBagItemCount)
        {
            target.SetBagOpenVisual(false);
            target.CompleteAction();
            PrototypeAudio.PlayBagClose();
            PrototypeAudio.PlayTaskComplete();
            Score += 75;
            bootstrap.ShowNotice("Çanta hazırlandı. +75 ek puan.");
        }
        else
        {
            bootstrap.ShowNotice("Eşya çantaya kondu. " + baggedItemCount + "/" + RequiredBagItemCount);
        }

        RefreshHud();
        if (PreparationComplete())
            StartPreparationCountdown();
    }

    private void InteractWithBagItem(PrototypeInteractable item)
    {
        if (CurrentPhase != Phase.Preparation || CurrentPreparationTarget != disasterBag)
        {
            PrototypeInteractable active = CurrentPreparationTarget;
            bootstrap.ShowNotice(active != null
                ? "Önce aktif hazırlık görevini tamamla: " + active.ActionName
                : "Hazırlık görevleri tamamlandı.");
            return;
        }

        if (!disasterBagOpen)
        {
            bootstrap.ShowNotice("Önce koltuğun üzerindeki afet çantasını aç.");
            return;
        }

        if (heldBagItem != null)
        {
            bootstrap.ShowNotice("Önce elindekini çantaya koy.");
            return;
        }

        heldBagItem = item;
        player.HoldBagItem(item);
        PrototypeAudio.PlayItemPickup();
        bootstrap.ShowNotice("Eşyayı aldın. Çantaya dönüp E veya sol tıkla içine koy.");
        RefreshHud();
    }

    private void StartPreparationCountdown()
    {
        if (preparationCountdownStarted || !PreparationComplete())
            return;

        preparationCountdownStarted = true;
        preparationCountdownRemaining = PreparationCountdownDuration;
        bootstrap.ShowNotice(floodScenario
            ? "Hazırlık tamamlandı. Taşkın suyu yaklaşık 5 saniye içinde yükselmeye başlayacak."
            : "Hazırlık tamamlandı. Deprem yaklaşık 5 saniye içinde başlayacak.");
        RefreshHud();
    }

    private bool CorePreparationTasksComplete()
    {
        if (preparationTasks.Count == 0)
            return false;

        for (int i = 0; i < preparationTasks.Count; i++)
        {
            PrototypeInteractable task = preparationTasks[i];
            if (task != null && task.Action != PrototypeAction.PrepareDisasterBag && !task.IsComplete)
                return false;
        }
        return true;
    }

    public bool PreparationComplete()
    {
        if (floodScenario)
            return floodPreparationTasks.Count > 0 && floodPreparationIndex >= floodPreparationTasks.Count;
        return CorePreparationTasksComplete()
            && (bagTaskSkipped || (disasterBag != null && disasterBag.IsComplete));
    }

    public void StartEarthquake()
    {
        if (!PreparationComplete() || CurrentPhase != Phase.Preparation)
        {
            bootstrap.ShowNotice("Önce tüm hazırlık görevlerini tamamla.");
            return;
        }

        if (floodScenario)
        {
            StartFlooding();
            return;
        }

        CurrentPhase = Phase.Shaking;
        preparationCountdownStarted = false;
        preparationCountdownRemaining = 0f;
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

        bootstrap.ShowNotice("Deprem başladı. Çıkışa koşma. Yeşil güvenli alanın içine girip E veya sol tıkla tutun; merkeze yerleşeceksin.");
        RefreshHud();
    }

    private void StartFlooding()
    {
        CurrentPhase = Phase.Flooding;
        preparationCountdownStarted = false;
        preparationCountdownRemaining = 0f;
        floodTimer = 0f;
        if (floodWater != null)
            floodWater.gameObject.SetActive(true);
        GameManager.Instance?.StartScenario();
        bootstrap.ShowNotice("Taşkın başladı. Evin arka-sağ köşesindeki yeşil çizgiyi merdivene kadar izle; basamaklardan üst kata çıkıp yeşil güvenli alanda dur.");
        RefreshHud();
    }

    public void TryStartTableHold()
    {
        if (!IsShaking || player == null || player.IsHoldingTable)
            return;

        if (!player.IsInSafeZone(HoldZonePosition, HoldZoneRadius))
        {
            Vector3 offset = player.transform.position - HoldZonePosition;
            offset.y = 0f;
            string message = offset.magnitude < 2.6f
                ? "Yeşil güvenli alanın içine gir, sonra E veya sol tıkla tutun."
                : "Sağlam masanın önündeki yeşil güvenli alana yaklaş.";
            bootstrap.ShowNotice(message);
            return;
        }

        holdTimer = 0f;
        player.BeginTableHold(HoldZonePosition, safeTablePosition);
        PrototypeAudio.PlayHoldStart();
        bootstrap.ShowNotice("Güvenli alana geçtin ve masaya tutunuyorsun; sarsıntı boyunca burada kal.");
        RefreshHud();
    }
    public void CancelTableHold(string message)
    {
        if (player == null || !player.IsHoldingTable)
            return;

        player.EndTableHold();
        PrototypeAudio.PlayCrouch();
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
            PrototypeAudio.PlayTaskComplete();
            bootstrap.ShowNotice("Doğru: güvenli alanda çöktün ve masaya tutunarak pozisyonunu korudun.");
            RefreshHud();
        }
    }


    private void Update()
    {
        if (CurrentPhase == Phase.Preparation && preparationCountdownStarted)
        {
            preparationCountdownRemaining = Mathf.Max(0f, preparationCountdownRemaining - Time.deltaTime);
            if (preparationCountdownRemaining <= 0f)
            {
                StartEarthquake();
                return;
            }
            if (Time.frameCount % 10 == 0)
                RefreshHud();
            return;
        }

        if (IsTrapped)
        {
            ReadTrappedAnswer();
            return;
        }

        if (floodScenario)
        {
            UpdateFlood();
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
                else if (!exitDoorOpened)
                {
                    if (!exitHintShown)
                    {
                        exitHintShown = true;
                        bootstrap.ShowNotice("Kapı kapalı. Giriş kapısına dönüp E veya sol tıkla aç.");
                    }
                }
                else if (position.x < -10.0f)
                {
                    FinishScenario();
                }
            }
        }
    }

    private void UpdateFlood()
    {
        if (CurrentPhase != Phase.Flooding && CurrentPhase != Phase.Aftershock)
            return;

        floodTimer += Time.deltaTime;
        if (floodWater != null)
        {
            float t = Mathf.Clamp01(floodTimer / FloodRiseDuration);
            float y = Mathf.Lerp(floodWaterStartY, floodWaterEndY, t);
            Vector3 position = floodWater.position;
            position.y = y;
            floodWater.position = position;
        }

        if (!floodEvacuated && player != null && IsInFloodSafeZone(player.transform.position))
        {
            floodEvacuated = true;
            Score += 100;
            if (CurrentPhase == Phase.Flooding)
                CurrentPhase = Phase.Aftershock;
            bootstrap.ShowNotice("Yüksek ve güvenli alana çıktın. Su çekilene kadar burada kal. +100 puan.");
            RefreshHud();
        }

        if (CurrentPhase == Phase.Flooding && floodTimer >= FloodRiseDuration)
        {
            CurrentPhase = Phase.Aftershock;
            bootstrap.ShowNotice(floodEvacuated
                ? "Su seviyesi sabitlendi. Güvenli alanda kal ve resmî uyarıları izle."
                : "Su yükseldi. Akıntıya girme; güvenli üst kata çıkmak için merdivenleri kullan.");
            RefreshHud();
        }
        else if (Time.frameCount % 10 == 0)
            RefreshHud();
    }

    private bool IsInFloodSafeZone(Vector3 position)
    {
        Vector3 offset = position - floodSafeCenter;
        return position.y >= floodSafeCenter.y - 0.2f
            && Mathf.Abs(offset.x) <= 1.15f
            && Mathf.Abs(offset.z) <= 1.15f;
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
            PrototypeAudio.PlayCollapse();
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

        if (choiceIndex >= TrappedChoices[trappedQuestionIndex].Length)
            return;

        if (choiceIndex != TrappedCorrectChoices[trappedQuestionIndex])
        {
            PrototypeAudio.PlayAnswerWrong();
            bootstrap.SetTrappedFeedback("Bu seçim güvenli değil. Bilgiyi tekrar gözden geçir ve doğru adımı seç.");
            return;
        }

        PrototypeAudio.PlayAnswerCorrect();
        Score += TrappedQuestionPoints;
        trappedQuestionIndex++;
        if (trappedQuestionIndex >= TrappedQuestions.Length)
        {
            FinishScenario();
            return;
        }

        bootstrap.ShowTrappedQuestion(trappedQuestionIndex + 1,
            TrappedQuestions[trappedQuestionIndex], TrappedChoices[trappedQuestionIndex],
            TrappedCorrectFeedback[trappedQuestionIndex - 1] + "  +" + TrappedQuestionPoints + " puan.");
        RefreshHud();
    }
    private void FinishScenario()
    {
        if (CurrentPhase == Phase.Completed)
            return;

        CurrentPhase = Phase.Completed;
        PrototypeAudio.PlayTaskComplete();
        if (!collapseOccurred)
            Score += 100;
        ProvinceInfo province = ProvinceManager.Instance != null ? ProvinceManager.Instance.SelectedProvince : null;
        int plateCode = province != null ? province.plateCode : 6;
        string disasterKey = floodScenario ? "TurkiyeAfet.Sel" : "TurkiyeAfet.Deprem";
        string completedKey = ProvinceManager.ProgressKey(disasterKey + ".Completed", plateCode);
        string lastScoreKey = ProvinceManager.ProgressKey(disasterKey + ".LastScore", plateCode);
        string bestScoreKey = ProvinceManager.ProgressKey(disasterKey + ".BestScore", plateCode);
        PlayerPrefs.SetInt(completedKey, 1);
        PlayerPrefs.SetInt(disasterKey + ".Completed", 1);
        PlayerPrefs.SetInt(lastScoreKey, Score);
        PlayerPrefs.SetInt(bestScoreKey, Mathf.Max(Score, PlayerPrefs.GetInt(bestScoreKey, 0)));
        PlayerPrefs.SetInt("TurkiyeAfet.LastProvince", plateCode);
        PlayerPrefs.SetInt("TurkiyeAfet.LastCompletedProvince", plateCode);
        if (!floodScenario && plateCode == 6)
        {
            // Keep the save keys from the first Ankara-only prototype readable.
            PlayerPrefs.SetInt("TurkiyeAfet.Deprem.LastScore", Score);
            PlayerPrefs.SetInt("TurkiyeAfet.Deprem.BestScore", Mathf.Max(Score, PlayerPrefs.GetInt("TurkiyeAfet.Deprem.BestScore", 0)));
        }
        PlayerPrefs.Save();

        GameManager.Instance?.ShowResults();
        string review;
        if (floodScenario)
        {
            review = (province != null ? province.name + "  •  PLAKA " + province.PlateLabel + ". " : "") +
                "Sel öncesi: " + floodPreparationTasks.Count + " hazırlık adımı. " +
                (floodEvacuated ? "Taşkın sırasında üst kattaki güvenli alana çıktın. " : "Yüksek alana zamanında ulaşamadın. ") +
                "Sel sonrası: resmî uyarıları takip et, su basmış alanlara ve elektrik hatlarına yaklaşma.";
        }
        else
        {
            review = (province != null ? province.name + "  •  PLAKA " + province.PlateLabel + ". " : "") +
                "Hazırlık: " + preparationTasks.FindAll(t => t.IsComplete).Count + "/4 görev. " +
                (bagTaskSkipped ? "Afet çantası bonusu atlandı. " : "") +
                "Deprem anı: " + (safeActionComplete ? "5 saniye çök-kapan-tutun tamamlandı. " : "5 saniyelik güvenli tutunma tamamlanmadı. ");
            if (collapseOccurred)
            {
                review += "Enkaz altında: " + trappedQuestionIndex + "/" + TrappedQuestions.Length + " doğru güvenlik adımı. " +
                    "Sakin kal, tozu azalt, tehlikeli hareketlerden kaçın, enerjini koru ve kurtarma ekiplerine kontrollü biçimde ses ver.";
            }
            else
            {
                review += "Deprem sonrası: " + (gasValveClosed ? "gaz vanası kapatıldı, " : "gaz vanası kapatılmadı; ") +
                    (windowsOpened ? "pencere açıldı, " : "pencere açılmadı; ") +
                    "toplanma alanına ulaşıldı. Hasarlı binaya geri dönme; resmî uyarıları izle.";
            }
        }
        bootstrap.ShowResults(Score, review);
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (bootstrap == null)
            return;

        if (floodScenario)
        {
            string floodTasks = "";
            if (CurrentPhase == Phase.Preparation)
            {
                floodTasks = "SEL ÖNCESİ HAZIRLIK\nAktif nesneye yaklaş; E veya sol tıkla etkileş.\n";
                AppendFloodTasks(ref floodTasks, floodPreparationTasks, floodPreparationIndex);
            }
            else if (CurrentPhase == Phase.Flooding)
                floodTasks = "SEL ANI\n▶ AKTİF HEDEF: Evin arka-sağ köşesindeki yeşil çizgiyi merdivene kadar izle.\nBasamaklardan üst kata çık ve yeşil güvenli alanda dur.";
            else if (CurrentPhase == Phase.Aftershock)
            {
                floodTasks = floodEvacuated ? "SEL SONRASI\n" : "SEL SONRASI  •  ÖNCE GÜVENLİ ALANA ÇIK\n";
                if (!floodEvacuated)
                    floodTasks += "Arka-sağdaki merdivenden çık; üst kattaki yeşil alana geç.\n";
                AppendFloodTasks(ref floodTasks, floodAfterTasks, floodAfterIndex);
            }

            string phase = CurrentPhase switch
            {
                Phase.Preparation => "SEL / TAŞKIN ÖNCESİ HAZIRLIK",
                Phase.Flooding => "SEL ANI  •  SU YÜKSELİYOR",
                Phase.Aftershock => "SEL SONRASI",
                _ => "TATBİKAT TAMAMLANDI"
            };
            bootstrap.RefreshHud(phase, floodTasks,
                floodEvacuated ? "✓ Yüksek ve güvenli alanda kal" : "○ Üst kattaki güvenli alana çık",
                "", "", CurrentPhase == Phase.Completed ? "✓ Resmî uyarıları izle" : "○ Su çekilene kadar geri dönme",
                Score, 0f);
            return;
        }

        string tasks = "";
        for (int i = 0; i < preparationTasks.Count; i++)
        {
            PrototypeInteractable task = preparationTasks[i];
            bool active = task == CurrentPreparationTarget;
            task.SetGuidance(active);
            bool skipped = task.Action == PrototypeAction.PrepareDisasterBag && bagTaskSkipped;
            if (active)
                tasks += "<color=#5ED7C4>▶ AKTİF HEDEF: " + task.ActionName + "</color>\n";
            tasks += (task.IsComplete ? "✓ " : skipped ? "↷ " : "○ ") + task.ActionName;
            if (skipped)
                tasks += "  (atlandı)";
            if (task.Action == PrototypeAction.PrepareDisasterBag)
                tasks += "  " + baggedItemCount + "/" + RequiredBagItemCount;
            if (i < preparationTasks.Count - 1)
                tasks += "\n";
        }
        string shelter = safeActionComplete ? "✓ Güvenli bölgede çök-kapan-tutun" : "○ Güvenli bölgede çök-kapan-tutun";
        string valve = gasValveClosed ? "✓ Gaz vanasını kapat" : "○ Gaz kokusu: güvenliyse vanayı kapat";
        string windows = windowsOpened ? "✓ Pencereyi aç" : "○ Kapı ve pencereleri aç";
        string exit = CurrentPhase == Phase.Completed
            ? "✓ Kapıdan çıkıp toplanma alanına ulaştın"
            : (exitDoorOpened ? "✓ Kapıyı aç, toplanma alanına ilerle" : "○ Deprem bitince kapıyı aç, toplanma alanına ilerle");
        string phaseText = CurrentPhase switch
        {
            Phase.Preparation => "DEPREM ÖNCESİ HAZIRLIK",
            Phase.Shaking => "DEPREM ANI  •  " + Mathf.CeilToInt(Mathf.Max(0f, QuakeDuration - quakeTimer)) + " sn",
            Phase.Blackout => "ENKAZ OLUŞTU",
            Phase.Trapped => "ENKAZ ALTINDA  •  ADIM " + Mathf.Min(TrappedQuestions.Length, trappedQuestionIndex + 1) + "/" + TrappedQuestions.Length,
            Phase.Aftershock => "DEPREM SONRASI",
            _ => "TATBİKAT TAMAMLANDI"
        };
        bootstrap.RefreshHud(phaseText, tasks, shelter, valve, windows, exit, Score, HoldProgress);
    }

    private void AppendFloodTasks(ref string text, List<PrototypeInteractable> tasks, int activeIndex)
    {
        for (int i = 0; i < tasks.Count; i++)
        {
            PrototypeInteractable task = tasks[i];
            if (task == null)
                continue;
            if (i == activeIndex)
                text += "<color=#5ED7C4>▶ " + task.TargetDisplayName + "</color>\n   " + task.ActionName;
            else
                text += (task.IsComplete ? "✓ " : "○ ") + task.ActionName;
            if (i < tasks.Count - 1)
                text += "\n";
        }
    }
}



