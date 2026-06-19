using Godot;
using Moon.Class;

namespace Moon.Component;

[GlobalClass]
public partial class NodeDestroyer : Node
{
    [Export]
    public NodePath Target { get ;set; } = "..";

    public void Destroy()
    {
        GetNodeOrNull(Target).TryQueueFree();
    }
}