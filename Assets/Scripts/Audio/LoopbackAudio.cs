using Assets.Scripts.Audio;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class LoopbackAudio : MonoBehaviour
{
    #region Constants

    private const int EnergyAverageCount = 100;

    #endregion

    #region Private Member Variables

    private RealtimeAudio _realtimeAudio;
    private List<float> _postScaleAverages = new List<float>();

    #endregion

    #region Public Properties

    public int SpectrumSize;
    public ScalingStrategy ScalingStrategy;
    public float[] SpectrumData;
    public float[] PostScaledSpectrumData;
    public float[] WeightedPostScaledSpectrumData;
    public float[] WeightedPostScaledSpectrumData2;

    public float WeightedPostScaledMax;
    public float PostScaledEnergy;
    public bool IsIdle;

    public float Average;
    public float WeightedAverage;
    public float WeightedAverage2;

    [Header("Recording Settings")]
    public float Weight = 0.25f;
    public float Weight2 = 0.25f;

    // stores data in time
    public Queue<float[]> audioQueue = new Queue<float[]>();
    public int bufferSize = 32;
    public float bufferRecordPeriod = 0.2f;
    float bufferTime = 0f;

    #endregion

    #region Startup / Shutdown

    public void Awake()
    {
        SpectrumData = new float[SpectrumSize];
        PostScaledSpectrumData = new float[SpectrumSize];
        WeightedPostScaledSpectrumData = new float[SpectrumSize];
        WeightedPostScaledSpectrumData2 = new float[SpectrumSize];

        // Used for post scaling
        float postScaleStep = 1.0f / SpectrumSize;

        // Setup loopback audio and start listening
        _realtimeAudio = new RealtimeAudio(SpectrumSize, ScalingStrategy,(spectrumData) =>
        {
            // Raw
            SpectrumData = spectrumData;

            Average = 0; WeightedAverage = 0; WeightedAverage2 =0;
            for (int i =0; i<SpectrumSize; i++)
            {
                Average += SpectrumData[i];
                WeightedAverage += WeightedPostScaledSpectrumData[i];
                WeightedAverage2 += WeightedPostScaledSpectrumData2[i];

            }
            Average /= SpectrumSize;
            WeightedAverage /= SpectrumSize;

            WeightedAverage2 /= SpectrumSize;
            // Post scaled for visualization
            float postScaledPoint = postScaleStep;

            bool isIdle = true;

            // Pass 1: Scaled. Scales progressively as moving up the spectrum
            for (int i = 0; i < SpectrumSize; i++)
            {
                // Don't scale low band, it's too useful
                if (i == 0)
                {
                    PostScaledSpectrumData[i] = SpectrumData[i];
                    WeightedPostScaledSpectrumData[i] = SpectrumData[i]*Weight + (1-Weight)*WeightedPostScaledSpectrumData[i];
                    WeightedPostScaledSpectrumData2[i] = SpectrumData[i] * Weight2 + (1 - Weight2) * WeightedPostScaledSpectrumData2[i];
                }
                else
                {
                    float postScaleValue = postScaledPoint * SpectrumData[i] * (RealtimeAudio.MaxAudioValue - (1.0f - postScaledPoint));
                    PostScaledSpectrumData[i] = Mathf.Clamp(postScaleValue, 0, RealtimeAudio.MaxAudioValue); // TODO: Can this be done better than a clamp?
                    WeightedPostScaledSpectrumData[i] = Mathf.Clamp(postScaleValue, 0, RealtimeAudio.MaxAudioValue) * Weight + (1 - Weight) * WeightedPostScaledSpectrumData[i];
                    WeightedPostScaledSpectrumData2[i] = Mathf.Clamp(postScaleValue, 0, RealtimeAudio.MaxAudioValue) * Weight2 + (1 - Weight2) * WeightedPostScaledSpectrumData2[i];
                }

                postScaledPoint += postScaleStep;

                if (spectrumData[i] > 0)
                {
                    isIdle = false;
                }
            }

            IsIdle = isIdle;
        });
        _realtimeAudio.StartListen();
    }

    public void Update()
    {
        // add data to buffer every X seconds 
        if ((Time.time - bufferTime) > bufferRecordPeriod)
        {
            float[] newData = new float[SpectrumSize];
            for(int i =0; i<SpectrumSize; i++)
            {
                //newData[i] = Mathf.Clamp(WeightedPostScaledSpectrumData[i] - WeightedPostScaledSpectrumData2[i],0,1f);
                newData[i] = WeightedPostScaledSpectrumData[i];
            }
            audioQueue.Enqueue(newData);
            if (audioQueue.Count > bufferSize)
            {
                audioQueue.Dequeue(); // remove first/oldest element 
            }
            bufferTime = Time.time;
        }
    }

    public float[,] getBuffer(int smooth=0)
    {
        float[,] data = new float[audioQueue.Count,SpectrumSize];

        int di = 0;

        foreach(float[] dat in audioQueue){


            if (smooth > 0)
            {
                float[] sdata = new float[SpectrumSize];

                /*smooth data */
                for (int i = 0; i < SpectrumSize; i++)
                {
                    float avg = 0;
                    for (int j = 0; j < smooth; j++)
                    {
                        avg += dat[Mathf.Clamp(i - j, 0, SpectrumSize - 1)] + dat[Mathf.Clamp(i + j, 0, SpectrumSize - 1)];
                    }
                    avg += dat[i];
                    sdata[i] = avg / (2f * smooth + 1);
                }

                // transfer data 
                for (int i = 0; i < SpectrumSize; i++)
                {
                    data[di,i] = sdata[i];
                }
            }
            else{
                // copy data
                for (int j = 0; j < SpectrumSize; j++)
                {
                    data[di, j] = dat[j];
                }
            }

            di++;
        }

        return data;
    }
    public void OnApplicationQuit()
    {
        _realtimeAudio.StopListen();
    }

    #endregion

}
