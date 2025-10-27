using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShapeUnlockSystem : MonoBehaviour
{
    private static HashSet<PlayerShape> unlockedShapes = new HashSet<PlayerShape>();

    /// <summary>
    /// 하나의 도형을 제외하고 모두 잠금 후, 해당 도형으로 강제 변신
    /// </summary>
    /// <param name="shape">해제할 도형</param>
    public static void Initialize(PlayerShape shape)
    {
        unlockedShapes.Clear();
        unlockedShapes.Add(shape); // 시작 도형은 해금
        PlayerManager.Instance.ForceToChangeShape(shape);
    }

    /// <summary>
    /// 특정 도형을 해금합니다.
    /// </summary>
    public static void Unlock(PlayerShape shape)
    {
        if (unlockedShapes.Add(shape))
        {
            Debug.Log($"해금: {shape}");
        }
    }

    /// <summary>
    /// 특정 모양을 다시 잠급니다.
    /// </summary>
    public static void Lock(PlayerShape shape)
    {
        if (unlockedShapes.Remove(shape))
        {
            Debug.Log($"잠금: {shape}");
            // 이미 해당 도형이면
            if(PlayerManager.Instance.CurrentShape == shape)
            {
                // 강제로 변경
                PlayerManager.Instance.ForceToChangeShape(unlockedShapes.FirstOrDefault());
            }
        }
    }
    /// <summary>
    /// 모든 도형을 해금합니다
    /// </summary>
    public static void UnLockAllShape()
    {
        unlockedShapes.Add(PlayerShape.Circle);
        unlockedShapes.Add(PlayerShape.Star);
        unlockedShapes.Add(PlayerShape.Square);
        unlockedShapes.Add(PlayerShape.Triangle);
    }

    public static void LockAllShape()
    {
        foreach (PlayerShape shape in System.Enum.GetValues(typeof(PlayerShape)))
        {
            if(unlockedShapes.Contains(shape))
            {
                Lock(shape);
            }
        }
    }

    /// <summary>
    /// 특정 모양이 해금되었는지 확인합니다.
    /// </summary>
    public static bool IsUnlocked(PlayerShape shape)
    {
        return unlockedShapes.Contains(shape);
    }
}
