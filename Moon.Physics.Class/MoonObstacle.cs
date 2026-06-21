using Fodot.Injection;
using Godot;

namespace Moon.Physics;

[GlobalClass, ChildOf("CollisionObject2D, CollisionObject3D")]
public partial class MoonObstacle : ParentComponent
{
    [Export]
    public bool IgnoreWhenInside { get ;set; }

    [Signal]
    public delegate void IgnoredByEventHandler(GodotObject body);
}