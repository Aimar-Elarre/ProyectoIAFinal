
using UnityEngine;
using UnityEngine.InputSystem;

public class SoundSensor : SensorBase
{
    readonly Transform _origin;
    readonly Transform _player;
    readonly float     _hearingRange;

    public SoundSensor(Transform origin, Transform player, float hearingRange, Blackboard blackboard)
        : base(blackboard)
    {
        _origin       = origin;
        _player       = player;
        _hearingRange = hearingRange;
    }

    public override void Sense()
    {
        if (_player == null)
        {
            _blackboard.Set<bool>(BB.HeardNoise, false);
            return;
        }

        float distance = Vector3.Distance(_origin.position, _player.position);
        bool isMakingNoise = Keyboard.current != null && Keyboard.current.eKey.isPressed;
        bool heardNoise = isMakingNoise && distance <= _hearingRange;

        if (heardNoise)
        {
            _blackboard.Set<bool>(BB.HeardNoise, true);
            _blackboard.Set<Vector3>(BB.NoisePosition, _player.position);
            _blackboard.Set<Vector3>(BB.LastKnownPosition, _player.position);
            _blackboard.Set<bool>(BB.HasClue, true);
        }
        else
        {
            _blackboard.Set<bool>(BB.HeardNoise, false);
        }
    }
}
