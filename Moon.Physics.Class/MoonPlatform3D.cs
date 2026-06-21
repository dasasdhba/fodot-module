using Fodot.Injection;
using Godot;

namespace Moon.Physics;

[GlobalClass, ChildOf("CollisionObject3D")]
public partial class MoonPlatform3D : ParentComponent
{
    [Export] 
    public bool CrashAsTravel { get; set; } = true;
    
    [Export(PropertyHint.Range, "0,10,0.001,suffix:m")]
    public float Margin { get ;set; } = 0.04f;

    [Export] 
    public Vector3 Direction { get; set; } = Vector3.Down;
}
