
public class Selector : BTNode
{
    readonly BTNode[] _children;
    public System.Collections.Generic.IReadOnlyList<BTNode> Children => _children;

    public Selector(params BTNode[] children) => _children = children;

    public override NodeStatus Tick()
    {
        foreach (var child in _children)
        {
            var status = child.Tick();
            if (status != NodeStatus.Failure)
            {
                LastStatus = status; // Running o Success detienen la búsqueda
                return status;
            }
        }
        LastStatus = NodeStatus.Failure;
        return NodeStatus.Failure;
    }
}
