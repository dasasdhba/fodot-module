using Godot;

namespace Moon.Physics;

public interface IPlatformShape
{
    public bool CrashAsTravel { get ;set; }
}

// this one is used for 3d one way collision
// as it's not a built-in feature in godot

public interface IPlatformShape3D
{
    public bool OneWayCollision { get ;set; }
    public float OneWayCollisionMargin { get ;set; }
    public Vector3 OneWayCollisionDirection { get; set; }
    
    public Transform3D GetGlobalTransform();
}