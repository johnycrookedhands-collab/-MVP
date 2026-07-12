using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class PhoneCallSchedule
{
    public PhoneCallData callData;
    public float firstRingDelay = 3f;
    public float repeatDelay = 0f;
    public bool repeat;

    [HideInInspector] public float nextRingTime;
    [HideInInspector] public float remainingDelay;
    [HideInInspector] public bool completed;
}

public class PhoneInteractable : MonoBehaviour
{
    private static readonly List<PhoneInteractable> activePhones = new List<PhoneInteractable>();
    private static int answeredCalls;
    private static int totalRequiredCalls;
    private static int preparedDay;
    private static bool canReturnHome;
    private static GameObject returnHomePrompt;
    private static PhoneInteractable partnerIntroPhone;
    private static bool partnerIntroCompleted;
    private static bool partnerIntroRang;

    [SerializeField] private PhoneCallUI phoneCallUI;
    [SerializeField] private PhoneCallData callData;

    [Header("Partner intro call")]
    [SerializeField] private bool useAsPartnerIntroPhone;
    [SerializeField] private string partnerIntroPhoneObjectName = "Phone";
    [SerializeField] private PhoneCallData partnerIntroCallData;
    [SerializeField] private string partnerIntroResourcesPath = "PhoneCalls/PartnerIntroCall/PartnerIntroCall";
    [SerializeField] private float partnerIntroRingDelay = 3f;

    [Header("Call schedule")]
    [SerializeField] private PhoneCallSchedule[] scheduledCalls;
    [SerializeField] private bool useRandomDialogsFromResources = true;
    [SerializeField] private string randomDialogsResourcesFolder = "PhoneCalls";
    [SerializeField] private bool useCurrentDaySubfolders = true;

    [Header("Phone feedback")]
    [SerializeField] private string pickupPrompt = "E - чтобы поднять трубку";
    [SerializeField] private float ringPulseSpeed = 7f;
    [SerializeField] private float ringPulseAmount = 0.12f;
    [SerializeField] private AudioClip ringSound;
    [SerializeField] private AudioClip[] ringSoundVariants;
    [SerializeField] private float ringVolume = 0.8f;

    [Header("Progress")]
    [SerializeField] private string nextSceneName = "PointClickPrototype";
    [SerializeField] private string returnHomePromptText = "E - вернуться домой";

    private bool playerIsNear;
    private bool isRinging;
    private bool isPartnerIntroRinging;
    private float partnerIntroRemainingDelay;
    private int completedCallsOnThisPhone;
    private PhoneCallData currentCallData;
    private bool partnerIntroDelayInitialized;
    private TMP_Text promptText;
    private Vector3 baseScale;
    private AudioSource ringAudioSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetProgress()
    {
        answeredCalls = 0;
        totalRequiredCalls = 0;
        preparedDay = 0;
        canReturnHome = false;
        returnHomePrompt = null;
        partnerIntroPhone = null;
        partnerIntroCompleted = false;
        partnerIntroRang = false;
    }

    private void OnEnable()
    {
        PrepareProgressForCurrentDay();
        RegisterPhone();
        RecalculateTotalRequiredCalls();
    }

    private void OnDisable()
    {
        activePhones.Remove(this);
        if (partnerIntroPhone == this)
        {
            partnerIntroPhone = null;
        }

        RecalculateTotalRequiredCalls();
    }

    private void Awake()
    {
        baseScale = transform.localScale;
        EnsurePrompt();
        EnsureAudioSource();
    }

    private void Start()
    {
        ResolvePartnerIntroPhone();
        InitializePartnerIntroDelayIfNeeded();
        PrepareSchedule();
        UpdatePrompt();
    }

    private static void PrepareProgressForCurrentDay()
    {
        if (preparedDay == GameDayState.CurrentDay)
        {
            return;
        }

        answeredCalls = 0;
        totalRequiredCalls = 0;
        canReturnHome = false;
        returnHomePrompt = null;
        partnerIntroPhone = null;
        partnerIntroCompleted = GameDayState.CurrentDay != 1;
        partnerIntroRang = false;
        activePhones.Clear();
        preparedDay = GameDayState.CurrentDay;
    }

    private void RegisterPhone()
    {
        if (!activePhones.Contains(this))
        {
            activePhones.Add(this);
        }
    }

