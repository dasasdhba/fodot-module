using Godot;

namespace Fodot.Injection;

public partial class OwnerComponent : Node
{
    public string[] _GetFScripts()
        => ["owner_component"];
}