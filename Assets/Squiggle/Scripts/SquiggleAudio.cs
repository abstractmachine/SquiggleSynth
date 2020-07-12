using UnityEngine;
using System.Collections;

public class SquiggleAudio : MonoBehaviour
{
    private int position = 0;
    private int samplerate = 44100;
    private float frequency = 440;
    private bool playing = false;
    
    public float[] spectrum = new float[256];
    public float[] logSpectrum = new float[256];

    public void Create(float frequency)
    {
        AudioClip myClip = AudioClip.Create("MySinusoid", samplerate * 2, 1, samplerate, true, OnAudioRead, OnAudioSetPosition);
        AudioSource source = GetComponent<AudioSource>();
        source.clip = myClip;
        source.Play();
        playing = true;
    }

     void Update()
    {
        if (!playing) return;

        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);

        for (int i = 1; i < spectrum.Length - 1; i++)
        {
            logSpectrum[i] = Mathf.Log(spectrum[i]);
        }
    }

    void OnAudioRead(float[] data)
    {
        int index = 0;
        while (index < data.Length)
        {
            data[index] = Mathf.Sin(2 * Mathf.PI * frequency * position / samplerate);
            position++;
            index++;
        }
    }

    void OnAudioSetPosition(int newPosition)
    {
        position = newPosition;
    }
}