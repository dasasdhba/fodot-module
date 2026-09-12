using Godot;

namespace Moon;

[GlobalClass]
public partial class PlatformPolygon2D : CollisionPolygon2D, IPlatformShape
{
    [Export] 
    public bool CrashAsTravel { get; set; } = true;
}
