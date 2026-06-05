
public abstract class SensorBase
{
    protected readonly Blackboard _blackboard;

    protected SensorBase(Blackboard blackboard)
    {
        _blackboard = blackboard;
    }

    public abstract void Sense();
}
