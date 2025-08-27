using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
public enum NetworkStateEnum
{
    Inactive,
    Host,
    Client
}

public partial class Network : Node
{
    protected static Network instance;
    public static Network Instance => instance;
    public Network()
    {
        instance = this;
    }
    #region readonly strings
    public static StringName PlayerGroup = new StringName("Player");

    protected static readonly StringName NetworkIgnoreGroup = new StringName("NetworkIgnoreGroup");
    protected static readonly StringName NetworkIgnoreChildrenGroup = new StringName("NetworkIgnoreChildrenGroup");
    protected static readonly string _Parent = "Parent";
    protected static readonly string _Node = "Node";
    protected static readonly string _Nodes = "Nodes";

    protected static readonly string _PackedScene = "PackedScene";
    protected static readonly string _PackedSceneChild = "PackedSceneChild";
    protected static readonly string _FilePath = "FilePath";

    protected static readonly string _Add = "A";
    protected static readonly string _Caller = "CL";
    protected static readonly string _MethodName = "MN";
    protected static readonly string _Params = "PS";
    protected static readonly string _Value = "V";

    protected static readonly string _Name = "Name";
    protected static readonly string _Type = "Type";
    protected static readonly string _DerivedType = "DT";
    protected static readonly string _Position = "Pos";
    protected static readonly string _Rotation = "Rot";
    protected static readonly string _Scale = "Scale";
    protected static readonly string _Size = "SZ";
    protected static readonly string _ZIndex = "ZI";
    protected static readonly string _ZIsRelative = "ZIR";
    protected static readonly string _YSortEnabled = "YSE";
    protected static readonly string _Group = "Group";
    protected static readonly string _Children = "Children";
    protected static readonly string _ISerializeData = "SerializedData";
    protected static readonly string _VisibilityLayer = "VisibilityLayer";
    protected static readonly string _Visible = "Visible";

    protected static readonly string _CollisionLayer = "CL";
    protected static readonly string _CollisionMask = "CM";
    protected static readonly string _SerializeData = "SD";
    protected static readonly string _Data = "Data";

    protected static readonly string _Radius = "Rad";
    protected static readonly string _Shape = "SHP";
    protected static readonly string _Height = "HGHT";
    #endregion
    public event Action<long> PeerConnected;
    public event Action<long> PeerDisconnected;
    public HashSet<long> Users { get; set; } = new HashSet<long>();
    public NetworkStateEnum NetworkState { get; private set; }
    public void SetNetworkState(int state)
    {
        NetworkState = (NetworkStateEnum)state;
    }
    public int UserId => Multiplayer.GetUniqueId();
    public Player LocalPlayer { get; set; }
    public List<Player> AllPlayers { get; set; } = new List<Player>();

    TimeTracker networkUpdateTimer = new TimeTracker();
    public event Action NetworkUpdate;
    public override void _Ready()
    {
        base._Ready();


        networkUpdateTimer.Loop = true;
        networkUpdateTimer.WaitTime = (1f / 20f); // 20 times per second
        networkUpdateTimer.TimeOut += OnNetworkUpdate;
        networkUpdateTimer.Start();
        Multiplayer.PeerConnected += OnPeerConnect;
        Multiplayer.PeerDisconnected += OnPeerDisconnect;
    }

    private void OnNetworkUpdate(TimeTracker tracker)
    {
        NetworkUpdate?.Invoke();
    }

    private void OnPeerConnect(long id)
    {
        Users.Add(id);
        GD.Print("peer connected " + id);
        if (NetworkState == NetworkStateEnum.Host)
        {
            // send full server state to client id
            JsonValue data = new JsonValue();

            foreach (var child in GetTree().CurrentScene.GetChildren())
            {
                data[_Nodes].Append(CollectAllNodeData(GetTree().CurrentScene, GetTree().CurrentScene, child, true));
            }

            RpcId(id, MethodName.HandleAddEverything, data.ToString());
        }

        PeerConnected?.Invoke(id);
    }

    private void OnPeerDisconnect(long id)
    {
        Users.Remove(id);
        if (NetworkState == NetworkStateEnum.Client)
        {
            NetworkState = NetworkStateEnum.Inactive;
        }
        PeerDisconnected?.Invoke(id);
    }

