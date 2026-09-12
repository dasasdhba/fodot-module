using Godot;

namespace Moon;

public partial class OwnerComponent : Node, IFScripts
{
    public string[] GetFScripts()
        => ["owner_component"];
}
