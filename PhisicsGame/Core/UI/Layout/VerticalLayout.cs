using Microsoft.Xna.Framework;
using PhisicsGame.Core.UI.Elements;

namespace PhisicsGame.Core.UI.Layout;

public sealed class VerticalLayout
{
    public Rectangle Bounds { get; set; }
    public int Spacing { get; set; } = 12;

    public void Arrange(IReadOnlyList<UIElement> elements, int elementHeight)
    {
        var y = Bounds.Y;
        foreach (var element in elements)
        {
            element.Bounds = new Rectangle(Bounds.X, y, Bounds.Width, elementHeight);
            y += elementHeight + Spacing;
        }
    }
}
