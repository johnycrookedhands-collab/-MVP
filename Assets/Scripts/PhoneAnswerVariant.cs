using UnityEngine;

[System.Serializable]
public class PhoneAnswerVariant
{
    public string answerName = "Правильный ответ";

    public string[] wordsInOrder;

    public int callerMoodDelta = 10;
    public int sectInfluenceDelta = -5;

    [TextArea(2, 4)]
    public string callerReaction = "Клиент успокоился.";
}