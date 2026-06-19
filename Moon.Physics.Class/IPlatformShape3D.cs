using Godot;

namespace Moon.Physics;

public interface IPlatformShape3D
{
    public bool OneWayCollision { get ;set; }
    public float OneWayCollisionMargin { get ;set; }
    public Vector3 OneWayCollisionDirection { get; set; }
    
    public Transform3D GetGlobalTransform();
}