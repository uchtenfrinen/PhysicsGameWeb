namespace WPFPhisics1._1.Physics;

/// <summary>
/// Наклонная плоскость: угол в градусах (10–60).
/// </summary>
public class InclinedPlane
{
    private double _angleDegrees = 30;

    public double AngleDegrees
    {
        get => _angleDegrees;
        set => _angleDegrees = Math.Clamp(value, 10, 60);
    }

    public double AngleRadians => _angleDegrees * Math.PI / 180.0;
}
