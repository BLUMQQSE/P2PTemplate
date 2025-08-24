using Godot;
using System;

public partial class Main : Node
{
    [Export]
    CanvasLayer canvas;
    [Export]
    Button join;

    public override void _Ready()
    {
        base._Ready();
        join.Pressed += OnJoin;
    }
    private void OnJoin()
    {
        canvas.Visible = false;
        LocalNetwork.Instance.CreateClient();
    }

}
