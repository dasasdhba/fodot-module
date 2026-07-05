using Fodot.Injection;
using Godot;
using Godot.Collections;
using Godot.Warning;
using Microsoft.FSharp.Collections;

namespace Moon.Physics;

[GlobalClass, ChildOf("CollisionObject3D")]
public partial class MoonBody3D : ParentComponent
{
    /// <summary>
    /// x, y, z, range(degrees)
    /// </summary>
    [Export]
    public Array<Vector4> Snaps { get; set; } = [];
    
    /// <summary>
    /// The safe margin used in the recovery stage of block pushing.
    /// Increase this for better stability, or omit it if
    /// you never need to deal with rotated blocks.
    /// </summary>
    [Export(PropertyHint.Range, "0,10,0.001,suffix:m")]
    public float SafeMargin { get; set; } = 0.004f;

    [Export]
    public int MaxCollision { get; set; } = 32;

    [Signal]
    public delegate void CrashedEventHandler();

    [Signal]
    public delegate void PushedEventHandler(CollisionObject3D col, Vector3 motion);

    [Signal]
    public delegate void SnappedEventHandler(CollisionObject3D col, Vector3 dir);

    public FSharpList<Vector3> SnapMotions { get; set; } = FSharpList<Vector3>.Empty;
    public Vector3 LastSnapMotion { get; set; }
    public Vector3 LastPushMotion { get; set; }
}
