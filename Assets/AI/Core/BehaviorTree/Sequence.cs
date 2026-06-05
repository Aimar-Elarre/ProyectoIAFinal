
public class Sequence : BTNode
{
    readonly BTNode[] _children;
    public System.Collections.Generic.IReadOnlyList<BTNode> Children => _children;

    public Sequence(params BTNode[] children) => _children = children;

    public override NodeStatus Tick()
    {
        foreach (var child in _children)
        {
            var status = child.Tick();
            if (status != NodeStatus.Success)
            {
                LastStatus = status; // Running o Failure detienen la secuencia
                return status;
            }
        }
        LastStatus = NodeStatus.Success;
        return NodeStatus.Success;
    }
}
