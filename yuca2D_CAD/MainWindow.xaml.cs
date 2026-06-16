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

    // プレビュー用の一時 Line を保持する。
    // 実際に確定した線は DrawingCanvas.Children に追加されるが、
    // プレビューはあくまで視覚フィードバック用であり確定時のみ Children に追加する。
    private Line? previewLine = null;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void DrawingCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 右クリックで現在の描画途中状態をキャンセルする
        // CADでは Esc や右クリックでコマンドキャンセルが一般的なので簡易対応
        startPoint = null;
        RemovePreviewLine();
    }

    private void LineButton_Click(object sender, RoutedEventArgs e)
    {
        // ツールを線描画に切り替える
        // 将来的にはボタンの見た目をトグルにしたり、ホットキーで切り替えられるようにする
        currentMode = Mode.Line;
        // モード切替時はプレビューをリセット
        RemovePreviewLine();
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
        // 選択モードに切り替えた際には、もし線描画の途中(startPoint が設定されている)
        // 場合はそれをキャンセルしておく。これにより意図しない連続描画が防止される。
        startPoint = null;
        RemovePreviewLine();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        // キャンバス上の全オブジェクトを削除します。
        // 注意: 将来的に Undo/Redo を実装する場合は、ここで直接削除するのではなく
        // コマンドパターンを使用して操作を記録するように変更すること。
        DrawingCanvas.Children.Clear();
        startPoint = null;
        RemovePreviewLine();
    }

    private void RemovePreviewLine()
    {
        if (previewLine != null)
        {
            // プレビューは Children に追加していないのでキャンバスから直接削除する必要はないが、
            // 万が一追加しているケースに備えて安全に削除処理を行う
            if (DrawingCanvas.Children.Contains(previewLine))
            {
                DrawingCanvas.Children.Remove(previewLine);
            }

            previewLine = null;
        }
    }

    private void EnsurePreviewLine()
    {
        if (previewLine == null)
        {
            previewLine = new Line
            {
                Stroke = Brushes.Gray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 4, 2 },
                IsHitTestVisible = false // プレビューはヒットテスト対象にしない
            };

            // プレビューは Children に追加して視覚化する
            DrawingCanvas.Children.Add(previewLine);
        }
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
                // 始点が決まったらマウス移動でプレビューを表示できるようにする
                EnsurePreviewLine();
            }
            else
            {
                // 既に始点がある場合は、その始点から今回クリック位置までの線を確定する
                Line line = new Line();

                line.X1 = startPoint.Value.X;
                line.Y1 = startPoint.Value.Y;

                line.X2 = p.X;
                line.Y2 = p.Y;

                line.Stroke = Brushes.Black;
                line.StrokeThickness = 2;

                DrawingCanvas.Children.Add(line);
                // CADライクな連続線描画にするために、今回の終点を次の始点として保持する
                // これにより三回目以降は一回クリックで次の線が確定される
                startPoint = p;

                // 次の線描画に備えてプレビューの始点を更新する
                if (previewLine != null)
                {
                    previewLine.X1 = startPoint.Value.X;
                    previewLine.Y1 = startPoint.Value.Y;
                    previewLine.X2 = startPoint.Value.X;
                    previewLine.Y2 = startPoint.Value.Y;
                }
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

    private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        // マウス移動時にプレビューを更新する処理
        if (currentMode == Mode.Line && startPoint != null)
        {
            Point p = e.GetPosition(DrawingCanvas);

            // プレビューラインが無ければ作成
            EnsurePreviewLine();

            // プレビューの始点は startPoint、終点は現在のマウス位置
            previewLine!.X1 = startPoint.Value.X;
            previewLine!.Y1 = startPoint.Value.Y;
            previewLine!.X2 = p.X;
            previewLine!.Y2 = p.Y;
        }
    }
}