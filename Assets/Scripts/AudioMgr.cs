using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioMgr : MonoBehaviour
{
    public static AudioMgr Instance { get; private set; }

    [SerializeField] AudioSource bgmSource;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        bgmSource = GetComponent<AudioSource>();
    }
    public void SetIsOpen(bool isOpen)
    {
        bgmSource.mute = !isOpen;
    }
    public void ChangeValue(float value)
    {
        bgmSource.volume = value;
    }
    public void SetBGM(AudioClip bgm)
    {
        bgmSource.clip = bgm;
    }
}
