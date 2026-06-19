using Godot;

namespace Moon.Physics;

[GlobalClass]
public partial class PlatformShape2D : CollisionShape2D, IPlatformShape
{
    [Export] 
    public bool CrashAsTravel { get; set; } = true;
}