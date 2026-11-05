using System;
using System.Windows;

namespace GlassDesk.UI.Controls;

public sealed record GuidedTourStep(FrameworkElement Target, string Title, string Description, GuideInteraction Interaction = GuideInteraction.Explore, Action? Prepare = null);
