namespace Moon.Script

open Fodot
open Moon.Component
open Moon.Library

[<FScript(typeof<NodeToggle>)>]
type private NodeToggleScript(node : NodeToggle) =
    
    let toggle =
        SmoothToggle(node, node.EditorFlag, node.EditorTime, node.EditorPhysics)
    
    do
        toggle.Value <- node.EditorValue
        toggle.Paused <- node.EditorPaused
        toggle.FullyOn.Add node.EmitSignalFullyOn
        toggle.FullyOff.Add node.EmitSignalFullyOff
        toggle.Bind node.OnValueUpdated

        node.Toggle <- toggle

