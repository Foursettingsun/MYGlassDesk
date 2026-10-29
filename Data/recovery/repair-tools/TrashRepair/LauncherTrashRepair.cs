using System;
using System.Reflection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GlassDesk.Launcher.Repair;

public static class LauncherTrashRepair
{
    private sealed class Placement
    {
        public Border Trash = null!;
        public Grid Content = null!;
        public FrameworkElement SearchHeader = null!;
        public Thickness OriginalHeaderMargin;
        public DependencyPropertyDescriptor VisibilityDescriptor=null!;
        public void VisibilityChanged(object? sender,EventArgs args){UpdateHeader();}
        public void SizeChanged(object sender,SizeChangedEventArgs args){UpdateHeader();}
        public void Closed(object? sender,EventArgs args)
        {
            VisibilityDescriptor.RemoveValueChanged(Trash,VisibilityChanged);
            Content.SizeChanged-=SizeChanged;
        }
        public void UpdateHeader()
        {
            double desiredHeaderWidth=double.IsFinite(SearchHeader.MaxWidth)?SearchHeader.MaxWidth:600;
            bool reserve=Trash.Visibility==Visibility.Visible&&Content.ActualWidth<desiredHeaderWidth+100;
            var original=OriginalHeaderMargin;
            var margin=reserve?new Thickness(original.Left+50,original.Top,original.Right,original.Bottom):original;
            if(SearchHeader.Margin!=margin)SearchHeader.Margin=margin;
        }
    }

    public static void Configure(object window)
    {
        var trash=(Border)Read(window,"TrashDropTarget");
        var content=(Grid)Read(window,"LauncherContent");
        var header=(FrameworkElement)Read(window,"SearchHeader");
        var placement=new Placement{Trash=trash,Content=content,SearchHeader=header,OriginalHeaderMargin=header.Margin};
        if(trash.Parent is Panel parent)parent.Children.Remove(trash);
        else throw new InvalidOperationException("Unexpected launcher trash parent.");
        trash.Width=38;trash.Height=29;trash.MinWidth=0;trash.MinHeight=0;trash.AllowDrop=true;
        trash.HorizontalAlignment=HorizontalAlignment.Left;trash.VerticalAlignment=VerticalAlignment.Top;
        trash.Margin=new Thickness(0,3,0,0);
        var radius=trash.CornerRadius;
        trash.CornerRadius=new CornerRadius(radius.TopLeft*0.5,radius.TopRight*0.5,radius.BottomRight*0.5,radius.BottomLeft*0.5);
        var padding=trash.Padding;
        trash.Padding=new Thickness(padding.Left*0.5,padding.Top*0.5,padding.Right*0.5,padding.Bottom*0.5);
        if(trash.Child is Viewbox icon){icon.Width=14;icon.Height=14;}
        Grid.SetRow(trash,0);Grid.SetColumn(trash,0);Grid.SetRowSpan(trash,1);Grid.SetColumnSpan(trash,Math.Max(1,content.ColumnDefinitions.Count));
        Panel.SetZIndex(trash,100);
        content.Children.Add(trash);
        placement.VisibilityDescriptor=DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty,typeof(Border));
        placement.VisibilityDescriptor.AddValueChanged(trash,placement.VisibilityChanged);
        content.SizeChanged+=placement.SizeChanged;
        ((Window)window).Closed+=placement.Closed;
        placement.UpdateHeader();
    }

    private static object Read(object window,string name)=>window.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(window)!;
}
