using UnityEngine;

public static class TweenExtension
{
    public static void TweenMove(this Transform transform, Vector3 target, float duration)
    {
        TweenManager.Instance.AddMoveTween(transform, target, duration, false);
    }

    public static void TweenRotate(this Transform transform, Vector3 target, float duration)
    {
        TweenManager.Instance.AddRotateTween(transform, target, duration, false);
    }
}