    public void AddNode(Node node, Node parent)
    {
        parent.AddChild(node, true);
        JsonValue data = new JsonValue();

        if (node.SceneFilePath != string.Empty)
        {
            data[_FilePath].Set(node.SceneFilePath);
            data[_PackedScene].Set(true);
            data[_Data].Set(GetPrimitiveDataFromNode(node));
            data[_Name].Set(node.Name);
            data[_Parent].Set(parent.GetPath());
        }
        else
        {
            // need to traverse the node being added and any potential node children it has and send all the data to peers
            data = CollectAllNodeData(node.Owner, parent, node, false);
            data[_Parent].Set(parent.GetPath());
        }

        Rpc(MethodName.HandleAddNode, data.ToString());
    }
    public void RemoveNode(Node node)
    {
        node.QueueFree();

        JsonValue data = new JsonValue();
        data[_Node].Set(node.GetPath());

        Rpc(MethodName.HandleRemoveNode, data.ToString());
    }
    public void ReparentNode(Node node, Node newParent)
    {
        JsonValue data = new JsonValue();
        data[_Node].Set(node.GetPath());
        data[_Parent].Set(newParent.GetPath());

        node.Reparent(newParent);
        node.SetMultiplayerAuthority();
        Rpc(MethodName.HandleReparentNode, data.ToString());
    }

    protected JsonValue CollectAllNodeData(Node currentOwner, Node parent, Node nodeProcessing, bool addingEverything)
    {
        if (nodeProcessing.IsInGroup(NetworkIgnoreGroup))
            return null;
        JsonValue nodeData = new JsonValue();

        if (nodeProcessing.SceneFilePath != string.Empty) // new PackedScene
        {
            nodeData[_PackedScene].Set(true);
            nodeData[_FilePath].Set(nodeProcessing.SceneFilePath);
            nodeData[_Name].Set(nodeProcessing.Name);
            nodeData[_Data].Set(GetPrimitiveDataFromNode(nodeProcessing));


            if (!nodeProcessing.IsInGroup(NetworkIgnoreChildrenGroup))
            {
                foreach (var child in nodeProcessing.GetChildren())
                    nodeData[_Children].Append(CollectAllNodeData(nodeProcessing, nodeProcessing, child, addingEverything));
            }
        }
        else if (nodeProcessing.Owner == currentOwner && currentOwner != null) // child node existing in packedScene
        {
            nodeData[_PackedSceneChild].Set(true);
            nodeData[_Name].Set(nodeProcessing.Name);
            if (addingEverything)
                nodeData[_Data].Set(GetPrimitiveDataFromNode(nodeProcessing));
            if (!nodeProcessing.IsInGroup(NetworkIgnoreChildrenGroup))
            {
                foreach (var child in nodeProcessing.GetChildren())
                    nodeData[_Children].Append(CollectAllNodeData(currentOwner, nodeProcessing, child, addingEverything));
            }
        }
        else // unique node not from packed scene
        {
            nodeData[_Node].Set(GetNodeType(nodeProcessing));
            nodeData[_Data].Set(GetPrimitiveDataFromNode(nodeProcessing));
            nodeData[_Name].Set(nodeProcessing.Name);
            if (!nodeProcessing.IsInGroup(NetworkIgnoreChildrenGroup))
            {
                foreach (var child in nodeProcessing.GetChildren())
                    nodeData[_Children].Append(CollectAllNodeData(currentOwner, nodeProcessing, child, addingEverything));
            }
        }

        return nodeData;
    }

