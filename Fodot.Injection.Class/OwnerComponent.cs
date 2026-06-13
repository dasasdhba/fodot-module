using Godot;

namespace Fodot.Injection;

public partial class OwnerComponent : Node
{
    public static string[] _GetFScripts()
        => ["owner_component"];
}