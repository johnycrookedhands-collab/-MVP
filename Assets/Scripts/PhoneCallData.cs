using UnityEngine;

[CreateAssetMenu(menuName = "Game/Phone Call Data")]
public class PhoneCallData : ScriptableObject
{
    [TextArea(2, 5)]
    public string clientLine;

    [Header("Long dialogue")]
    public bool useClientLineParts;

    [TextArea(2, 5)]
    public string[] clientLineParts;

    public bool typeClientLine;

    [Min(1)]
    public int charactersPerTypeStep = 2;

    [Min(0.005f)]
    public float typeStepDelay = 0.03f;

    [Header("Client Letter Voice Override (optional)")]
    [Tooltip("Оставьте пустым, чтобы использовать стандартный голос из PhoneCallUI.")]
    public AudioClip clientAlphabetVoice;
    [Tooltip("Порядок букв в индивидуальной записи.")]
    public string clientRecordedAlphabet = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
    [Tooltip("Сдвиг начала каждого буквенного сегмента, в секундах.")]
    [Range(0f, 0.2f)] public float clientSegmentStartOffset;
    [Tooltip("Обрезка конца каждого буквенного сегмента, в секундах.")]
    [Range(0f, 0.2f)] public float clientSegmentEndPadding = 0.01f;
    [Tooltip("Дополнительный множитель громкости только для этого диалога.")]
    [Range(0f, 2f)] public float clientVoiceVolumeMultiplier = 1f;

    [Header("Шаблон ответа")]
    [Tooltip("Обычные слова пиши как есть. Пропуски обозначай символом _.")]
    public string[] answerTemplateTokens;

    [Header("Слова, которые игрок может перетаскивать")]
    public string[] wordBank;

    [Header("Ответы, которые игра принимает")]
    public PhoneAnswerVariant[] acceptedAnswers;

    [Header("Если время вышло")]
    public PhoneAnswerVariant timeoutResult;

    [Header("Настройки")]
    public float timeLimit = 30f;

    [TextArea(2, 4)]
    public string invalidAnswerReaction = "Ответ звучит неправильно. Попробуйте иначе.";

    [Header("Hidden failure result")]
    public int invalidAnswerSectInfluenceDelta = 10;
}
