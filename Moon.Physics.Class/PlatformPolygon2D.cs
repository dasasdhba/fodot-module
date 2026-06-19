using Godot;

namespace Moon.Physics;

[GlobalClass]
public partial class PlatformPolygon2D : CollisionPolygon2D, IPlatformShape
{
    [Export] 
    public bool CrashAsTravel { get; set; } = true;
}