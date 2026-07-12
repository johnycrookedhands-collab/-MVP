using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DailyWordReward
{
    [Min(1)] public int day = 1;
    public string[] words;
}

[CreateAssetMenu(menuName = "Game/Word Reward Schedule")]
public class DailyWordRewardSchedule : ScriptableObject
{
    [SerializeField] private DailyWordReward[] rewardsByDay =
    {
        new DailyWordReward { day = 1, words = new[] { "тихо" } },
        new DailyWordReward { day = 2, words = new[] { "адрес" } },
        new DailyWordReward { day = 3, words = new[] { "слушай" } },
        new DailyWordReward { day = 4, words = new[] { "жди" } },
        new DailyWordReward { day = 5, words = new[] { "помогу" } }
    };

    public IReadOnlyList<string> GetWordsForDay(int day)
    {
        if (rewardsByDay == null)
        {
            return Array.Empty<string>();
        }

        foreach (DailyWordReward reward in rewardsByDay)
        {
            if (reward != null && reward.day == day && reward.words != null)
            {
                return reward.words;
            }
        }

        return Array.Empty<string>();
    }
}
