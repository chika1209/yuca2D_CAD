using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace yuca2D_CAD;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Point? startPoint = null;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void DrawingCanvas_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(DrawingCanvas);

        if (startPoint == null)
        {
            startPoint = p;
        }
        else
        {
            Line line = new Line();

            line.X1 = startPoint.Value.X;
            line.Y1 = startPoint.Value.Y;

            line.X2 = p.X;
            line.Y2 = p.Y;

            line.Stroke = Brushes.Black;
            line.StrokeThickness = 2;

            DrawingCanvas.Children.Add(line);

            startPoint = null;
        }
    }
}