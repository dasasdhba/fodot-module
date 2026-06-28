using Godot;
using Godot.Collections;

namespace Moon.Component;

[GlobalClass, Tool]
public partial class StageUI : Control
{
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

#if DEBUG
    public override void _EnterTree()
    {
        if (Engine.IsEditorHint())
        {
            NotifyPropertyListChanged();
        }
    }

    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);

        var name = property["name"].AsStringName();
        var parent = GetParent();
        if (parent is CanvasItem)
        {
            return;
        }

        if (parent is Node3D &&
            (name == PropertyName.SyncRotation || name == PropertyName.SyncScale))
        {
            property["usage"] = (uint)PropertyUsageFlags.None;
        }

        if (parent is not Node3D &&
            (name == PropertyName.Offset ||
             name == PropertyName.SyncRotation ||
             name == PropertyName.SyncScale ||
             name == PropertyName.SyncVisibility))
        {
            property["usage"] = (uint)PropertyUsageFlags.None;
        }
    }
#endif
}
