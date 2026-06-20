using Fodot.Injection;
using Godot;

namespace Moon.Physics;

[GlobalClass, ChildOf("CollisionObject2D")]
public partial class MoonBlock2D : ParentComponent
{
    [Export(PropertyHint.None, "suffix:px/s")]
    public Vector2 ConstLinearVelocity { get ;set; }
    
    [Export(PropertyHint.None, "radians_as_degrees,suffix:\u00B0/s")]
    public float ConstAngularVelocity { get ;set; }
    
    [Export]
    public bool CrashBodies { get ;set; } = true;

    [Export]
    public Vector2[] InvalidSnaps { get ;set; } = [];
    
    [Export]
    public int MaxCollision { get ;set; } = 32;
}
