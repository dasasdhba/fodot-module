using Godot;
using Moon;

namespace Moon;

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
