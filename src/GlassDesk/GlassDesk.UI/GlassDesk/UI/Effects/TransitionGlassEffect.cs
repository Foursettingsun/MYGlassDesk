using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;

namespace GlassDesk.UI.Effects;

internal sealed class TransitionGlassEffect : ShaderEffect
{
	private static readonly PixelShader Shader = LoadShader();

	public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(TransitionGlassEffect), 0, SamplingMode.Bilinear);

	public static readonly DependencyProperty PanelProperty = DependencyProperty.Register("Panel", typeof(Point4D), typeof(TransitionGlassEffect), new UIPropertyMetadata(new Point4D(0.0, 0.0, 1.0, 1.0), ShaderEffect.PixelShaderConstantCallback(0)));

	public static readonly DependencyProperty MaterialProperty = DependencyProperty.Register("Material", typeof(Point4D), typeof(TransitionGlassEffect), new UIPropertyMetadata(new Point4D(1.0, 1.0, 0.0, 1.0), ShaderEffect.PixelShaderConstantCallback(1)));

	public static readonly DependencyProperty DispersionProperty = DependencyProperty.Register("Dispersion", typeof(Point4D), typeof(TransitionGlassEffect), new UIPropertyMetadata(new Point4D(1.0, 1.0, 1.0, 0.0), ShaderEffect.PixelShaderConstantCallback(2)));

	public static readonly DependencyProperty ProfileProperty = DependencyProperty.Register("Profile", typeof(Point4D), typeof(TransitionGlassEffect), new UIPropertyMetadata(new Point4D(0.0, 0.0, 0.0, 0.0), ShaderEffect.PixelShaderConstantCallback(3)));

	public Brush? Input
	{
		get
		{
			return (Brush)GetValue(InputProperty);
		}
		set
		{
			SetValue(InputProperty, value);
		}
	}

	public Point4D Panel
	{
		get
		{
			return (Point4D)GetValue(PanelProperty);
		}
		set
		{
			SetValue(PanelProperty, value);
		}
	}

	public Point4D Material
	{
		get
		{
			return (Point4D)GetValue(MaterialProperty);
		}
		set
		{
			SetValue(MaterialProperty, value);
		}
	}

	public Point4D Dispersion
	{
		get
		{
			return (Point4D)GetValue(DispersionProperty);
		}
		set
		{
			SetValue(DispersionProperty, value);
		}
	}

	public Point4D Profile
	{
		get
		{
			return (Point4D)GetValue(ProfileProperty);
		}
		set
		{
			SetValue(ProfileProperty, value);
		}
	}

	public TransitionGlassEffect()
	{
		PixelShader = Shader;
		UpdateShaderValue(InputProperty);
		UpdateShaderValue(PanelProperty);
		UpdateShaderValue(MaterialProperty);
		UpdateShaderValue(DispersionProperty);
		UpdateShaderValue(ProfileProperty);
	}

	private static PixelShader LoadShader()
	{
		using Stream streamSource = typeof(TransitionGlassEffect).Assembly.GetManifestResourceStream("GlassDesk.UI.Effects.TransitionGlass.ps") ?? throw new InvalidOperationException("Transition glass shader is missing.");
		PixelShader pixelShader = new PixelShader();
		pixelShader.ShaderRenderMode = ShaderRenderMode.HardwareOnly;
		pixelShader.SetStreamSource(streamSource);
		pixelShader.Freeze();
		return pixelShader;
	}
}
