using Godot;

namespace Moon.Component;

[GlobalClass]
public partial class StagePersistantUI : Marker2D
{
    [Export]
    public string Key { get; set; } = "";

    [Export]
    public PackedScene UiScene { get; set; }

    public Control UiNode { get; set; }

    [Export]
    public string TargetNode { get; set; } = "";

    [Export(PropertyHint.None, "suffix:px")]
    public Vector2 Offset { get; set; } = Vector2.Zero;

    [Export]
    public bool SyncRotation { get; set; } = false;

    [Export]
    public bool SyncScale { get; set; } = false;

    [Export]
    public bool SyncVisibility { get; set; } = true;

    [Export]
    public bool PhysicsProcess { get; set; } = true;
}
