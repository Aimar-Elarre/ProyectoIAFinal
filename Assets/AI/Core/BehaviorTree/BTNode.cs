
public abstract class BTNode
{
    public string Name { get; set; }
    public NodeStatus? LastStatus { get; protected set; }

    public abstract NodeStatus Tick();
}
