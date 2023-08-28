using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public enum SoundName
{
    SelectedBlock = 0,
    RemovedBlock = 1,
    GetScore = 2,
    Boom = 3,
    GameOver = 4,
    BlockFallDown = 5,
}


[Serializable]
public class SoundData
{
    public SoundName soundName;
    public AudioClip audioClip;
}


public class SimpleSoundManager : MonoBehaviour
{
    private static SimpleSoundManager _instance;

    public static SimpleSoundManager Instance => _instance;
   

    [SerializeField] private List<SoundData> soundDatas = new List<SoundData>();
    [SerializeField] private AudioSource audioSource;

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
            return;
        }
        _instance = this;
    }

    public void PlaySound(SoundName soundName)
    {
        foreach (var soundData in soundDatas)
        {
            if (soundData.soundName == soundName)
            {
                Debug.Log($"Play SoundName : {soundName}");
                audioSource.enabled = true;
                audioSource.PlayOneShot(soundData.audioClip);
                break;
            }
        }
    }

    
}
