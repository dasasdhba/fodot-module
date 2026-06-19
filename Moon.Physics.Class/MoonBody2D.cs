using Fodot.Injection;
using Godot;

namespace Moon.Physics;

[GlobalClass, ChildOf("CollisionObject2D")]
public partial class MoonBody2D : ParentComponent
{
    [Export]
    public Vector2[] Snaps { get ;set; } = [Vector2.Down];
    
    [Signal]
    public delegate void CrashedEventHandler();
    
    [Signal]
    public delegate void SnappedEventHandler(CollisionObject2D col, Vector2 dir);
}
