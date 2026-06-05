
public class StickySelector : BTNode
{
    readonly BTNode[] _children;
    int _runningIndex = -1;

    public System.Collections.Generic.IReadOnlyList<BTNode> Children => _children;

    public StickySelector(params BTNode[] children) => _children = children;

    public override NodeStatus Tick()
    {
        int start = (_runningIndex >= 0) ? _runningIndex : 0;

        for (int i = start; i < _children.Length; i++)
        {
            var status = _children[i].Tick();
            if (status == NodeStatus.Running)
            {
                _runningIndex = i;
                LastStatus = NodeStatus.Running;
                return NodeStatus.Running;
            }
            if (status == NodeStatus.Success)
            {
                _runningIndex = -1;
                LastStatus = NodeStatus.Success;
                return NodeStatus.Success;
            }
        }

        _runningIndex = -1;
        LastStatus = NodeStatus.Failure;
        return NodeStatus.Failure;
    }

    public void ResetCursor() => _runningIndex = -1;
}
