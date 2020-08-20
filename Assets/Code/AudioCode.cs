using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioCode : MonoBehaviour
{
    [SerializeField]
    [Range(0.0f, 1.0f)]
    float volume = 1.0f;
    AudioSource[] audioSources = new AudioSource[2];
    int toggle = 0;
    double endOfTake;

    [System.Serializable]
    public class AudioElement
    {
        [SerializeField]
        AudioClip[] takes;

        AudioClip nextTake;
        public AudioClip NextTake
        {
            get
            {
                AudioClip lastTake = nextTake;
                if (takes.Length > 1)
                {
                    while (nextTake == lastTake)
                    {
                        nextTake = takes[Random.Range(0, takes.Length)];
                    }
                }

                return nextTake;
            }
        }
    }

    [SerializeField]
    AudioElement audioElement;
    
    void Start()
    {
        if (audioElement == null)
        {
            Debug.LogError("Must provide an AudioElement to AudioCode.");
        }

        for (int i = 0; i < 2; i++)
        {
            GameObject source = new GameObject("Source");
            source.transform.parent = transform;

            audioSources[i] = source.AddComponent<AudioSource>();
            audioSources[i].playOnAwake = false;
            audioSources[i].priority = 0;
            audioSources[i].volume = volume;
        }

        endOfTake = AudioSettings.dspTime;
    }

    void Update()
    {
        if (AudioSettings.dspTime > endOfTake - 1.0)
        {
            audioSources[toggle].clip = audioElement.NextTake;
        }
    }

    public void PlayNextTake()
    {
        endOfTake = AudioSettings.dspTime + 0.05;
        audioSources[toggle].PlayScheduled(endOfTake);

        if (audioSources[toggle].clip != null)
        {
            double length = (double)audioSources[toggle].clip.samples / audioSources[toggle].clip.frequency;
            endOfTake += length;
        }
        else
        {
            endOfTake += 0.5f;
        }

        toggle = 1 - toggle;
    }
}
