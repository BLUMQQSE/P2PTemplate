using Godot;
using System;
using System.Reflection.Emit;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 8000.0f;

    public override void _Ready()
    {
        base._Ready();
        SetMultiplayerAuthority(int.Parse(Name));
        if(IsMultiplayerAuthority())
            GetNode<Camera2D>("Camera2D").MakeCurrent();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!IsMultiplayerAuthority())
            return;
        if (Input.IsActionJustPressed("Space"))
        {
            if (GetParent() == GetTree().CurrentScene)
            {
                Network.Instance.ReparentNode(this, GetTree().CurrentScene.GetNode("Game"));
            }
            else
            {
                Network.Instance.ReparentNode(this, GetTree().CurrentScene);
            }
        }
        if (Input.IsActionJustPressed("Shift"))
        {
            Rpc("rpc", "wowzers");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority())
            return;
        Vector2 direction = Vector2.Zero;
        if (Input.IsActionPressed("A"))
            direction.X = -1;
        else if (Input.IsActionPressed("D"))
            direction.X = 1;
        if (Input.IsActionPressed("W"))
            direction.Y = -1;
        else if (Input.IsActionPressed("S"))
            direction.Y = 1;

        Velocity = direction.Normalized() * Speed * (float)delta;
        
        MoveAndSlide();
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    void rpc(string val)
    {
        GD.Print(Multiplayer.GetUniqueId() + " says "+val + " on " +Multiplayer.GetRemoteSenderId() + " orders");
        GD.Print();
    }
}
