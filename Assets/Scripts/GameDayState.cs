using System.Collections.Generic;
using UnityEngine;

public static class GameDayState
{
    public const string PhoneSceneName = "SampleScene";
    public const string HomeSceneName = "PointClickPrototype";

    private static readonly List<string> earnedWords = new List<string>();
    private static readonly HashSet<int> completedWordRewardDays = new HashSet<int>();
    private static int dialogueLockCount;

    public static int CurrentDay { get; private set; } = 1;
    public static IReadOnlyList<string> EarnedWords => earnedWords;
    public static bool IsDialogueActive => dialogueLockCount > 0;

    public static void AdvanceDay()
    {
        CurrentDay = Mathf.Max(1, CurrentDay + 1);
    }

    public static bool AddEarnedWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        string trimmedWord = word.Trim();
        foreach (string earnedWord in earnedWords)
        {
            if (string.Equals(earnedWord, trimmedWord, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        earnedWords.Add(trimmedWord);
        return true;
    }

    public static int AddEarnedWords(IEnumerable<string> words)
    {
        if (words == null)
        {
            return 0;
        }

        int addedCount = 0;
        foreach (string word in words)
        {
            if (AddEarnedWord(word))
            {
                addedCount++;
            }
        }

        return addedCount;
    }

    public static bool HasCompletedWordRewardForDay(int day)
    {
        return completedWordRewardDays.Contains(day);
    }

    public static void MarkWordRewardCompletedForDay(int day)
    {
        completedWordRewardDays.Add(Mathf.Max(1, day));
    }

    public static void BeginDialogue()
    {
        dialogueLockCount++;
    }

    public static void EndDialogue()
    {
        dialogueLockCount = Mathf.Max(0, dialogueLockCount - 1);
    }

    public static string GetPhoneCallsFolder(string baseFolder)
    {
        if (string.IsNullOrWhiteSpace(baseFolder))
        {
            return string.Empty;
        }

        return $"{baseFolder.TrimEnd('/')}/Day{CurrentDay}";
    }
}