    private static void ResolvePartnerIntroPhone()
    {
        if (GameDayState.CurrentDay != 1 || partnerIntroCompleted)
        {
            return;
        }

        PhoneInteractable namedPhone = null;
        PhoneInteractable explicitPhone = null;
        foreach (PhoneInteractable phone in activePhones)
        {
            if (phone == null)
            {
                continue;
            }

            if (phone.IsNamedPartnerIntroPhone())
            {
                namedPhone = phone;
                break;
            }

            if (phone.useAsPartnerIntroPhone)
            {
                explicitPhone = phone;
            }
        }

        if (namedPhone != null)
        {
            SetPartnerIntroPhone(namedPhone);
            return;
        }

        if (explicitPhone != null)
        {
            SetPartnerIntroPhone(explicitPhone);
            return;
        }

        SetPartnerIntroPhone(activePhones.Count > 0 ? activePhones[0] : null);
    }

    private static void SetPartnerIntroPhone(PhoneInteractable phone)
    {
        if (partnerIntroPhone == phone)
        {
            return;
        }

        partnerIntroPhone = phone;
        RecalculateTotalRequiredCalls();
    }

    private static void RecalculateTotalRequiredCalls()
    {
        totalRequiredCalls = 0;
        foreach (PhoneInteractable phone in activePhones)
        {
            if (phone != null)
            {
                totalRequiredCalls += phone.GetRequiredCallCount();
            }
        }
    }

    private void InitializePartnerIntroDelayIfNeeded()
    {
        if (partnerIntroPhone != this || partnerIntroDelayInitialized)
        {
            return;
        }

        partnerIntroRemainingDelay = Mathf.Max(0f, partnerIntroRingDelay);
        partnerIntroDelayInitialized = true;
    }

    private void Update()
    {
        ResolvePartnerIntroPhone();
        InitializePartnerIntroDelayIfNeeded();
        UpdatePartnerIntroSchedule();
        UpdateSchedule();
        UpdateRingAnimation();
        UpdatePrompt();

        if (canReturnHome && !string.IsNullOrWhiteSpace(nextSceneName) && WasInteractPressed())
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        if (!playerIsNear || !isRinging)
        {
            return;
        }

        if (WasInteractPressed())
        {
            PickUpPhone();
        }
    }

    private void PrepareSchedule()
    {
        if ((scheduledCalls == null || scheduledCalls.Length == 0) && callData != null)
        {
            scheduledCalls = new[]
            {
                new PhoneCallSchedule
                {
                    callData = callData,
                    firstRingDelay = 3f,
                    repeatDelay = 0f,
                    repeat = false
                }
            };
        }

        if (scheduledCalls == null)
        {
            return;
        }

        foreach (PhoneCallSchedule schedule in scheduledCalls)
        {
            if (schedule == null)
            {
                continue;
            }

            schedule.nextRingTime = Time.time + Mathf.Max(0f, schedule.firstRingDelay);
            schedule.remainingDelay = Mathf.Max(0f, schedule.firstRingDelay);
            schedule.completed = false;
        }
    }

    private void UpdateSchedule()
    {
        if (isRinging || scheduledCalls == null || GameDayState.IsDialogueActive || IsRegularCallsBlockedForCurrentDay())
        {
            return;
        }

        foreach (PhoneCallSchedule schedule in scheduledCalls)
        {
            if (schedule == null || schedule.completed || schedule.callData == null)
            {
                continue;
            }

            schedule.remainingDelay -= Time.deltaTime;
            if (schedule.remainingDelay <= 0f)
            {
                StartRinging(schedule);
                return;
            }
        }
    }

    private void UpdatePartnerIntroSchedule()
    {
        if (partnerIntroPhone != this || partnerIntroCompleted || partnerIntroRang || isRinging || GameDayState.IsDialogueActive)
        {
            return;
        }

        partnerIntroRemainingDelay -= Time.deltaTime;
        if (partnerIntroRemainingDelay <= 0f)
        {
            StartPartnerIntroRinging();
        }
    }

    private void StartRinging(PhoneCallSchedule schedule)
    {
        currentCallData = PickCallData(schedule.callData);
        if (currentCallData == null)
        {
            schedule.completed = true;
            return;
        }

        isRinging = true;
        isPartnerIntroRinging = false;
        StartRingSound();

        if (schedule.repeat && schedule.repeatDelay > 0f)
        {
            schedule.nextRingTime = Time.time + schedule.repeatDelay;
            schedule.remainingDelay = schedule.repeatDelay;
        }
        else
        {
            schedule.completed = true;
        }
    }

