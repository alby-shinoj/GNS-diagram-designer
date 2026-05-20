using System.Windows;
using GsnDiagramEditor.ViewModels;

namespace GsnDiagramEditor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
