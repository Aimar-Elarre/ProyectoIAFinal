using System;


public class BTAction : BTNode
{
    readonly Func<NodeStatus> _action;

    public BTAction(Func<NodeStatus> action, string name = null)
    {
        _action = action;
        Name = name;
    }

    public override NodeStatus Tick()
    {
        LastStatus = _action();
        return LastStatus.Value;
    }
}
