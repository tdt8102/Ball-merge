using UnityEngine;

/// <summary>Detects a horizontal swipe (mouse or finger) so pages with several panels can be flipped. Call Poll() once per frame.</summary>
public static class Swipe
{
    static float _startX;
    static bool _down;

    /// <summary>+1 after a swipe to the left (next page), -1 after a swipe to the right (previous page), otherwise 0.</summary>
    public static int Poll()
    {
        if (Input.GetMouseButtonDown(0)) { _startX = Input.mousePosition.x; _down = true; }
        if (_down && Input.GetMouseButtonUp(0))
        {
            _down = false;
            float dx = Input.mousePosition.x - _startX;
            if (Mathf.Abs(dx) > Screen.width * 0.2f) return dx < 0f ? 1 : -1;
        }
        return 0;
    }
}
