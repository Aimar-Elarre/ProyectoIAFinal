
public class ConditionalSequence : BTNode
{
    readonly BTNode _condition;
    readonly BTNode[] _actions;
    int _runningActionIndex;

    public BTNode Condition => _condition;
    public System.Collections.Generic.IReadOnlyList<BTNode> Actions => _actions;

    public ConditionalSequence(BTNode condition, params BTNode[] actions)
    {
        _condition = condition;
        _actions = actions;
        _runningActionIndex = 0;
    }

    public override NodeStatus Tick()
    {
        var condStatus = _condition.Tick();
        if (condStatus != NodeStatus.Success)
        {
            _runningActionIndex = 0;
            LastStatus = NodeStatus.Failure;
            return NodeStatus.Failure; // ← abort: la condición dejó de cumplirse
        }

        for (int i = _runningActionIndex; i < _actions.Length; i++)
        {
            var actionStatus = _actions[i].Tick();
            if (actionStatus == NodeStatus.Running)
            {
                _runningActionIndex = i;
                LastStatus = NodeStatus.Running;
                return NodeStatus.Running;
            }
            if (actionStatus == NodeStatus.Failure)
            {
                _runningActionIndex = 0;
                LastStatus = NodeStatus.Failure;
                return NodeStatus.Failure;
            }
        }

        _runningActionIndex = 0;
        LastStatus = NodeStatus.Success;
        return NodeStatus.Success;
    }
}
