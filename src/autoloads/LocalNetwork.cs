using Godot;
using System;

public partial class LocalNetwork : Node
{
    private static LocalNetwork instance;
    public static LocalNetwork Instance => instance;
    public LocalNetwork()
    {
        instance = this;
    }

    public Vector2I largeSize = new Vector2I(16 * 73, 9 * 73);
    public Vector2I largeServer = new Vector2I(1380, 35);
    public Vector2I largeClient = new Vector2I(1380, 725);


    public Vector2I smallSize = new Vector2I(16 * 45, 9 * 45);
    public Vector2I smallServer = new Vector2I(1825, 35);
    public Vector2I smallClient = new Vector2I(1825, 475);
    
    public override void _Ready()
    {
        base._Ready();

        GetWindow().FocusEntered += OnFocusEntered;
        GetWindow().FocusExited += OnFocusExited;
        if (OS.GetCmdlineArgs().Length == 2)
        {
            CreateHost();
            GetTree().CurrentScene.GetNode<CanvasLayer>("CanvasLayer").Visible = false;

            Network.Instance.AddNode(ResourceManager.Instance.GetResourceByName<PackedScene>("Game.tscn").Instantiate(), GetTree().CurrentScene);
            Node player = ResourceManager.Instance.GetResourceByName<PackedScene>("Player.tscn").Instantiate();
            player.Name = "1";
            Network.Instance.AddNode(player, GetTree().CurrentScene);   
        }
        else
        {
            GetWindow().Position = largeClient;
            GetWindow().Size = largeSize;
            GetWindow().Title = "InActive";
        }

        Network.Instance.PeerConnected += OnPeerConnect;
    }

    private void OnPeerConnect(long id)
    {
        if (Network.Instance.NetworkState == NetworkStateEnum.Host)
        {
            Node player = ResourceManager.Instance.GetResourceByName<PackedScene>("Player.tscn").Instantiate();
            player.Name = id.ToString();
            Network.Instance.AddNode(player, GetTree().CurrentScene);
        }
    }

    public void CreateHost()
    {
        GetWindow().Position = largeServer;
        GetWindow().Size = largeSize;

        GetWindow().Title = "Host";
        GetWindow().Transient = true;

        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        peer.TransferMode = MultiplayerPeer.TransferModeEnum.Reliable;
        var err = peer.CreateServer(5555);
        if (err == Error.Ok)
        {
            Multiplayer.MultiplayerPeer = peer;
            Network.Instance.SetNetworkState((int)NetworkStateEnum.Host);

            Node n = new Node();
            n.Name = "HOST";
            GetTree().Root.CallDeferred("add_child", n, true);
        }

        GetWindow().GrabFocus();
    }

    public void CreateClient()
    {
        GetWindow().Title = "Client";
        GetWindow().Transient = true;

       
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        peer.TransferMode = MultiplayerPeer.TransferModeEnum.Reliable;
        var err = peer.CreateClient("localhost", 5555);
        if (err == Error.Ok)
        {
            Multiplayer.MultiplayerPeer = peer; 
            Node n = new Node();
            n.Name = "CLIENT";
            GetTree().Root.CallDeferred("add_child", n, true);

            Network.Instance.SetNetworkState((int)NetworkStateEnum.Client);
            GetTree().CurrentScene.SetMeta("Type", "Client");
        }
    }

    private void OnFocusExited()
    {
        string t = "Host";
        if (Network.Instance.NetworkState == NetworkStateEnum.Client)
            t = "Client";
        else if (Network.Instance.NetworkState == NetworkStateEnum.Inactive)
            t = "InActive";
        GetWindow().Title = t;
    }

    private void OnFocusEntered()
    {
        string t = "Host";
        if (Network.Instance.NetworkState == NetworkStateEnum.Client)
            t = "Client";
        else if (Network.Instance.NetworkState == NetworkStateEnum.Inactive)
            t = "InActive";
        GetWindow().Title = t + ": FOCUS";
    }
}
