using Fodot.Injection;
using Godot;

namespace Moon.Physics;

[GlobalClass, ChildOf("CollisionObject2D")]
public partial class MoonPlatform2D : ParentComponent
{
    [Export] 
    public bool CrashAsTravel { get; set; } = true;
    
    [Export(PropertyHint.Range, "0,128,0.1,suffix:px")]
    public float Margin { get ;set; } = 1f;
    
    [Export] 
    public Vector2 Direction { get; set; } = Vector2.Down;
}