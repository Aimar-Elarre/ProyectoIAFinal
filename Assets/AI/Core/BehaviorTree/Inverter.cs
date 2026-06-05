
public class Inverter : BTNode
{
    BTNode childNode;
    public BTNode Child => childNode;

    public Inverter(BTNode childNode)
    {
        if (childNode == null)
            throw new System.ArgumentNullException(nameof(childNode));

        this.childNode = childNode;
    }

    public override NodeStatus Tick()
    {
        var childStatus = childNode.Tick();
        NodeStatus result;

        switch (childStatus)
        {
            case NodeStatus.Success: result = NodeStatus.Failure; break;
            case NodeStatus.Failure: result = NodeStatus.Success; break;
            default:                 result = NodeStatus.Running; break;
        }

        LastStatus = result;
        return result;
    }
}
