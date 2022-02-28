using System;
using System.Windows;
using System.Windows.Controls;

namespace CrossChainArbitrageBot;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private bool noticesAutoScroll;

    private void ImportantNoticeViewerOnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // User scroll event : set or unset auto-scroll mode
        if (e.ExtentHeightChange == 0)
        {
            // Content unchanged : user scroll event
            noticesAutoScroll = Math.Abs(ImportantNoticeViewer.VerticalOffset - ImportantNoticeViewer.ScrollableHeight) < 0.1;
        }

        // Content scroll event : auto-scroll eventually
        if (noticesAutoScroll && e.ExtentHeightChange != 0)
        {   // Content changed and auto-scroll mode set
            // Autoscroll
            ImportantNoticeViewer.ScrollToVerticalOffset(ImportantNoticeViewer.ExtentHeight);
        }
    }
}