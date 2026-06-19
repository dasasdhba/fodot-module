using Godot;

namespace Moon.Physics;

[GlobalClass]
public partial class PlatformPolygon3D : CollisionPolygon3D, IPlatformShape3D
{
    [ExportGroup("OneWayCollision")]
    [Export(PropertyHint.GroupEnable, "OneWayCollision")]
    public bool OneWayCollision { get ;set; } = true;

    [Export(PropertyHint.Range, "0,10,0.001,suffix:m")]
    public float OneWayCollisionMargin { get ;set; } = 0.04f;

    [Export] 
    public Vector3 OneWayCollisionDirection { get; set; } = Vector3.Down;
}