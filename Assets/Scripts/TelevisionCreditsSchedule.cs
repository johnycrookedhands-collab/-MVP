using System;
using UnityEngine;

[Serializable]
public class TelevisionCreditsDay
{
    [Min(1)] public int day = 1;
    [TextArea(2, 5)] public string creditsText;
    [Tooltip("Слово из титров, которое игрок должен найти.")]
    public string rewardWord;
    [Tooltip("Скорость движения титров слева направо, пикселей в секунду.")]
    [Min(10f)] public float scrollSpeed = 90f;
}

[CreateAssetMenu(menuName = "Game/Television Credits Schedule")]
public class TelevisionCreditsSchedule : ScriptableObject
{
    [SerializeField] private TelevisionCreditsDay[] days;

    public TelevisionCreditsDay GetDay(int day)
    {
        if (days == null) return null;
        foreach (TelevisionCreditsDay entry in days)
        {
            if (entry != null && entry.day == day) return entry;
        }
        return null;
    }
}
