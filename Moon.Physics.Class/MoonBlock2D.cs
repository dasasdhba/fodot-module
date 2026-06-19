using Fodot.Injection;
using Godot;
using Microsoft.FSharp.Core;

namespace Moon.Physics;

[GlobalClass, ChildOf("CollisionObject2D")]
public partial class MoonBlock2D : ParentComponent
{
    [Export]
    public bool CrashBodies { get ;set; } = true;

    [Export]
    public Vector2[] InvalidSnaps { get ;set; } = [];
    
    [Export]
    public int MaxCollision { get ;set; } = 32;
}
