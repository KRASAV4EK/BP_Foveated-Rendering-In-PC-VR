using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    public float sampleDuration = 20f; // Average over last 20 seconds
    public float updateInterval = 0.5f; // How often update displayed FPS (seconds)

    private Queue<float> frameTimes = new Queue<float>();
    private float totalTime = 0f;
    private float timer = 0f;
    private float smoothedCurrentFPS = 0f;

    void Update()
    {
        float frameTime = Time.unscaledDeltaTime;

        // Save frame times
        frameTimes.Enqueue(frameTime);
        totalTime += frameTime;

        while (totalTime > sampleDuration && frameTimes.Count > 0)
        {
            totalTime -= frameTimes.Dequeue();
        }

        // Calculate FPS
        float instantFPS = 1.0f / frameTime;
        float averageFPS = frameTimes.Count > 0 ? frameTimes.Count / totalTime : 0f;

        // Smooth the instant FPS using Lerp
        smoothedCurrentFPS = Mathf.Lerp(smoothedCurrentFPS, instantFPS, Time.deltaTime * 5f);

        // Update text only every `updateInterval` seconds
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            if (fpsText != null)
            {
                fpsText.text = $"FPS: {smoothedCurrentFPS:F1}\nAvg (last {sampleDuration}s): {averageFPS:F1}";
            }
        }
    }
}
