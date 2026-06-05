
using UnityEngine;

public static class SteeringBehaviors
{

    public static Vector3 Seek(Vector3 agentPos, Vector3 targetPos, float maxSpeed)
    {
        Vector3 dir = targetPos - agentPos;
        if (dir.sqrMagnitude < 0.0001f) return Vector3.zero;
        return dir.normalized * maxSpeed;
    }


    public static Vector3 Flee(Vector3 agentPos, Vector3 targetPos, float maxSpeed)
    {
        Vector3 dir = agentPos - targetPos;
        if (dir.sqrMagnitude < 0.0001f) return Vector3.zero;
        return dir.normalized * maxSpeed;
    }


    public static Vector3 Arrive(
        Vector3 agentPos,
        Vector3 targetPos,
        float maxSpeed,
        float slowRadius = 3f,
        float targetRadius = 0.2f)
    {
        Vector3 toTarget = targetPos - agentPos;
        float distance = toTarget.magnitude;

        if (distance <= targetRadius)
            return Vector3.zero;

        float desiredSpeed = distance <= slowRadius
            ? maxSpeed * (distance / slowRadius)
            : maxSpeed;

        return toTarget.normalized * desiredSpeed;
    }


    public static Vector3 Pursue(
        Vector3 agentPos,
        Vector3 targetPos,
        Vector3 targetVelocity,
        float maxSpeed,
        float predictionTime = 0.5f)
    {
        Vector3 futurePos = targetPos + targetVelocity * predictionTime;
        return Seek(agentPos, futurePos, maxSpeed);
    }


    public static Vector3 Evade(
        Vector3 agentPos,
        Vector3 targetPos,
        Vector3 targetVelocity,
        float maxSpeed,
        float predictionTime = 0.5f)
    {
        Vector3 futurePos = targetPos + targetVelocity * predictionTime;
        return Flee(agentPos, futurePos, maxSpeed);
    }
}
