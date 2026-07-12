using UnityEngine;

public class PhoneCallResultSystem : MonoBehaviour
{
    [Range(0, 100)]
    public int callerMood = 50;

    [Range(0, 100)]
    public int sectInfluence = 50;

    public void ApplyAnswer(PhoneAnswerVariant answer)
    {
        callerMood += answer.callerMoodDelta;
        sectInfluence += answer.sectInfluenceDelta;
        ClampValues();

        Debug.Log("Настроение клиента: " + callerMood);
        Debug.Log("Влияние секты: " + sectInfluence);
    }

    public void ApplyInvalidAnswer(PhoneCallData callData)
    {
        if (callData == null)
        {
            return;
        }

        sectInfluence += callData.invalidAnswerSectInfluenceDelta;
        ClampValues();

        Debug.Log("Влияние секты: " + sectInfluence);
    }

    private void ClampValues()
    {
        callerMood = Mathf.Clamp(callerMood, 0, 100);
        sectInfluence = Mathf.Clamp(sectInfluence, 0, 100);
    }
}
