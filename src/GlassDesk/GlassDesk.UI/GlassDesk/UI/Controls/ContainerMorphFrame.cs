using System.Windows;

namespace GlassDesk.UI.Controls;

public readonly record struct ContainerMorphFrame(Rect Bounds, double CornerRadius, double GlassStrength, double BlurRadius, double ContentOpacity, double ContentScale, double GlyphOpacity);
