using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhoneCallUI : MonoBehaviour
{
    [Header("Main window")]
    [SerializeField] private GameObject rootPanel;

    [Header("Texts")]
    [SerializeField] private TMP_Text clientLineText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text reactionText;

    [Header("Containers")]
    [SerializeField] private Transform answerContainer;
    [SerializeField] private Transform wordBankContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject fixedWordPrefab;
    [SerializeField] private WordSlot slotPrefab;
    [SerializeField] private DraggableWord draggableWordPrefab;

    [Header("Buttons")]
    [SerializeField] private Button answerButton;
    [SerializeField] private Button nextLineButton;

    [Header("Systems")]
    [SerializeField] private PhoneCallResultSystem resultSystem;

    [Header("NPC Letter Voice (Animal Crossing style)")]
    [Tooltip("Включает печать и озвучивание отдельных букв в репликах NPC.")]
    [SerializeField] private bool enableLetterVoice = true;
    [Tooltip("Необязательный собственный звук буквы. Если поле пустое, звук создаётся автоматически.")]
    [SerializeField] private AudioClip customLetterSound;
    [Tooltip("Включите для записи, в которой подряд произнесены все буквы русского алфавита.")]
    [SerializeField] private bool customSoundContainsCyrillicAlphabet = true;
    [Tooltip("Порядок букв в записи. Можно изменить, если буквы записаны в другом порядке.")]
    [SerializeField] private string recordedAlphabet = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
    [Tooltip("Сдвиг начала каждой буквы внутрь её фрагмента, в секундах.")]
    [SerializeField, Range(0f, 0.2f)] private float alphabetSegmentStartOffset;
    [Tooltip("Насколько раньше остановить фрагмент перед следующей буквой, в секундах.")]
    [SerializeField, Range(0f, 0.2f)] private float alphabetSegmentEndPadding = 0.01f;
    [Tooltip("Громкость звука каждой буквы.")]
    [SerializeField, Range(0f, 1f)] private float letterVoiceVolume = 0.18f;
    [Tooltip("Минимальная и максимальная высота тона. X не должен превышать Y.")]
    [SerializeField] private Vector2 letterPitchRange = new Vector2(0.85f, 1.25f);
    [Tooltip("Если включено, каждый PhoneCallData может задавать собственную скорость печати.")]
    [SerializeField] private bool useCallSpecificTypingSpeed;
    [Tooltip("Пауза между буквами в секундах.")]
    [SerializeField, Range(0.005f, 0.2f)] private float defaultCharacterDelay = 0.03f;

    [Header("Generated Letter Sound (used when Custom Letter Sound is empty)")]
    [Tooltip("Основная частота автоматически созданного звука.")]
    [SerializeField, Range(100f, 1400f)] private float generatedSoundFrequency = 520f;
    [Tooltip("Продолжительность автоматически созданного звука в секундах.")]
    [SerializeField, Range(0.01f, 0.15f)] private float generatedSoundDuration = 0.041f;

    private readonly List<WordSlot> slots = new List<WordSlot>();
    private readonly List<string> clientLineParts = new List<string>();

    private PhoneCallData currentCall;
    private Canvas canvas;
    private PhoneInteractable activePhone;
    private float timeLeft;
    private bool isCallOpen;
    private bool countsForPhoneProgress = true;
    private bool isListeningToClient;
    private bool isTypingClientLine;
    private bool responseControlsBuilt;
    private int currentClientLinePartIndex;
    private string currentFullClientLine;
    private Coroutine typeLineRoutine;
    private CanvasGroup answerContainerGroup;
    private CanvasGroup wordBankContainerGroup;
    private CanvasGroup answerButtonGroup;
    private AudioSource letterVoiceSource;
    private AudioClip generatedLetterBlip;
    private Coroutine reactionRoutine;
    private Coroutine stopRecordedLetterRoutine;
    private AudioClip[] hangupSounds;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        EnsureLetterVoice();
        hangupSounds = Resources.LoadAll<AudioClip>("GameAudio/PhoneHangup");

        rootPanel.SetActive(false);

        EnsureNextLineButton();
        answerButton.onClick.AddListener(CheckAnswer);
        nextLineButton.onClick.AddListener(GoToNextClientLine);
        nextLineButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isCallOpen || isListeningToClient)
        {
            return;
        }

        timeLeft -= Time.deltaTime;
        timerText.text = Mathf.CeilToInt(timeLeft).ToString();

        if (timeLeft <= 0)
        {
            TimeIsOver();
        }
    }

    public void OpenCall(PhoneCallData callData)
    {
        OpenCall(callData, null, true);
    }

    public void OpenCall(PhoneCallData callData, PhoneInteractable sourcePhone)
    {
        OpenCall(callData, sourcePhone, true);
    }

    public void OpenCall(PhoneCallData callData, PhoneInteractable sourcePhone, bool countForPhoneProgress)
    {
        currentCall = callData;
        activePhone = sourcePhone;
        countsForPhoneProgress = countForPhoneProgress;

        rootPanel.SetActive(true);
        isCallOpen = true;
        isListeningToClient = false;
        isTypingClientLine = false;
        responseControlsBuilt = false;
        currentClientLinePartIndex = 0;
        GameDayState.BeginDialogue();

        timeLeft = currentCall.timeLimit;

        ConfigureClientLineText();
        ConfigureResponseAreas();
        reactionText.text = "";

        BuildClientLineParts();
        StartClientLinePresentation();
    }

    private void BuildClientLineParts()
    {
        clientLineParts.Clear();

        if (currentCall.useClientLineParts && currentCall.clientLineParts != null && currentCall.clientLineParts.Length > 0)
        {
            foreach (string linePart in currentCall.clientLineParts)
            {
                if (!string.IsNullOrWhiteSpace(linePart))
                {
                    clientLineParts.Add(linePart);
                }
            }
        }

        if (clientLineParts.Count == 0)
        {
            clientLineParts.Add(currentCall.clientLine);
        }
    }

    private void StartClientLinePresentation()
    {
        SetAnswerControlsVisible(false);

        bool shouldListenFirst = true;
        if (!shouldListenFirst)
        {
            clientLineText.text = clientLineParts[0];
            BuildResponseControls();
            SetAnswerControlsVisible(true);
            return;
        }

        isListeningToClient = true;
        nextLineButton.gameObject.SetActive(true);
        ShowClientLinePart(0);
    }

    private void ShowClientLinePart(int partIndex)
    {
        currentClientLinePartIndex = partIndex;
        currentFullClientLine = clientLineParts[Mathf.Clamp(partIndex, 0, clientLineParts.Count - 1)];

        if (typeLineRoutine != null)
        {
            StopCoroutine(typeLineRoutine);
            typeLineRoutine = null;
        }

        typeLineRoutine = StartCoroutine(TypeClientLineRoutine(currentFullClientLine));
    }

    private IEnumerator TypeClientLineRoutine(string line)
    {
        isTypingClientLine = true;
        clientLineText.text = "";

        int step = 1;
        float configuredDelay = useCallSpecificTypingSpeed && currentCall.typeStepDelay > 0f
            ? currentCall.typeStepDelay
            : defaultCharacterDelay;
        float delay = Mathf.Max(0.005f, configuredDelay);

        for (int i = 0; i < line.Length; i += step)
        {
            int length = Mathf.Min(i + step, line.Length);
            clientLineText.text = line.Substring(0, length);
            PlayLetterVoice(line[length - 1]);
            yield return new WaitForSeconds(delay);
        }

        clientLineText.text = line;
        isTypingClientLine = false;
        typeLineRoutine = null;

        if (IsLastClientLinePart() && !IsDialogueOnlyCall())
        {
            CompleteClientListening();
        }
    }

    private void GoToNextClientLine()
    {
        if (!isListeningToClient)
        {
            return;
        }

        if (isTypingClientLine)
        {
            if (typeLineRoutine != null)
            {
                StopCoroutine(typeLineRoutine);
                typeLineRoutine = null;
            }

            clientLineText.text = currentFullClientLine;
            isTypingClientLine = false;

            if (IsLastClientLinePart() && !IsDialogueOnlyCall())
            {
                CompleteClientListening();
            }

            return;
        }

        if (currentClientLinePartIndex < clientLineParts.Count - 1)
        {
            ShowClientLinePart(currentClientLinePartIndex + 1);
            return;
        }

        if (IsDialogueOnlyCall())
        {
            FinishDialogueOnlyCall();
        }
        else
        {
            CompleteClientListening();
        }
    }

    private bool IsLastClientLinePart()
    {
        return currentClientLinePartIndex >= clientLineParts.Count - 1;
    }

    private void CompleteClientListening()
    {
        isListeningToClient = false;
        nextLineButton.gameObject.SetActive(false);
        BuildResponseControls();
        SetAnswerControlsVisible(true);
    }

    private void BuildResponseControls()
    {
        if (responseControlsBuilt)
        {
            return;
        }

        BuildAnswerArea();
        BuildWordBank();
        RebuildResponseLayouts();
        responseControlsBuilt = true;
    }

    private void RebuildResponseLayouts()
    {
        ForceRebuildLayout(answerContainer as RectTransform);
        ForceRebuildLayout(wordBankContainer as RectTransform);
        Canvas.ForceUpdateCanvases();
    }

    private void ForceRebuildLayout(RectTransform rect)
    {
        if (rect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }

    private void BuildAnswerArea()
    {
        ClearContainer(answerContainer);
        slots.Clear();

        if (currentCall.answerTemplateTokens == null)
        {
            return;
        }

        foreach (string token in currentCall.answerTemplateTokens)
        {
            if (IsBlankToken(token))
            {
                WordSlot slot = Instantiate(slotPrefab, answerContainer);
                slot.Setup(wordBankContainer);
                ConfigureAnswerItem(slot.GetComponent<RectTransform>(), 100f, 28f);
                slots.Add(slot);
            }
            else
            {
                GameObject fixedWordObject = Instantiate(fixedWordPrefab, answerContainer);
                ConfigureAnswerItem(fixedWordObject.GetComponent<RectTransform>(), 110f, 28f);

                TMP_Text text = fixedWordObject.GetComponent<TMP_Text>();
                text.text = token;
                text.fontSize = 18f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 10f;
                text.fontSizeMax = 18f;
            }
        }
    }

    private void BuildWordBank()
    {
        ClearContainer(wordBankContainer);

        HashSet<string> addedWords = new HashSet<string>();
        AddWordsToBank(currentCall.wordBank, addedWords);
        AddWordsToBank(GameDayState.EarnedWords, addedWords);
    }

    private void AddWordsToBank(IEnumerable<string> words, HashSet<string> addedWords)
    {
        if (words == null)
        {
            return;
        }

        foreach (string word in words)
        {
            if (string.IsNullOrWhiteSpace(word) || !addedWords.Add(word.Trim().ToLowerInvariant()))
            {
                continue;
            }

            DraggableWord wordObject = Instantiate(draggableWordPrefab, wordBankContainer);
            wordObject.Init(word.Trim(), canvas);
        }
    }

    private void CheckAnswer()
    {
        string[] insertedWords = GetInsertedWords();

        if (HasNoInsertedWords(insertedWords))
        {
            FinishInvalidAnswer();
            return;
        }

        if (HasEmptySlots(insertedWords))
        {
            reactionText.text = "Сначала заполните все пропуски.";

            foreach (WordSlot slot in slots)
            {
                if (slot.IsEmpty)
                {
                    slot.FlashRed();
                }
            }

            return;
        }

        PhoneAnswerVariant matchedAnswer = FindMatchingAnswer(insertedWords);

        if (matchedAnswer == null)
        {
            FinishInvalidAnswer();
            return;
        }

        FinishCall(matchedAnswer);
    }

    private string[] GetInsertedWords()
    {
        string[] result = new string[slots.Count];

        for (int i = 0; i < slots.Count; i++)
        {
            result[i] = slots[i].CurrentWord;
        }

        return result;
    }

    private bool HasEmptySlots(string[] insertedWords)
    {
        foreach (string word in insertedWords)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasNoInsertedWords(string[] insertedWords)
    {
        if (insertedWords == null || insertedWords.Length == 0)
        {
            return false;
        }

        foreach (string word in insertedWords)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBlankToken(string token)
    {
        return token == "_" || token == "-";
    }

    private PhoneAnswerVariant FindMatchingAnswer(string[] insertedWords)
    {
        if (currentCall.acceptedAnswers == null)
        {
            return null;
        }

        foreach (PhoneAnswerVariant answer in currentCall.acceptedAnswers)
        {
            if (IsSameAnswer(insertedWords, answer.wordsInOrder))
            {
                return answer;
            }
        }

        return null;
    }

    private bool IsSameAnswer(string[] insertedWords, string[] correctWords)
    {
        if (correctWords == null || insertedWords.Length != correctWords.Length)
        {
            return false;
        }

        for (int i = 0; i < insertedWords.Length; i++)
        {
            string playerWord = insertedWords[i].Trim().ToLower();
            string neededWord = correctWords[i].Trim().ToLower();

            if (playerWord != neededWord)
            {
                return false;
            }
        }

        return true;
    }

    private void TimeIsOver()
    {
        if (!isCallOpen)
        {
            return;
        }

        if (currentCall.timeoutResult != null)
        {
            FinishCall(currentCall.timeoutResult);
        }
        else
        {
            isCallOpen = false;
            reactionText.text = "Вы не успели ответить. Клиент недоволен.";
            Invoke(nameof(CloseCall), 2f);
        }
    }

    private void FinishCall(PhoneAnswerVariant answer)
    {
        isCallOpen = false;

        if (resultSystem != null)
        {
            resultSystem.ApplyAnswer(answer);
        }

        PresentNpcReaction(answer.callerReaction);

        Invoke(nameof(CloseCall), 2f);
    }

    private void FinishInvalidAnswer()
    {
        if (resultSystem != null)
        {
            resultSystem.ApplyInvalidAnswer(currentCall);
        }

        PresentNpcReaction(currentCall.invalidAnswerReaction);

        foreach (WordSlot slot in slots)
        {
            slot.FlashRed();
        }

        isCallOpen = false;
        Invoke(nameof(CloseCall), 2f);
    }

    private void FinishDialogueOnlyCall()
    {
        isCallOpen = false;
        Invoke(nameof(CloseCall), 0.1f);
    }

    private void CloseCall()
    {
        if (typeLineRoutine != null)
        {
            StopCoroutine(typeLineRoutine);
            typeLineRoutine = null;
        }

        if (reactionRoutine != null)
        {
            StopCoroutine(reactionRoutine);
            reactionRoutine = null;
        }

        if (stopRecordedLetterRoutine != null)
        {
            StopCoroutine(stopRecordedLetterRoutine);
            stopRecordedLetterRoutine = null;
        }
        if (letterVoiceSource != null)
        {
            letterVoiceSource.Stop();
        }

        isListeningToClient = false;
        isTypingClientLine = false;
        nextLineButton.gameObject.SetActive(false);
        rootPanel.SetActive(false);
        GameDayState.EndDialogue();

        if (activePhone != null)
        {
            activePhone.MarkCallCompleted(countsForPhoneProgress);
            activePhone = null;
        }
        PlayRandomHangupSound();
    }

    private void PlayRandomHangupSound()
    {
        if (hangupSounds == null || hangupSounds.Length == 0 || letterVoiceSource == null) return;
        letterVoiceSource.pitch = 1f;
        letterVoiceSource.PlayOneShot(hangupSounds[Random.Range(0, hangupSounds.Length)], 0.7f);
    }

    private void PresentNpcReaction(string line)
    {
        if (reactionRoutine != null)
        {
            StopCoroutine(reactionRoutine);
        }

        reactionRoutine = StartCoroutine(TypeReactionRoutine(line ?? string.Empty));
    }

    private IEnumerator TypeReactionRoutine(string line)
    {
        reactionText.text = string.Empty;
        float delay = Mathf.Max(0.005f, defaultCharacterDelay);
        for (int i = 0; i < line.Length; i++)
        {
            reactionText.text = line.Substring(0, i + 1);
            PlayLetterVoice(line[i]);
            yield return new WaitForSeconds(delay);
        }
        reactionRoutine = null;
    }

    private void EnsureLetterVoice()
    {
        letterVoiceSource = GetComponent<AudioSource>();
        if (letterVoiceSource == null)
        {
            letterVoiceSource = gameObject.AddComponent<AudioSource>();
        }
        letterVoiceSource.playOnAwake = false;
        letterVoiceSource.loop = false;
        generatedLetterBlip = CreateLetterBlip(generatedSoundFrequency, generatedSoundDuration);
    }

    private void PlayLetterVoice(char character)
    {
        if (!enableLetterVoice || char.IsWhiteSpace(character) || char.IsPunctuation(character) || letterVoiceSource == null)
        {
            return;
        }

        char normalizedCharacter = char.ToLowerInvariant(character);
        AudioClip activeAlphabetClip = GetActiveAlphabetClip();
        string activeAlphabet = GetActiveRecordedAlphabet();
        if (activeAlphabetClip != null && !string.IsNullOrEmpty(activeAlphabet))
        {
            int letterIndex = activeAlphabet.IndexOf(normalizedCharacter);
            if (letterIndex >= 0)
            {
                PlayRecordedAlphabetLetter(letterIndex, activeAlphabetClip, activeAlphabet);
                return;
            }
        }

        float normalized = (character % 17) / 16f;
        letterVoiceSource.pitch = Mathf.Lerp(letterPitchRange.x, letterPitchRange.y, normalized);
        // Никогда не проигрываем запись всего алфавита как обычный звук одной буквы.
        AudioClip sound = customLetterSound != null && !customSoundContainsCyrillicAlphabet
            ? customLetterSound
            : generatedLetterBlip;
        if (sound != null)
        {
            letterVoiceSource.PlayOneShot(sound, letterVoiceVolume * GameAudioSettings.VoiceVolume);
        }
    }

    private void PlayRecordedAlphabetLetter(int letterIndex, AudioClip alphabetClip, string alphabet)
    {
        if (alphabetClip == null || string.IsNullOrEmpty(alphabet) || letterIndex < 0 || letterIndex >= alphabet.Length)
        {
            return;
        }

        float startOffset = currentCall != null && currentCall.clientAlphabetVoice != null
            ? currentCall.clientSegmentStartOffset
            : alphabetSegmentStartOffset;
        float endPadding = currentCall != null && currentCall.clientAlphabetVoice != null
            ? currentCall.clientSegmentEndPadding
            : alphabetSegmentEndPadding;
        float volumeMultiplier = currentCall != null && currentCall.clientAlphabetVoice != null
            ? currentCall.clientVoiceVolumeMultiplier
            : 1f;
        float segmentLength = alphabetClip.length / alphabet.Length;
        float segmentStart = letterIndex * segmentLength + startOffset;
        float segmentDuration = Mathf.Max(0.01f, segmentLength - startOffset - endPadding);

        if (stopRecordedLetterRoutine != null)
        {
            StopCoroutine(stopRecordedLetterRoutine);
        }

        letterVoiceSource.Stop();
        letterVoiceSource.clip = alphabetClip;
        letterVoiceSource.pitch = 1f;
        letterVoiceSource.volume = letterVoiceVolume * volumeMultiplier * GameAudioSettings.VoiceVolume;
        letterVoiceSource.time = Mathf.Clamp(segmentStart, 0f, alphabetClip.length - 0.01f);
        letterVoiceSource.Play();
        stopRecordedLetterRoutine = StartCoroutine(StopRecordedLetter(segmentDuration));
    }

    private AudioClip GetActiveAlphabetClip()
    {
        if (currentCall != null && currentCall.clientAlphabetVoice != null)
        {
            return currentCall.clientAlphabetVoice;
        }
        return customSoundContainsCyrillicAlphabet ? customLetterSound : null;
    }

    private string GetActiveRecordedAlphabet()
    {
        if (currentCall != null && currentCall.clientAlphabetVoice != null && !string.IsNullOrEmpty(currentCall.clientRecordedAlphabet))
        {
            return currentCall.clientRecordedAlphabet;
        }
        return recordedAlphabet;
    }

    private IEnumerator StopRecordedLetter(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        letterVoiceSource.Stop();
        stopRecordedLetterRoutine = null;
    }

    private static AudioClip CreateLetterBlip(float frequency, float duration)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.Max(64, Mathf.RoundToInt(sampleRate * duration));
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - i / (float)sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * envelope * 0.32f;
        }
        AudioClip clip = AudioClip.Create("Generated NPC Letter Blip", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnValidate()
    {
        letterPitchRange.x = Mathf.Clamp(letterPitchRange.x, -3f, 3f);
        letterPitchRange.y = Mathf.Clamp(letterPitchRange.y, letterPitchRange.x, 3f);
        defaultCharacterDelay = Mathf.Max(0.005f, defaultCharacterDelay);
        if (string.IsNullOrEmpty(recordedAlphabet))
        {
            recordedAlphabet = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        }

        if (Application.isPlaying && customLetterSound == null)
        {
            generatedLetterBlip = CreateLetterBlip(generatedSoundFrequency, generatedSoundDuration);
        }
    }

    private void ConfigureClientLineText()
    {
        RectTransform textRect = clientLineText.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(760f, 120f);

        clientLineText.fontSize = 24f;
        clientLineText.enableAutoSizing = true;
        clientLineText.fontSizeMin = 14f;
        clientLineText.fontSizeMax = 24f;
        clientLineText.alignment = TextAlignmentOptions.Center;
        clientLineText.textWrappingMode = TextWrappingModes.Normal;
    }

    private void ConfigureResponseAreas()
    {
        ConfigureRect(answerContainer as RectTransform, new Vector2(0f, 30f), new Vector2(760f, 60f));
        ConfigureRect(wordBankContainer as RectTransform, new Vector2(0f, -90f), new Vector2(760f, 70f));
        ConfigureRect(answerButton.GetComponent<RectTransform>(), new Vector2(0f, -175f), new Vector2(160f, 30f));

        ConfigureHorizontalLayout(answerContainer, 8f, TextAnchor.MiddleCenter);
        ConfigureHorizontalLayout(wordBankContainer, 10f, TextAnchor.MiddleCenter);
    }

    private bool IsDialogueOnlyCall()
    {
        return currentCall.acceptedAnswers == null || currentCall.acceptedAnswers.Length == 0;
    }

    private void SetAnswerControlsVisible(bool visible)
    {
        if (timerText != null)
        {
            timerText.gameObject.SetActive(visible);
        }

        if (answerContainer != null)
        {
            SetCanvasGroupVisible(ref answerContainerGroup, answerContainer.gameObject, visible);
        }

        if (wordBankContainer != null)
        {
            SetCanvasGroupVisible(ref wordBankContainerGroup, wordBankContainer.gameObject, visible);
        }

        if (answerButton != null)
        {
            SetCanvasGroupVisible(ref answerButtonGroup, answerButton.gameObject, visible);
        }
    }

    private void SetCanvasGroupVisible(ref CanvasGroup group, GameObject target, bool visible)
    {
        if (target == null)
        {
            return;
        }

        if (!target.activeSelf)
        {
            target.SetActive(true);
        }

        if (group == null)
        {
            group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
            }
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void EnsureNextLineButton()
    {
        if (nextLineButton != null)
        {
            return;
        }

        GameObject buttonObject = new GameObject("NextLineButton");
        buttonObject.transform.SetParent(rootPanel.transform, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -35f);
        rect.sizeDelta = new Vector2(180f, 42f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.28f, 0.96f);

        nextLineButton = buttonObject.AddComponent<Button>();
        nextLineButton.targetGraphic = image;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "Далее";
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
    }

    private void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private void ConfigureHorizontalLayout(Transform container, float spacing, TextAnchor alignment)
    {
        HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            return;
        }

        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private void ConfigureAnswerItem(RectTransform rect, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}
