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
    // startPoint は線を描く際の最初のクリック位置を保持します。
    // null の場合は次のクリックが始点として扱われます。
    private Point? startPoint = null;

    // 現在のツールモードを表す簡易列挙体
    // - None: 未選択（将来的にツールパレット無しの状態で使用）
    // - Line: 線を描画するモード（現在実装済み）
    // - Select: オブジェクト選択・編集モード（プレースホルダ）
    private enum Mode { None, Line, Select }

    // アプリ起動時は線描画モードにしておく（好みに応じて変更可）
    private Mode currentMode = Mode.Line;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void LineButton_Click(object sender, RoutedEventArgs e)
    {
        // ツールを線描画に切り替える
        // 将来的にはボタンの見た目をトグルにしたり、ホットキーで切り替えられるようにする
        currentMode = Mode.Line;
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        // ツールを選択モードに切り替える（ただし実装は未完）
        // TODO: 選択モードの実装方針
        //  - マウスクリックでのヒットテスト (点-線距離、点-点距離)
        //  - Shift/Ctrl による複数選択のサポート
        //  - 選択ハンドルとドラッグによる移動・スケーリング
        //  - 選択情報はモデル側で管理し、見た目はアドオンのレイヤーで描画する
        currentMode = Mode.Select;
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        // キャンバス上の全オブジェクトを削除します。
        // 注意: 将来的に Undo/Redo を実装する場合は、ここで直接削除するのではなく
        // コマンドパターンを使用して操作を記録するように変更すること。
        DrawingCanvas.Children.Clear();
        startPoint = null;
    }

    private void DrawingCanvas_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(DrawingCanvas);

        // 現状は Line モードでの2クリックによる線描画のみを実装しています。
        // 将来的にはマウスダウンで始点、マウスムーブでプレビュー、マウスアップで確定
        // のインタラクションに変更することを推奨します。
        if (currentMode == Mode.Line)
        {
            if (startPoint == null)
            {
                // 最初のクリック: 始点を記憶
                startPoint = p;
            }
            else
            {
                // 2回目のクリック: 終点を使って Line 要素を作成
                Line line = new Line();

                // Canvas 上の座標系 (左上が原点) をそのまま使用
                line.X1 = startPoint.Value.X;
                line.Y1 = startPoint.Value.Y;

                line.X2 = p.X;
                line.Y2 = p.Y;

                // 簡易的な見た目設定。将来的にはスタイルや線種をプロパティ化する
                line.Stroke = Brushes.Black;
                line.StrokeThickness = 2;

                DrawingCanvas.Children.Add(line);

                // 次の線描画に備えて始点をクリア
                startPoint = null;
            }
        }
        else if (currentMode == Mode.Select)
        {
            // 選択モード: ここにヒットテストと選択管理のロジックを入れる
            // 例:
            //  - foreach (var child in DrawingCanvas.Children) { if (HitTest(child, p)) select; }
            //  - 選択された要素には Adorner や別レイヤーでハンドルを描画する
        }
    }
}