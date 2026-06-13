using Godot;

namespace Fodot.Injection;

public partial class ParentComponent : Node
{
    public static string[] _GetFScripts()
        => ["component"];
}