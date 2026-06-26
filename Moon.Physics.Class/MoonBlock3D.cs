using System;
using Fodot.Injection;
using Godot;
using Godot.Warning;

namespace Moon.Physics;

[GlobalClass, ChildOf("CollisionObject3D")]
public partial class MoonBlock3D : ParentComponent
{
    [Export(PropertyHint.None, "suffix:m/s")]
    public Vector3 ConstLinearVelocity { get; set; }

    [Export(PropertyHint.None, "radians_as_degrees,suffix:\u00B0/s")]
    public Vector3 ConstAngularVelocity { get; set; }

    [Export]
    public bool CrashBodies { get; set; } = true;

    [Export]
    public Vector3[] InvalidSnapNormals { get; set; } = [];

    [Export]
    public int MaxCollision { get; set; } = 32;

    public Lazy<CollisionObject3D[]> LastPushed { get; set; } = new(() => []);
    public Lazy<CollisionObject3D[]> LastSnapped { get; set; } = new(() => []);

    public CollisionObject3D[] GetLastPushedBodies()
        => LastPushed.Value;

    public CollisionObject3D[] GetLastSnappedBodies()
        => LastSnapped.Value;
}
