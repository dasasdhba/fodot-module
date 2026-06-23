using Godot;

namespace Moon.Component;

[GlobalClass]
public partial class DeferredFollower2D : Node2D
{
    [Export]
    public float Rate { get; set; } = 15f;

    [Export(PropertyHint.NodePathValidTypes, "CanvasItem")]
    public NodePath TrackingNode { get; set; } = "..";

    [Export]
    public NodePath ReparentRoot { get; set; } = "../..";

    public enum DeferredFollower2DProcessCallback { Idle, Physics }

    [Export]
    public DeferredFollower2DProcessCallback ProcessCallback { get; set; }
        = DeferredFollower2DProcessCallback.Physics;
}
