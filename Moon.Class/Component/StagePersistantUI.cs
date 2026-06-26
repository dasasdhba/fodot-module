using Godot;
using Godot.Warning;

namespace Moon.Component;

public interface IPersistantUI
{
    public void OnReturn();
    public void OnExit();
}

[GlobalClass]
public partial class StagePersistantUI : Marker2D
{
    [Export, NotNullString]
    public partial string Key { get; set; }

    [Export, NotNull]
    public partial PackedScene UiScene { get; set; }

    public Control UiNode { get; set; }

    [Export]
    public string TargetNode { get; set; } = "";

    [Export(PropertyHint.None, "suffix:px")]
    public Vector2 Offset { get; set; } = Vector2.Zero;

    [Export]
    public bool PhysicsProcess { get; set; } = true;
    
    [ExportGroup("Sync", "Sync")]
    [Export]
    public bool SyncRotation { get; set; } = false;

    [Export]
    public bool SyncScale { get; set; } = false;

    [Export]
    public bool SyncVisibility { get; set; } = true;
}