    private void StartPartnerIntroRinging()
    {
        currentCallData = PickPartnerIntroCallData();
        if (currentCallData == null)
        {
            partnerIntroCompleted = true;
            return;
        }

        isRinging = true;
        isPartnerIntroRinging = true;
        partnerIntroRang = true;
        StartRingSound();
    }

    private void PickUpPhone()
    {
        isRinging = false;
        transform.localScale = baseScale;
        StopRingSound();
        UpdatePrompt();

        PhoneCallData callToOpen = currentCallData != null ? currentCallData : callData;
        if (callToOpen != null)
        {
            phoneCallUI.OpenCall(callToOpen, this, !isPartnerIntroRinging);
        }
    }

    public void MarkCallCompleted()
    {
        MarkCallCompleted(true);
    }

    public void MarkCallCompleted(bool countsTowardDayProgress)
    {
        if (!countsTowardDayProgress)
        {
            partnerIntroCompleted = true;
            isPartnerIntroRinging = false;
            RecalculateTotalRequiredCalls();
            return;
        }

        int requiredCalls = GetRequiredCallCount();
        if (completedCallsOnThisPhone >= requiredCalls)
        {
            return;
        }

        completedCallsOnThisPhone++;
        answeredCalls++;

        if (totalRequiredCalls > 0 && answeredCalls >= totalRequiredCalls)
        {
            canReturnHome = true;
            ShowReturnHomePrompt();
        }
    }

    private int GetRequiredCallCount()
    {
        if (IsPartnerOnlyPhoneInCurrentDay())
        {
            return 0;
        }

        if (scheduledCalls != null && scheduledCalls.Length > 0)
        {
            return scheduledCalls.Length;
        }

        return callData != null ? 1 : 0;
    }

    private PhoneCallData PickCallData(PhoneCallData fallback)
    {
        if (ShouldUseCurrentDaySubfolder())
        {
            PhoneCallData[] dayCalls = Resources.LoadAll<PhoneCallData>(GetCurrentDayCallsFolder());
            PhoneCallData[] regularDayCalls = FilterRegularCalls(dayCalls);
            if (regularDayCalls.Length > 0)
            {
                return regularDayCalls[Random.Range(0, regularDayCalls.Length)];
            }

            return null;
        }

        if (!useRandomDialogsFromResources || string.IsNullOrWhiteSpace(randomDialogsResourcesFolder))
        {
            return fallback;
        }

        PhoneCallData[] possibleCalls = Resources.LoadAll<PhoneCallData>(randomDialogsResourcesFolder);
        PhoneCallData[] regularCalls = FilterRegularCalls(possibleCalls);
        if (regularCalls.Length == 0)
        {
            return fallback;
        }

        return regularCalls[Random.Range(0, regularCalls.Length)];
    }

    private PhoneCallData[] FilterRegularCalls(PhoneCallData[] calls)
    {
        if (calls == null || calls.Length == 0)
        {
            return new PhoneCallData[0];
        }

        List<PhoneCallData> regularCalls = new List<PhoneCallData>();
        foreach (PhoneCallData call in calls)
        {
            if (CanUseAsRegularCall(call))
            {
                regularCalls.Add(call);
            }
        }

        return regularCalls.ToArray();
    }

    private bool CanUseAsRegularCall(PhoneCallData data)
    {
        return data != null
            && data.acceptedAnswers != null
            && data.acceptedAnswers.Length > 0
            && data.answerTemplateTokens != null
            && data.answerTemplateTokens.Length > 0
            && data.wordBank != null
            && data.wordBank.Length > 0;
    }

    private bool ShouldUseCurrentDaySubfolder()
    {
        return useCurrentDaySubfolders
            || string.IsNullOrWhiteSpace(randomDialogsResourcesFolder)
            || string.Equals(randomDialogsResourcesFolder, "PhoneCalls", System.StringComparison.OrdinalIgnoreCase);
    }

    private string GetCurrentDayCallsFolder()
    {
        string baseFolder = string.IsNullOrWhiteSpace(randomDialogsResourcesFolder)
            ? "PhoneCalls"
            : randomDialogsResourcesFolder;

        return GameDayState.GetPhoneCallsFolder(baseFolder);
    }

