using Fodot;
using Godot;

namespace Fodot.Injection;

public partial class OwnerComponent : Node, IFScripts
{
    public string[] GetFScripts()
        => ["owner_component"];
}
