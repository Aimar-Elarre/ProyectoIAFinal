
using System;

public class HealthSensor : SensorBase
{
    readonly Func<float> _getHealth;
    readonly float       _maxHealth;

    public HealthSensor(Func<float> getHealth, float maxHealth, Blackboard blackboard)
        : base(blackboard)
    {
        _getHealth = getHealth;
        _maxHealth = maxHealth;
    }

    public override void Sense()
    {
        float health = _getHealth();
        _blackboard.Set<float>(BB.Health, health);
        _blackboard.Set<bool>(BB.LowHealth, health < _maxHealth * 0.5f);
    }
}