    private PhoneCallData PickPartnerIntroCallData()
    {
        if (partnerIntroCallData != null)
        {
            return partnerIntroCallData;
        }

        if (!string.IsNullOrWhiteSpace(partnerIntroResourcesPath))
        {
            PhoneCallData resourceCall = Resources.Load<PhoneCallData>(partnerIntroResourcesPath);
            if (resourceCall != null)
            {
                return resourceCall;
            }
        }

        return callData;
    }

    private bool IsRegularCallsBlockedForCurrentDay()
    {
        if (!IsFirstPhoneSceneDay())
        {
            return false;
        }

        return !partnerIntroCompleted || IsPartnerOnlyPhoneInCurrentDay();
    }

    private bool IsPartnerOnlyPhoneInCurrentDay()
    {
        return IsFirstPhoneSceneDay() && (partnerIntroPhone == this || IsNamedPartnerIntroPhone());
    }

    private static bool IsFirstPhoneSceneDay()
    {
        return GameDayState.CurrentDay == 1;
    }

    private bool IsNamedPartnerIntroPhone()
    {
        string targetName = string.IsNullOrWhiteSpace(partnerIntroPhoneObjectName)
            ? "Phone"
            : partnerIntroPhoneObjectName.Trim();

        return string.Equals(name, targetName, System.StringComparison.Ordinal);
    }

    private static bool WasInteractPressed()
    {
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }

    private void UpdateRingAnimation()
    {
        if (!isRinging)
        {
            transform.localScale = baseScale;
            return;
        }

        float pulse = Mathf.Sin(Time.time * ringPulseSpeed) * ringPulseAmount;
        transform.localScale = new Vector3(
            baseScale.x * (1f + pulse),
            baseScale.y * (1f - pulse * 0.35f),
            baseScale.z);
    }

    private void EnsurePrompt()
    {
        if (promptText != null)
        {
            return;
        }

        promptText = GetComponentInChildren<TextMeshPro>(true);
        GameObject promptObject;

        if (promptText == null)
        {
            promptObject = new GameObject("PickupPrompt");
            promptObject.transform.SetParent(transform, false);
            promptText = promptObject.AddComponent<TextMeshPro>();
        }
        else
        {
            promptObject = promptText.gameObject;
        }

        promptObject.transform.localPosition = new Vector3(0f, 0.95f, 0f);

        promptText.text = pickupPrompt;
        promptText.fontSize = 2.2f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.enableAutoSizing = true;
        promptText.fontSizeMin = 1.2f;
        promptText.fontSizeMax = 2.2f;

        RectTransform rectTransform = promptText.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(4f, 0.6f);

        MeshRenderer renderer = promptText.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 50;
        }

        promptObject.SetActive(false);
    }

    private void EnsureAudioSource()
    {
        ringAudioSource = GetComponent<AudioSource>();
        if (ringAudioSource == null)
        {
            ringAudioSource = gameObject.AddComponent<AudioSource>();
        }

        ringAudioSource.playOnAwake = false;
        ringAudioSource.loop = true;
        ringAudioSource.volume = ringVolume;
    }

    private void StartRingSound()
    {
        AudioClip selectedRing = PickRandomClip(ringSoundVariants);
        if (selectedRing == null)
        {
            if (ringSoundVariants == null || ringSoundVariants.Length == 0)
            {
                ringSoundVariants = Resources.LoadAll<AudioClip>("GameAudio/PhoneRing");
                selectedRing = PickRandomClip(ringSoundVariants);
            }
            if (selectedRing == null) selectedRing = ringSound;
        }
        if (selectedRing == null || ringAudioSource == null)
        {
            return;
        }

        ringAudioSource.clip = selectedRing;
        ringAudioSource.volume = ringVolume;

        if (!ringAudioSource.isPlaying)
        {
            ringAudioSource.Play();
        }
    }

    private static AudioClip PickRandomClip(AudioClip[] clips)
    {
        return clips != null && clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;
    }

    private void StopRingSound()
    {
        if (ringAudioSource != null && ringAudioSource.isPlaying)
        {
            ringAudioSource.Stop();
        }
    }

    private void UpdatePrompt()
    {
        if (promptText == null)
        {
            return;
        }

        promptText.gameObject.SetActive(playerIsNear && isRinging);
    }

    private void ShowReturnHomePrompt()
    {
        if (returnHomePrompt != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("ReturnHomePromptCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = new GameObject("ReturnHomePromptText");
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 120f);
        rect.sizeDelta = new Vector2(720f, 80f);

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = returnHomePromptText;
        text.fontSize = 36f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        returnHomePrompt = canvasObject;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
        }
    }

}
