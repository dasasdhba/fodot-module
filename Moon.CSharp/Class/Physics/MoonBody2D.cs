using Fodot.Injection;
using Godot;

namespace Moon.Class;

[GlobalClass, ChildOf("CollisionObject2D")]
public partial class MoonBody2D : ParentComponent
{
    [Export]
    public Vector2[] SnapDirections { get ;set; } = [Vector2.Down];
}
