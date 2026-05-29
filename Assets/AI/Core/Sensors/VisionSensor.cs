// ============================================================
//  EJERCICIO: BLACKBOARD — Parte 2a (Ampliación)
// ============================================================
//
//  VisionSensor simula los "ojos" del enemigo.
//  Cada frame comprueba si el jugador está en rango y
//  escribe el resultado en la pizarra.
//
//  CLAVES QUE DEBES ESCRIBIR:
//    BB.CanSeePlayer      (bool)    → true si el jugador está en rango
//    BB.LastKnownPosition (Vector3) → posición del jugador cuando es visible
//    BB.HasClue           (bool)    → true en cuanto se ha visto al jugador
//
//  REGLA IMPORTANTE:
//    Cuando el jugador SALE del rango, pon BB.CanSeePlayer = false,
//    pero NO borres BB.LastKnownPosition ni BB.HasClue.
//    Esos datos son la "memoria" que usará el estado Investigate.
//
//  REFLEXIÓN:
//    Compara esta solución con ChaseState.OnExit() de la FSM:
//
//      // ChaseState.cs (Enemigo_FSM):
//      public override void OnExit()
//      {
//          enemy.lastKnownPlayerPos = enemy.jugador.position;
//      }
//
//    Con la pizarra + sensor, ya no hace falta capturar la posición
//    en OnExit(): el sensor la actualiza continuamente y la conserva
//    automáticamente al perder la visión.
//
// ============================================================

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

    // TODO [PARTE 2a]: Implementa Sense().
    //
    // 1. Comprueba si _player está a menos de _range unidades de _origin.
    //    Usa Vector3.Distance(_origin.position, _player.position).
    //
    // 2. Si SÍ está en rango:
    //      _blackboard.Set<bool>(BB.CanSeePlayer, true);
    //      _blackboard.Set<Vector3>(BB.LastKnownPosition, _player.position);
    //      _blackboard.Set<bool>(BB.HasClue, true);
    //
    // 3. Si NO está en rango:
    //      _blackboard.Set<bool>(BB.CanSeePlayer, false);
    //      // ← No toques LastKnownPosition ni HasClue.
    //
    // 4. Guarda el caso nulo: si _player == null, escribe false y sal.
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