    /// <param name="addingEverything">If true, we will need to send all primitive data, if false, can skip that for all children of
    /// a packed scene.</param>
    /// <returns>A valid node if should be added, null if not necessary to add</returns>
    protected Node SetAllNodeData(Node currentOwner, Node parent, JsonValue data, bool addingEverything)
    {
        if (data[_PackedScene].AsBool() == true)
        {
            Node node = ResourceManager.Instance.GetResourceByPath<PackedScene>(data[_FilePath].AsString()).Instantiate();
            currentOwner = node;
            node.Name = data[_Name].AsString();
            SetPrimitiveDataOnNode(data[_Data], node);

            foreach (var childData in data[_Children].Array)
            {
                Node child = SetAllNodeData(currentOwner, node, childData, addingEverything);
                if (child != null)
                    node.AddChild(child, true);
            }
            if (parent.HasNode(node.Name.ToString()))
                return null;

            return node;
        }
        else if (data[_PackedSceneChild].AsBool() == true)
        {

            Node node = parent.GetNode(data[_Name].AsString());
            node.Name = data[_Name].AsString();

            if (addingEverything) // only worth adding all this data if a new player is joining and needs to know EVERYTHING
                // If its a node added mid game, the children won't have been modified yet on any peer
                SetPrimitiveDataOnNode(data[_Data], node);

            foreach (var childData in data[_Children].Array)
            {
                SetAllNodeData(currentOwner, node, childData, addingEverything);
            }
            return null;
        }
        else
        {
            Node node = CreateNode(data[_Node]);
            node.Name = data[_Name].AsString();
            SetPrimitiveDataOnNode(data[_Data], node);

            foreach (var childData in data[_Children].Array)
            {
                Node child = SetAllNodeData(currentOwner, node, childData, addingEverything);
                if (child != null)
                    node.AddChild(child, true);
            }
            if (parent.HasNode(node.Name.ToString()))
            {
                return null;
            }
            return node;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void HandleAddEverything(string dataString)
    {
        JsonValue data = JsonValue.Parse(dataString);
        foreach (var nodeData in data[_Nodes].Array)
        {
            Node n = SetAllNodeData(GetTree().CurrentScene, GetTree().CurrentScene, nodeData, true);
            if (n != null)
                GetTree().CurrentScene.AddChild(n, true);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void HandleAddNode(string dataString)
    {
        JsonValue data = JsonValue.Parse(dataString);
        Node parent = GetNode(data[_Parent].AsString());
        
        Node n = SetAllNodeData(null, parent, data, false);
       
        parent.AddChild(n, true);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void HandleReparentNode(string dataString)
    {
        JsonValue data = JsonValue.Parse(dataString);
        Node parent = GetNode(data[_Parent].AsString());
        Node node = GetNode(data[_Node].AsString());
        node.Reparent(parent);

        node.SetMultiplayerAuthority();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    protected void HandleRemoveNode(string dataString)
    {
        JsonValue data = JsonValue.Parse(dataString);
        GetNode(data[_Node].AsString()).QueueFree();
    }

    protected JsonValue GetNodeType(Node node)
    {
        JsonValue data = new JsonValue();
        data[_Type].Set(node.GetType().ToString());
        data[_DerivedType].Set(node.GetClass());
        return data;
    }

    protected Node CreateNode(JsonValue data)
    {
        Node node = (Node)ClassDB.Instantiate(data[_DerivedType].AsString());

        ulong nodeID = node.GetInstanceId();
        if (!data[_Type].AsString().Equals(data[_DerivedType].AsString()))
        {
            node.SetScript(ResourceManager.Instance.GetResourceByName<Script>(data[_Type].AsString() + ".cs"));
        }

        return InstanceFromId(nodeID) as Node;
    }

    protected JsonValue GetPrimitiveDataFromNode(Node node)
    {
        JsonValue data = new JsonValue();
        if (node is Node2D n2)
        {
            data[_Visible].Set(n2.Visible);
            data[_ZIsRelative].Set(n2.ZAsRelative);
            data[_YSortEnabled].Set(n2.YSortEnabled);
            data[_ZIndex].Set(n2.ZIndex);

            data[_Position].Set(n2.Position);
            data[_Rotation].Set(n2.Rotation);
            data[_Scale].Set(n2.Scale);

            data[_VisibilityLayer].Set(n2.VisibilityLayer);
        }
        if(node is Node3D n3)
        {
            data[_Visible].Set(n3.Visible);
            data[_Position].Set(n3.Position);
            data[_Rotation].Set(n3.Rotation);
            data[_Scale].Set(n3.Scale);
        }
        if (node is CollisionObject2D c2)
        {
            data[_CollisionMask].Set(c2.CollisionMask);
            data[_CollisionLayer].Set(c2.CollisionLayer);
        }
        if (node is CollisionObject3D c3)
        {
            data[_CollisionLayer].Set(c3.CollisionLayer);
            data[_CollisionMask].Set(c3.CollisionMask);
        }
        if (node is CollisionShape2D col2)
        {
            if (col2.Shape is CircleShape2D cir)
            {
                data[_Shape].Set("Cir");
                data[_Radius].Set(cir.Radius);
            }
            else if (col2.Shape is CapsuleShape2D cap)
            {
                data[_Shape].Set("Cap");
                data[_Radius].Set(cap.Radius);
                data[_Height].Set(cap.Height);
            }
            else if (col2.Shape is RectangleShape2D rect)
            {
                data[_Shape].Set("Rec");
                data[_Size].Set(rect.Size);
            }

        }
        if(node is CollisionShape3D col3)
        {
            if (col3.Shape is SphereShape3D sph)
            {
                data[_Shape].Set("Sphere");
                data[_Radius].Set(sph.Radius);
            }
            else if (col3.Shape is CapsuleShape3D cap)
            {
                data[_Shape].Set("Cap");
                data[_Radius].Set(cap.Radius);
                data[_Height].Set(cap.Height);
            }
            else if (col3.Shape is BoxShape3D rect)
            {
                data[_Shape].Set("Box");
                data[_Size].Set(rect.Size);
            }
            else if(col3.Shape is CylinderShape3D cil)
            {
                data[_Shape].Set("Cyl");
                data[_Height].Set(cil.Height);
                data[_Radius].Set(cil.Radius);
            }
        }

        foreach (string group in node.GetGroups())
            data[_Group].Append(group);

        return data;
    }

    protected void SetPrimitiveDataOnNode(JsonValue data, Node node)
    {
        if (node is Node2D n2)
        {
            n2.Visible = data[_Visible].AsBool();
            n2.Position = data[_Position].AsVector2();
            n2.Rotation = data[_Rotation].AsFloat();
            n2.Scale = data[_Scale].AsVector2();

            n2.ZIndex = data[_ZIndex].AsInt();
            n2.ZAsRelative = data[_ZIsRelative].AsBool();
            n2.YSortEnabled = data[_YSortEnabled].AsBool();
            n2.VisibilityLayer = data[_VisibilityLayer].AsUInt();
        }
        if(node is Node3D n3)
        {
            n3.Visible = data[_Visible].AsBool();
            n3.Position = data[_Position].AsVector3();
            n3.Rotation = data[_Rotation].AsVector3();
            n3.Scale = data[_Scale].AsVector3();
        }
        if (node is CollisionObject2D c2)
        {
            c2.CollisionMask = data[_CollisionMask].AsUInt();
            c2.CollisionLayer = data[_CollisionLayer].AsUInt();
        }
        if(node is CollisionObject3D c3)
        {
            c3.CollisionLayer = data[_CollisionLayer].AsUInt();
            c3.CollisionMask = data[_CollisionMask].AsUInt();
        }
        if (node is CollisionShape2D cs2)
        {
            if (data[_Shape].AsString() == "Cir")
            {
                CircleShape2D x = new CircleShape2D();
                x.Radius = data[_Radius].AsFloat();
                cs2.Shape = x;
            }
            else if (data[_Shape].AsString() == "Cap")
            {
                CapsuleShape2D x = new CapsuleShape2D();
                x.Radius = data[_Radius].AsFloat();
                x.Height = data[_Height].AsFloat();
                cs2.Shape = x;
            }
            else if (data[_Shape].AsString() == "Rect")
            {
                RectangleShape2D x = new RectangleShape2D();
                x.Size = data[_Size].AsVector2();
                cs2.Shape = x;
            }
            else
            {
                GD.Print("Error trying to create collision shape on client, shape unknown : " + data.ToString());
                throw new Exception();
            }
        }
        if(node is CollisionShape3D cs3)
        {
            if (data[_Shape].AsString() == "Sphere")
            {
                SphereShape3D x = new SphereShape3D();
                x.Radius = data[_Radius].AsFloat();
                cs3.Shape = x;
            }
            else if (data[_Shape].AsString() == "Cap")
            {
                CapsuleShape3D x = new CapsuleShape3D();
                x.Radius = data[_Radius].AsFloat();
                x.Height = data[_Height].AsFloat();
                cs3.Shape = x;
            }
            else if (data[_Shape].AsString() == "Box")
            {
                BoxShape3D x = new BoxShape3D();
                x.Size = data[_Size].AsVector3();
                cs3.Shape = x;
            }
            else if (data[_Shape].AsString() == "Cyl")
            {
                CylinderShape3D cyl = new CylinderShape3D();
                cyl.Radius = data[_Radius].AsFloat();
                cyl.Height = data[_Height].AsFloat();
                cs3.Shape = cyl;
            }
            else
            {
                GD.Print("Error trying to create collision shape on client, shape unknown : " + data.ToString());
                throw new Exception();
            }
        }

        foreach (JsonValue group in data[_Group].Array)
            node.AddToGroup(group.AsString());
    }

    public static string RemoveNamespace(string name)
    {
        int index = name.RFind(".");
        if (index < 0)
            return name;
        else
            return name.Substring(index + 1, name.Length - (index + 1));
    }
}

public static class NetworkExtensions
{
    /// <summary>
    /// Should be used in place of SetMultiplayerAuthority(int id)
    /// </summary>
    public static void SetMultiplayerAuthority(this Node node)
    {
        var player = node.FindParentOfType<Player>();
        if (!player.IsValid())
        {
            node.SetMultiplayerAuthority(1);
        }
        node.SetMultiplayerAuthority(int.Parse(player.Name));
    }
}
