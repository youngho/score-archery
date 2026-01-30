using UnityEngine;

/// <summary>
/// 99StageResult 씬 전용. 씬 시작 시 StageResultBGM을 루프 재생.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class StageResultBGMPlayer : MonoBehaviour
{
    [Tooltip("루프 재생할 BGM 클립 (StageResultBGM)")]
    public AudioClip bgmClip;

    private void Start()
    {
        var audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (bgmClip == null) return;

        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D (전역 BGM)
        audioSource.Play();
    }
}
