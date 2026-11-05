using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace GlassDesk.UI.Controls;

public sealed class GuidedTour : IDisposable
{
	private sealed class CoachAdorner : Adorner
	{
		private readonly Canvas _canvas = new Canvas();

		private readonly Border _ring;

		private readonly Border _card;

		private readonly TextBlock _progress;

		private readonly TextBlock _heading;

		private readonly TextBlock _body;

		private readonly TextBlock _feedback;

		private readonly Button _previous;

		private readonly Button _next;

		private FrameworkElement? _target;

		private bool _interactive;

		private readonly VisualCollection _visuals;

		protected override int VisualChildrenCount => 1;

		internal CoachAdorner(FrameworkElement root, Action previous, Action next, Action close)
			: base(root)
		{
			_visuals = new VisualCollection(this) { _canvas };
			SolidColorBrush borderBrush = new SolidColorBrush(Color.FromRgb(77, 150, byte.MaxValue));
			_ring = new Border
			{
				BorderBrush = borderBrush,
				BorderThickness = new Thickness(2.0),
				CornerRadius = new CornerRadius(8.0),
				IsHitTestVisible = false,
				Effect = new DropShadowEffect
				{
					Color = Color.FromRgb(77, 150, byte.MaxValue),
					BlurRadius = 12.0,
					ShadowDepth = 0.0,
					Opacity = 0.35
				}
			};
			_canvas.Children.Add(_ring);
			Grid grid = new Grid
			{
				RowDefinitions = 
				{
					new RowDefinition
					{
						Height = GridLength.Auto
					},
					new RowDefinition
					{
						Height = new GridLength(1.0, GridUnitType.Star)
					},
					new RowDefinition
					{
						Height = GridLength.Auto
					}
				}
			};
			StackPanel stackPanel = new StackPanel();
			_progress = Text(11.0, 0.66);
			_heading = Text(19.0, 1.0);
			_heading.FontWeight = FontWeights.SemiBold;
			_heading.Margin = new Thickness(0.0, 7.0, 0.0, 10.0);
			_body = Text(13.0, 0.9);
			_body.LineHeight = 21.0;
			_feedback = Text(12.0, 0.8);
			_feedback.Margin = new Thickness(0.0, 12.0, 0.0, 12.0);
			AutomationProperties.SetAutomationId(_feedback, "guide-feedback");
			stackPanel.Children.Add(_progress);
			stackPanel.Children.Add(_heading);
			grid.Children.Add(stackPanel);
			StackPanel content = new StackPanel
			{
				Children = 
				{
					(UIElement)_body,
					(UIElement)_feedback
				}
			};
			ScrollViewer element = new ScrollViewer
			{
				Content = content,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
			};
			Grid.SetRow(element, 1);
			grid.Children.Add(element);
			WrapPanel wrapPanel = new WrapPanel
			{
				HorizontalAlignment = HorizontalAlignment.Right
			};
			Button element2 = Button("返回教学", "guide-close", close);
			_previous = Button("上一步", "guide-previous", previous);
			_next = Button("下一步", "guide-next", next);
			_next.Background = new SolidColorBrush(Color.FromRgb(43, 111, 225));
			_next.Foreground = Brushes.White;
			wrapPanel.Children.Add(element2);
			wrapPanel.Children.Add(_previous);
			wrapPanel.Children.Add(_next);
			Grid.SetRow(wrapPanel, 2);
			grid.Children.Add(wrapPanel);
			_card = new Border
			{
				Padding = new Thickness(18.0),
				CornerRadius = new CornerRadius(18.0),
				Background = new SolidColorBrush(Color.FromRgb(20, 32, 53)),
				BorderBrush = new SolidColorBrush(Color.FromRgb(79, 113, 161)),
				BorderThickness = new Thickness(1.0),
				Child = grid
			};
			TextElement.SetForeground(_card, Brushes.White);
			AutomationProperties.SetAutomationId(_card, "guide-card");
			AutomationProperties.SetName(_card, "操作引导");
			_canvas.Children.Add(_card);
		}

		private static TextBlock Text(double size, double opacity)
		{
			return new TextBlock
			{
				FontSize = size,
				Opacity = opacity,
				TextWrapping = TextWrapping.Wrap
			};
		}

