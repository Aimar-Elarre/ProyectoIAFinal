using System;


public class Condition : BTNode
{
    readonly Func<bool> _condition;

    public Condition(Func<bool> condition, string name = null)
    {
        _condition = condition;
        Name = name;
    }

    public override NodeStatus Tick()
    {
        LastStatus = _condition() ? NodeStatus.Success : NodeStatus.Failure;
        return LastStatus.Value;
    }
}
