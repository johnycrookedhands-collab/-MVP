using UnityEngine;

public static class GameAudioSettings
{
    private const string GameVolumeKey = "GameVolume";
    private const string VoiceVolumeKey = "VoiceVolume";

    public static float GameVolume { get; private set; } = 1f;
    public static float VoiceVolume { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        GameVolume = PlayerPrefs.GetFloat(GameVolumeKey, 1f);
        VoiceVolume = PlayerPrefs.GetFloat(VoiceVolumeKey, 1f);
        AudioListener.volume = GameVolume;
    }

    public static void SetGameVolume(float value)
    {
        GameVolume = Mathf.Clamp01(value);
        AudioListener.volume = GameVolume;
        PlayerPrefs.SetFloat(GameVolumeKey, GameVolume);
    }

    public static void SetVoiceVolume(float value)
    {
        VoiceVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VoiceVolumeKey, VoiceVolume);
    }
}
