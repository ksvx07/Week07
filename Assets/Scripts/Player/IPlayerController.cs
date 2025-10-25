using UnityEngine;

public interface IPlayerController
{
    int dashCount { get; set; }
    void OnEnableSetVelocity(float newVelX, float newVelY, int currentDashCount);
}
