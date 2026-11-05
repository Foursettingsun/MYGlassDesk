using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using GlassDesk.UI.Effects;

namespace GlassDesk.UI.Controls;

internal sealed class TransitionGlassLayer : FrameworkElement
{
	private BitmapSource? _backdrop;

	private TransitionGlassEffect? _lens;

	private readonly DrawingGroup _drawing = new DrawingGroup();

	private bool _sleeping;

	private bool _frosted;

	private Rect _panel;

	private double _radius;

	private double _strength;

	public bool UsesGpuLens => Effect is TransitionGlassEffect;

	public TransitionGlassLayer()
	{
		IsHitTestVisible = false;
		Opacity = 0.0;
		RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
	}

	public void SetBackdrop(BitmapSource? backdrop)
	{
		if (backdrop != _backdrop)
		{
			_backdrop = backdrop;
			if (backdrop == null)
			{
				_drawing.Children.Clear();
			}
			InvalidateVisual();
		}
	}

	public void SetPerformanceSleeping(bool sleeping)
	{
		_sleeping = sleeping;
		if (sleeping)
		{
			SetBackdrop(null);
			_drawing.Children.Clear();
			Effect = null;
			_lens = null;
		}
	}

	public void SetPanel(Rect panel, Rect backdropBounds, double radius, double strength, bool launchpadOptics = false, bool frosted = false)
	{
		if (backdropBounds.Width <= 0.0 || backdropBounds.Height <= 0.0)
		{
			return;
		}
		_panel = new Rect(panel.X - backdropBounds.X, panel.Y - backdropBounds.Y, panel.Width, panel.Height);
		_radius = radius;
		_strength = strength;
		_frosted = frosted;
		if (_sleeping)
		{
			return;
		}
		if (frosted || BackdropRecovery.RequiresBasicRendering || !RenderCapability.IsPixelShaderVersionSupported(3, 0))
		{
			Effect = null;
			InvalidateVisual();
			return;
		}
		if (_lens == null)
		{
			_lens = new TransitionGlassEffect();
		}
		Effect = _lens;
		_lens.Panel = new Point4D((panel.X - backdropBounds.X) / backdropBounds.Width, (panel.Y - backdropBounds.Y) / backdropBounds.Height, panel.Width / backdropBounds.Width, panel.Height / backdropBounds.Height);
		_lens.Material = new Point4D(backdropBounds.Width, backdropBounds.Height, radius, strength);
		_lens.Profile = new Point4D(launchpadOptics ? 1 : 0, 0.0, 0.0, 0.0);
		LiquidGlassDispersionParameters liquidGlassDispersionParameters = LiquidGlassDebugState.DispersionParameters.Normalize();
		_lens.Dispersion = new Point4D(liquidGlassDispersionParameters.DispersionStrength / 0.003, liquidGlassDispersionParameters.InnerDispersionGain / 0.25, liquidGlassDispersionParameters.OuterDispersionGain / 0.4, 0.0);
	}

	protected override void OnRender(DrawingContext dc)
	{
		base.OnRender(dc);
		using (DrawingContext dc2 = _drawing.Open())
		{
			DrawBackdrop(dc2);
		}
		dc.DrawDrawing(_drawing);
	}

	private void DrawBackdrop(DrawingContext dc)
	{
		if (_frosted && _panel.Width > 0.0 && _panel.Height > 0.0)
		{
			dc.PushClip(SquircleGlassGeometry.Create(_panel, _radius));
		}
		if (_backdrop != null)
		{
			dc.DrawImage(_backdrop, new Rect(RenderSize));
		}
		if (_backdrop != null && !UsesGpuLens && _panel.Width > 0.0 && _panel.Height > 0.0)
		{
			LinearGradientBrush brush = new LinearGradientBrush(new GradientStopCollection
			{
				new GradientStop(Color.FromArgb(100, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0),
				new GradientStop(Color.FromArgb(20, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.4),
				new GradientStop(Color.FromArgb(75, 12, 17, 27), 1.0)
			}, new Point(0.0, 0.0), new Point(1.0, 1.0));
			dc.DrawGeometry(null, new Pen(brush, 1.5 * _strength), SquircleGlassGeometry.Create(_panel, _radius));
		}
		if (_frosted && _panel.Width > 0.0 && _panel.Height > 0.0)
		{
			dc.Pop();
		}
	}
}
