using Fodot.Injection;
using Godot;
using Microsoft.FSharp.Collections;

namespace Moon.Physics;

[GlobalClass, ChildOf("CollisionObject2D")]
public partial class MoonBody2D : ParentComponent
{
    /// <summary>
    /// x, y, range(degrees)
    /// </summary>
    [Export]
    public Vector3[] Snaps { get ;set; } = [new(0f, 1f, 45f)];
    
    [Export(PropertyHint.Range, "0,128,0.001,suffix:px")]
    public float SafeMargin { get ;set; } = 0.08f;
    
    [Export]
    public int MaxCollision { get ;set; } = 32;
    
    [Signal]
    public delegate void CrashedEventHandler();
    
    [Signal]
    public delegate void PushedEventHandler(CollisionObject2D col, Vector2 motion);
    
    [Signal]
    public delegate void SnappedEventHandler(CollisionObject2D col, Vector2 dir);
    
    public FSharpList<Vector2> SnapMotions { get; set; } = [];
    public Vector2 LastSnapMotion { get ;set; }
    public Vector2 LastPushMotion { get ;set; }
}
