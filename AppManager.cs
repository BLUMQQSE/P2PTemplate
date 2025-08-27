using Godot;
using System;
using System.Threading;

public partial class AppManager : Node
{
    public event Action<double> Update;
    public event Action<double> FixedUpdate;
    public event Action Update20;

    private static AppManager instance;
    public static AppManager Instance { get { return instance; } }

    public static SceneTree Tree { get { return instance.GetTree(); } }

    private bool mouseMode = false;
    TimeTracker track20;
    public bool MouseMode
    {
        get { return mouseMode; }
        set
        {
            mouseMode = value;
            MouseModeChanged?.Invoke();    
        }
    }
    public event Action MouseModeChanged;

    public AppManager()
    {
        if (instance == null)
            instance = this;
    }

    public override void _Ready()
    {
        base._Ready();
        track20 = new TimeTracker(1f / 20f, true);
        track20.TimeOut += TimeOut20;
        track20.Start();
    }

    public override void _Process(double delta)
    {
        Update?.Invoke(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        FixedUpdate?.Invoke(delta);
    }

    private void TimeOut20(TimeTracker tracker) { Update20?.Invoke(); }
}
