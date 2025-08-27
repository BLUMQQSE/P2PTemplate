using System;
using System.Diagnostics;
using Godot;
[GlobalClass]
public partial class TimeTracker : Resource
{
    public event Action<TimeTracker> TimeOut;
    [Export]
    public bool Loop { get; set; } = false;
    /// <summary>
    /// Interval to wait in seconds
    /// </summary>
    [Export]
    public double WaitTime { get; set; } = 0;

    [Export]
    private double timeAddon;
    [Export]
    private double currentTime = 0;
    [Export]
    private bool isRunning = false;

    public bool IsRunning { get { return isRunning; } }
    public double ElapsedSeconds { get { return currentTime + timeAddon; } }
    public double ElapsedMilliseconds { get { return ElapsedSeconds * 1000; } }

    public TimeTracker() 
    {
        AppManager.Instance.Update += Update;
    }

    public TimeTracker(double waitTime, bool loop = false) : this()
    {
        WaitTime = waitTime;
        Loop = loop;
        AppManager.Instance.Update += Update;
    }
    
    public void Dispose()
    {
        AppManager.Instance.Update -= Update;
    }

    private void Update(double delta)
    {
        if (!isRunning)
            return;

        currentTime += delta;

        if (WaitTime == 0)
            return;

        if (ElapsedSeconds >= WaitTime)
        {
            TimeOut?.Invoke(this);
            if (Loop && IsRunning) // double check still running
            {
                double dif = ElapsedSeconds - WaitTime;
                Restart();
                timeAddon = dif;
            }
            else
            {
                Reset();
            }
        }
    }

    public void Restart()
    {
        timeAddon = 0;
        currentTime = 0;
        Start();
    }

    public void Reset()
    {
        timeAddon = 0;
        currentTime = 0;
        Stop();
    }
    public void Start()
    {
        isRunning = true;
    }

    public void Stop()
    {
        isRunning = false;
    }

    public bool IsFinished()
    {
        if (!isRunning)
            return false;

        if (ElapsedSeconds >= WaitTime)
            return true;

        return false;
    }

}

