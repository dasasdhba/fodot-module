using Godot;
using Moon.Interface;

namespace Moon.Class;

[GlobalClass]
public partial class PlatformPolygon3D : CollisionPolygon3D, IPlatformShape
{
    [ExportGroup("OneWayCollision")]
    [Export(PropertyHint.GroupEnable, "OneWayCollision")]
    public bool OneWayCollision { get ;set; } = true;

    [Export(PropertyHint.Range, "0,10,0.001,suffix:m")]
    public float OneWayCollisionMargin { get ;set; } = 0.04f;
}