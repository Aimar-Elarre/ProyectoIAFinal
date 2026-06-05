
using UnityEngine;

public class VisionSensor : SensorBase
{
    readonly Transform _origin;
    readonly Transform _player;
    readonly float     _range;
    readonly float     _hiddenRangeMultiplier;

    public VisionSensor(Transform origin, Transform player, float range, Blackboard blackboard, float hiddenRangeMultiplier = 0.35f)
        : base(blackboard)
    {
        _origin = origin;
        _player = player;
        _range  = range;
        _hiddenRangeMultiplier = Mathf.Clamp01(hiddenRangeMultiplier);
    }

    public override void Sense()
    {
        if (_player == null)
        {
            _blackboard.Set<bool>(BB.CanSeePlayer, false);
            return;
        }

        float distance = Vector3.Distance(_origin.position, _player.position);
        float effectiveRange = _range;

        if (_player.TryGetComponent<PlayerStealthController>(out var stealth) && stealth.IsHidden)
            effectiveRange *= _hiddenRangeMultiplier;

        bool canSee = distance < effectiveRange;
        _blackboard.Set<bool>(BB.CanSeePlayer, canSee);

        if (canSee)
        {
            _blackboard.Set<Vector3>(BB.LastKnownPosition, _player.position);
            _blackboard.Set<bool>(BB.HasClue, true);
        }
    }
}