		private static Button Button(string label, string id, Action action)
		{
			Button button = new Button
			{
				Content = label,
				Padding = new Thickness(9.0, 7.0, 9.0, 7.0),
				Margin = new Thickness(4.0, 3.0, 0.0, 0.0),
				Foreground = Brushes.White,
				Background = new SolidColorBrush(Color.FromRgb(40, 57, 82)),
				BorderBrush = Brushes.Transparent,
				Cursor = Cursors.Hand,
				Template = RoundedButtonTemplate()
			};
			AutomationProperties.SetAutomationId(button, id);
			AutomationProperties.SetName(button, label);
			button.Click += (object _, RoutedEventArgs _) =>
			{
				action();
			};
			return button;
		}

		private static ControlTemplate RoundedButtonTemplate()
		{
			FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(Border), "Chrome");
			frameworkElementFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8.0));
			frameworkElementFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1.0));
			(DependencyProperty, string)[] array = new (DependencyProperty, string)[3]
			{
				(Border.BackgroundProperty, "Background"),
				(Border.BorderBrushProperty, "BorderBrush"),
				(Border.PaddingProperty, "Padding")
			};
			for (int i = 0; i < array.Length; i++)
			{
				var (dp, path) = array[i];
				frameworkElementFactory.SetBinding(dp, new Binding(path)
				{
					RelativeSource = RelativeSource.TemplatedParent
				});
			}
			FrameworkElementFactory frameworkElementFactory2 = new FrameworkElementFactory(typeof(ContentPresenter));
			frameworkElementFactory2.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
			frameworkElementFactory2.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
			frameworkElementFactory2.SetBinding(ContentPresenter.ContentProperty, new Binding("Content")
			{
				RelativeSource = RelativeSource.TemplatedParent
			});
			frameworkElementFactory.AppendChild(frameworkElementFactory2);
			ControlTemplate controlTemplate = new ControlTemplate(typeof(Button))
			{
				VisualTree = frameworkElementFactory
			};
			Trigger trigger = new Trigger
			{
				Property = UIElement.IsMouseOverProperty,
				Value = true
			};
			trigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.82, "Chrome"));
			controlTemplate.Triggers.Add(trigger);
			Trigger trigger2 = new Trigger
			{
				Property = UIElement.IsKeyboardFocusedProperty,
				Value = true
			};
			trigger2.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.LightSkyBlue, "Chrome"));
			controlTemplate.Triggers.Add(trigger2);
			return controlTemplate;
		}

		internal void SetStep(GuidedTourStep step, int index, int count, bool observed)
		{
			_target = step.Target;
			_interactive = step.Interaction != GuideInteraction.Explore;
			_progress.Text = $"跟着做 · {index + 1:00} / {count:00}";
			_heading.Text = step.Title;
			_body.Text = step.Description;
			_previous.IsEnabled = index > 0;
			_next.Content = ((index + 1 == count) ? "完成实操" : "下一步");
			ShowFeedback(observed);
			InvalidateMeasure();
		}

		internal void ShowFeedback(bool observed)
		{
			TextBlock feedback = _feedback;
			string text;
			if (observed)
			{
				text = "✓ 已完成这一步的操作";
			}
			else
			{
				text = (_interactive ? "可以亲手试一试，也可以直接下一步。" : "了解后点下一步；无需修改当前设置。");
			}
			feedback.Text = text;
		}

		protected override Visual GetVisualChild(int index)
		{
			return _visuals[index];
		}

		protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
		{
			return null;
		}

		protected override Size MeasureOverride(Size constraint)
		{
			Size renderSize = AdornedElement.RenderSize;
			_card.Width = Math.Min(324.0, Math.Max(160.0, renderSize.Width - 20.0));
			_card.MaxHeight = CardHeightLimit(renderSize);
			_canvas.Measure(renderSize);
			return renderSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			_canvas.Arrange(new Rect(finalSize));
			Position();
			return finalSize;
		}

		internal void Position()
		{
			if (_target == null || !_target.IsVisible || AdornedElement.RenderSize.Width < 1.0)
			{
				_ring.Visibility = Visibility.Collapsed;
				return;
			}
			if (!AdornedElement.IsAncestorOf(_target))
			{
				_ring.Visibility = Visibility.Collapsed;
				return;
			}
			Rect area = new Rect(AdornedElement.RenderSize);
			double num = CardHeightLimit(area.Size);
			if (Math.Abs(_card.MaxHeight - num) > 0.2)
			{
				_card.MaxHeight = num;
				InvalidateMeasure();
				return;
			}
			Rect target = _target.TransformToAncestor(AdornedElement).TransformBounds(new Rect(_target.RenderSize));
			target.Inflate(5.0, 5.0);
			target.Intersect(area);
			if (target.IsEmpty)
			{
				_ring.Visibility = Visibility.Collapsed;
				return;
			}
			_ring.Visibility = Visibility.Visible;
			Set(_ring, target.Left, target.Top);
			SetSize(_ring, target.Width, target.Height);
			double width = _card.DesiredSize.Width;
			double height = _card.DesiredSize.Height;
			if (!(width < 1.0) && !(height < 1.0))
			{
				Point point = (from point2 in new Point[6]
					{
						new Point(target.Right + 16.0, target.Top),
						new Point(target.Left - width - 16.0, target.Top),
						new Point(target.Left, target.Bottom + 16.0),
						new Point(target.Left, target.Top - height - 16.0),
						new Point(area.Width - width - 10.0, area.Height - height - 10.0),
						new Point(10.0, 10.0)
					}
					select new Point(Math.Clamp(point2.X, 10.0, Math.Max(10.0, area.Width - width - 10.0)), Math.Clamp(point2.Y, 10.0, Math.Max(10.0, area.Height - height - 10.0))) into location
					orderby Overlap(new Rect(location, new Size(width, height)), target)
					select location).First();
				Set(_card, point.X, point.Y);
			}
		}

		private double CardHeightLimit(Size area)
		{
			double num = Math.Max(160.0, area.Height - 20.0);
			if (_target == null || !_target.IsVisible || !AdornedElement.IsAncestorOf(_target))
			{
				return num;
			}
			Rect rect = _target.TransformToAncestor(AdornedElement).TransformBounds(new Rect(_target.RenderSize));
			rect.Inflate(5.0, 5.0);
			if (area.Width - rect.Right - 26.0 >= _card.Width || rect.Left - 26.0 >= _card.Width)
			{
				return num;
			}
			return Math.Clamp(Math.Max(rect.Top - 26.0, area.Height - rect.Bottom - 26.0), 160.0, num);
		}

		private static double Overlap(Rect left, Rect right)
		{
			left.Intersect(right);
			if (!left.IsEmpty)
			{
				return left.Width * left.Height;
			}
			return 0.0;
		}

		private static void Set(FrameworkElement element, double x, double y)
		{
			if (double.IsNaN(Canvas.GetLeft(element)) || Math.Abs(Canvas.GetLeft(element) - x) > 0.2)
			{
				Canvas.SetLeft(element, x);
			}
			if (double.IsNaN(Canvas.GetTop(element)) || Math.Abs(Canvas.GetTop(element) - y) > 0.2)
			{
				Canvas.SetTop(element, y);
			}
		}

		private static void SetSize(FrameworkElement element, double width, double height)
		{
			if (double.IsNaN(element.Width) || Math.Abs(element.Width - width) > 0.2)
			{
				element.Width = width;
			}
			if (double.IsNaN(element.Height) || Math.Abs(element.Height - height) > 0.2)
			{
				element.Height = height;
			}
		}
	}

	private readonly Window _owner;

	private readonly FrameworkElement _root;

	private readonly IReadOnlyList<GuidedTourStep> _steps;

	private readonly AdornerLayer _layer;

	private readonly CoachAdorner _adorner;

	private readonly bool[] _observed;

	private Action? _detach;

	private DispatcherOperation? _position;

	private int _index;

	public bool IsActive { get; private set; } = true;

	public bool IsCompleted { get; private set; }

	public event EventHandler? Closed;

	public GuidedTour(Window owner, FrameworkElement root, IReadOnlyList<GuidedTourStep> steps)
	{
		ArgumentNullException.ThrowIfNull(owner, "owner");
		ArgumentNullException.ThrowIfNull(root, "root");
		ArgumentNullException.ThrowIfNull(steps, "steps");
		if (steps.Count == 0)
		{
			throw new ArgumentException("A guide needs at least one step.", "steps");
		}
		_owner = owner;
		_root = root;
		_steps = steps.ToArray();
		_observed = new bool[steps.Count];
		_layer = AdornerLayer.GetAdornerLayer(root) ?? throw new InvalidOperationException("The guide root needs an AdornerDecorator.");
		_adorner = new CoachAdorner(root, Previous, Next, Dispose);
		_layer.Add(_adorner);
		_owner.Closed += OnClosed;
		_owner.KeyDown += OnKeyDown;
		_owner.IsVisibleChanged += OnVisibility;
		_root.LayoutUpdated += OnLayout;
		ShowStep();
	}

	private void ShowStep()
	{
		_detach?.Invoke();
		_detach = null;
		_position?.Abort();
		GuidedTourStep step = _steps[_index];
		step.Prepare?.Invoke();
		_adorner.SetStep(step, _index, _steps.Count, _observed[_index]);
		_position = _root.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)(() =>
		{
			_position = null;
			if (IsActive)
			{
				step.Target.BringIntoView();
				_adorner.InvalidateMeasure();
				Observe(step);
			}
		}));
	}

	private void Observe(GuidedTourStep step)
	{
		GuideInteraction interaction = step.Interaction;
		FrameworkElement target = step.Target;
		switch (interaction)
		{
		case GuideInteraction.Value:
		{
			Slider slider = target as Slider;
			if (slider != null)
			{
				RoutedPropertyChangedEventHandler<double> value = (object _, RoutedPropertyChangedEventArgs<double> _) =>
				{
					Done();
				};
				slider.ValueChanged += value;
				_detach = () =>
				{
					slider.ValueChanged -= value;
				};
			}
			break;
		}
		case GuideInteraction.Toggle:
		{
			ToggleButton toggleButton = target as ToggleButton;
			if (toggleButton != null)
			{
				RoutedEventHandler changed = (object _, RoutedEventArgs _) =>
				{
					Done();
				};
				toggleButton.Checked += changed;
				toggleButton.Unchecked += changed;
				_detach = () =>
				{
					toggleButton.Checked -= changed;
					toggleButton.Unchecked -= changed;
				};
			}
			break;
		}
		case GuideInteraction.Selection:
		{
			Selector selector = target as Selector;
			if (selector != null)
			{
				SelectionChangedEventHandler selected = (object _, SelectionChangedEventArgs _) =>
				{
					Done();
				};
				selector.SelectionChanged += selected;
				_detach = () =>
				{
					selector.SelectionChanged -= selected;
				};
			}
			break;
		}
		case GuideInteraction.Text:
		case GuideInteraction.ClearText:
		{
			TextBox textBox = target as TextBox;
			if (textBox == null)
			{
				break;
			}
			TextChangedEventHandler text = (object _, TextChangedEventArgs _) =>
			{
				if ((step.Interaction == GuideInteraction.Text) ? (!string.IsNullOrWhiteSpace(textBox.Text)) : (textBox.Text.Length == 0))
				{
					Done();
				}
			};
			textBox.TextChanged += text;
			_detach = () =>
			{
				textBox.TextChanged -= text;
			};
			break;
		}
		case GuideInteraction.Click:
		{
			ButtonBase buttonBase = target as ButtonBase;
			if (buttonBase != null)
			{
				RoutedEventHandler click = (object _, RoutedEventArgs _) =>
				{
					Done();
				};
				buttonBase.Click += click;
				_detach = () =>
				{
					buttonBase.Click -= click;
				};
			}
			break;
		}
		case GuideInteraction.Focus:
		{
			KeyboardFocusChangedEventHandler focus = (object _, KeyboardFocusChangedEventArgs _) =>
			{
				Done();
			};
			step.Target.GotKeyboardFocus += focus;
			_detach = () =>
			{
				step.Target.GotKeyboardFocus -= focus;
			};
			break;
		}
		}
		void Done()
		{
			if (IsActive)
			{
				_observed[_index] = true;
				_adorner.ShowFeedback(observed: true);
			}
		}
	}

	private void Previous()
	{
		if (IsActive && _index > 0)
		{
			_index--;
			ShowStep();
		}
	}

	private void Next()
	{
		if (IsActive)
		{
			if (_index + 1 == _steps.Count)
			{
				IsCompleted = true;
				Dispose();
			}
			else
			{
				_index++;
				ShowStep();
			}
		}
	}

	private void OnLayout(object? sender, EventArgs e)
	{
		_adorner.Position();
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		Dispose();
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape && !e.Handled)
		{
			e.Handled = true;
			Dispose();
		}
	}

	private void OnVisibility(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!_owner.IsVisible)
		{
			Dispose();
		}
	}

	public void Dispose()
	{
		if (IsActive)
		{
			IsActive = false;
			_position?.Abort();
			_position = null;
			_detach?.Invoke();
			_detach = null;
			_root.LayoutUpdated -= OnLayout;
			_owner.Closed -= OnClosed;
			_owner.IsVisibleChanged -= OnVisibility;
			_owner.KeyDown -= OnKeyDown;
			_layer.Remove(_adorner);
			Closed?.Invoke(this, EventArgs.Empty);
		}
	}
}
