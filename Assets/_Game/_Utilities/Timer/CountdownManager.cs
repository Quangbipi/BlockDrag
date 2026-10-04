using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Utilities.Timer
{
public class CountdownManager : MonoBehaviour
{
    public class CountdownData
    {
        public float remainingTime;
        public int lastSentSecond;
        public bool isPaused;

        public double endRealtime;
        public double pausedAt;

        public Action onComplete;
        public Action<int> onTick;
        public Action<float> onUpdate;
        public Coroutine coroutine;
    }

    public static CountdownManager Instance;

    private readonly Dictionary<string, CountdownData> countdowns = new();

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

    public void StartingCoundown(
        string id,
        float time,
        Action onComplete,
        Action<int> onTick,
        Action<float> onUpdate)
    {
        if (time <= 0)
        {
            onUpdate?.Invoke(0);
            onTick?.Invoke(0);
            onComplete?.Invoke();
            return;
        }

        if (countdowns.ContainsKey(id))
        {
            Debug.Log($"Countdown '{id}' đang chạy. Stop trước khi start lại.");
            return;
        }

        int startSecond = Mathf.CeilToInt(time);

        CountdownData data = new CountdownData
        {
            remainingTime = time,
            lastSentSecond = startSecond,
            isPaused = false,
            endRealtime = Time.realtimeSinceStartupAsDouble + time,
            onComplete = onComplete,
            onTick = onTick,
            onUpdate = onUpdate
        };

        countdowns[id] = data;

        onTick?.Invoke(startSecond);
        onUpdate?.Invoke(time);

        data.coroutine = StartCoroutine(CountdownTimer(id, data));
    }

    private IEnumerator CountdownTimer(string id, CountdownData data)
    {
        while (true)
        {
            if (!data.isPaused)
            {
                double remain = data.endRealtime - Time.realtimeSinceStartupAsDouble;

                data.remainingTime = Mathf.Max(0f, (float)remain);

                int secondLeft = Mathf.CeilToInt(data.remainingTime);

                if (secondLeft != data.lastSentSecond)
                {
                    data.lastSentSecond = secondLeft;
                    data.onTick?.Invoke(secondLeft);
                }

                data.onUpdate?.Invoke(data.remainingTime);

                if (data.remainingTime <= 0)
                    break;
            }

            yield return null;
        }

        countdowns.Remove(id);
        data.onComplete?.Invoke();
    }

    public void StopCountdown(string id)
    {
        if (!countdowns.TryGetValue(id, out CountdownData data))
            return;

        if (data.coroutine != null)
            StopCoroutine(data.coroutine);

        countdowns.Remove(id);
    }

    public void RestartCountdown(
        string id,
        float time,
        Action onComplete,
        Action<int> onTick,
        Action<float> onUpdate)
    {
        StopCountdown(id);
        StartingCoundown(id, time, onComplete, onTick, onUpdate);
    }

    public void PauseCountdown(string id)
    {
        if (!countdowns.TryGetValue(id, out CountdownData data))
            return;

        if (data.isPaused)
            return;

        data.isPaused = true;
        data.pausedAt = Time.realtimeSinceStartupAsDouble;
    }

    public void ResumeCountdown(string id)
    {
        if (!countdowns.TryGetValue(id, out CountdownData data))
            return;

        if (!data.isPaused)
            return;

        double pausedDuration = Time.realtimeSinceStartupAsDouble - data.pausedAt;

        data.endRealtime += pausedDuration;
        data.isPaused = false;
    }

    public bool IsRunning(string id)
    {
        return countdowns.ContainsKey(id);
    }

    public float GetRemainingTime(string id)
    {
        return countdowns.TryGetValue(id, out CountdownData data) ? data.remainingTime : 0f;
    }

    public void AddTime(string id, float addTime = 30f)
    {
        if (!countdowns.TryGetValue(id, out CountdownData data))
            return;

        data.endRealtime += addTime;
        data.remainingTime += addTime;
        data.lastSentSecond = Mathf.CeilToInt(data.remainingTime);
    }

    public void ReduceTime(string id, float reduceTime = 30f)
    {
        if (!countdowns.TryGetValue(id, out CountdownData data))
            return;

        data.endRealtime -= reduceTime;

        double remain = data.endRealtime - Time.realtimeSinceStartupAsDouble;
        data.remainingTime = Mathf.Max(0f, (float)remain);
        data.lastSentSecond = Mathf.CeilToInt(data.remainingTime);
    }

    public void ResetTime(string id, float time)
    {
        if (!countdowns.TryGetValue(id, out CountdownData data))
            return;

        data.endRealtime = Time.realtimeSinceStartupAsDouble + time;
        data.remainingTime = time;
        data.lastSentSecond = Mathf.CeilToInt(time);

        data.onTick?.Invoke(data.lastSentSecond);
        data.onUpdate?.Invoke(data.remainingTime);
    }
}}