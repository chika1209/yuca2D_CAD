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
    private enum Mode { None, Line, Select }
    private Mode currentMode = Mode.Line;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void LineButton_Click(object sender, RoutedEventArgs e)
    {
        currentMode = Mode.Line;
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        currentMode = Mode.Select;
        // 選択モードはまだ実装していない。将来的にヒットテストと選択ハイライトを実装してください。
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.Children.Clear();
        startPoint = null;
    }

    private void DrawingCanvas_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(DrawingCanvas);

        if (currentMode == Mode.Line)
        {
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
        else if (currentMode == Mode.Select)
        {
            // 簡易選択（未実装）: 将来的にクリック位置でヒットテストを行い、選択状態を管理する
        }
    }
}