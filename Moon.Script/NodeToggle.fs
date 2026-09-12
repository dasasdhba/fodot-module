namespace Moon

[<FScript(typeof<NodeToggle>)>]
type private NodeToggleScript(node: NodeToggle) =

    let toggle =
        SmoothToggle(node, node.EditorFlag, node.EditorTime, node.EditorPhysics)

    do
        toggle.Bind node.OnValueUpdated
        toggle.FullyOn.Add node.EmitSignalFullyOn
        toggle.FullyOff.Add node.EmitSignalFullyOff

        node.Toggle <- toggle

        node.add_Ready (fun _ ->
            toggle.Value <- node.EditorValue
            toggle.Paused <- node.EditorPaused
        )
