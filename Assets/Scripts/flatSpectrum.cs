using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class flatSpectrum : MonoBehaviour
{

    public LoopbackAudio Audio;
    // Use this for initialization
    public List<GameObject> lines = new List<GameObject>();
    public GameObject Line;
    LineRenderer liner;
    public Gradient gradient;

    float x, y, theta, data, phase;
    public float rStart, rEnd, r, w;
    public float phaseSpeed = 0.001f;
    public float pow;

    void create_line(int channel)
    {
        GameObject l = Instantiate(Line, transform.position, Quaternion.identity);
        l.transform.parent = transform;

        liner = l.GetComponent<LineRenderer>();
        liner.material = new Material(Shader.Find("Sprites/Default"));
        liner.widthMultiplier = 0.03f;
        liner.positionCount = Audio.SpectrumSize;
        lines.Add(l);

        for (int i = 0; i < Audio.SpectrumSize; i++)
        {
            liner.SetPosition(i, new Vector3(
                transform.position.x + i/(Audio.SpectrumSize-1), 
                transform.position.y, 
                transform.position.z
                )
            );
        }
    }
    
    void Start()
    {
        for (int i = 0; i < Audio.SpectrumSize; i++)
        {
            create_line(i);
        }
    }

    void Mode1()
    {

    }
    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < Audio.SpectrumSize; i++)
        {

            data = Mathf.Clamp(Audio.WeightedPostScaledSpectrumData[i] - Audio.WeightedPostScaledSpectrumData2[i], 0, 10);
            data /= 2f;

            liner = lines[i].GetComponent<LineRenderer>();
            liner.SetColors(
                gradient.Evaluate(Audio.WeightedPostScaledSpectrumData[0] / 10f),
                gradient.Evaluate(data)
            );

            for (int j = 0; j < Audio.SpectrumSize; j++)
            {
                liner.SetPosition(j, new Vector3(
                    transform.position.x + 3*((float)i / Audio.SpectrumSize),
                    transform.position.y + ((float)j / Audio.SpectrumSize)*data - 0.5f*data,
                    transform.position.z )
                );
            }
        }
    }
}
