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
    // startPoint は描画中の基準点を保持します。
    // - 線モード: 最初のクリックで始点を保存し、次のクリックで終点を確定します。
    // - 円モード: 最初のクリックで中心を保存し、次のクリックで半径を確定します。
    // null の場合は描画が開始されていない状態を示します。
    private Point? startPoint = null;

    // 現在のツールモードを表す簡易列挙体
    // - None: 未選択
    // - Line: 線を描画するモード
    // - Circle: 円を描画するモード
    // - Select: オブジェクト選択・編集モード（プレースホルダ）
    private enum Mode { None, Line, Circle, Select }

    // アプリ起動時は線描画モードにする（必要なら初期値を変更する）
    // currentMode は UI のツール選択に応じて描画挙動を切り替えます。
    private Mode currentMode = Mode.Line;

    // プレビュー用の一時 Line / Ellipse を保持する。
    // - previewLine: 線描画時の破線プレビュー。確定されると実体の Line が追加される。
    // - previewEllipse: 円描画時の破線プレビュー。プレビューは描画補助でありヒットテスト対象外。
    // プレビュー要素は描画確定時に RemovePreview() で削除される。
    private Line? previewLine = null;
    private Ellipse? previewEllipse = null;

    public MainWindow()
    {
        InitializeComponent();
        // Esc キー押下で描画コマンドをキャンセルするためのハンドラを登録
        this.PreviewKeyDown += Window_PreviewKeyDown;
        // アクティブなキーボードフォーカスをウィンドウに設定しておくことで
        // キーイベント（Esc 等）を確実に受け取れるようにする
        this.Loaded += (s, e) => Keyboard.Focus(this);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Esc は現在のコマンドを完全にキャンセルする用途に使う
            // ここではプレビューを消し、描画の状態（startPoint）もクリアする
            RemovePreview();
            startPoint = null;
            // 他のコンポーネントに伝播させない
            e.Handled = true;
        }
    }

    private void DrawingCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 右クリックで現在の描画途中状態をキャンセルする
        // CADでは Esc や右クリックでコマンドキャンセルが一般的なので簡易対応
        startPoint = null;
        RemovePreview();
    }

    private void LineButton_Click(object sender, RoutedEventArgs e)
    {
        // ツールを線描画に切り替える
        // 将来的にはボタンの見た目をトグルにしたり、ホットキーで切り替えられるようにする
        currentMode = Mode.Line;
        // モード切替時はプレビューをリセット
        RemovePreview();
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
        // 選択モードに移行する際は描画中の状態をクリアする
        startPoint = null;
        RemovePreview();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        // キャンバス上の全オブジェクトを削除します。
        // 注意: 将来的に Undo/Redo を実装する場合は、ここで直接削除するのではなく
        // コマンドパターンを使用して操作を記録するように変更すること。
        // すべての図形を削除し内部状態をリセットする（Undo 未実装）
        DrawingCanvas.Children.Clear();
        startPoint = null;
        RemovePreview();
    }

    private void RemovePreview()
    {
        // プレビュー用の要素（線と楕円）を安全に削除する
        if (previewLine != null)
        {
            if (DrawingCanvas.Children.Contains(previewLine))
            {
                DrawingCanvas.Children.Remove(previewLine);
            }

            previewLine = null;
        }

        if (previewEllipse != null)
        {
            if (DrawingCanvas.Children.Contains(previewEllipse))
            {
                DrawingCanvas.Children.Remove(previewEllipse);
            }

            previewEllipse = null;
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

    private void EnsurePreviewEllipse()
    {
        if (previewEllipse == null)
        {
            previewEllipse = new Ellipse
            {
                Stroke = Brushes.Gray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 4, 2 },
                // プレビューは実際の図形操作の対象にしたくないためヒットテストを無効化しておく
                IsHitTestVisible = false
            };

            // Canvas に追加する。位置とサイズはマウス移動で更新される（中心は startPoint）。
            DrawingCanvas.Children.Add(previewEllipse);
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
                // 最初のクリック: 線の始点を記憶してプレビュー表示を開始する
                startPoint = p;
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

                // Canvas に確定した線を追加
                DrawingCanvas.Children.Add(line);
                // CAD ライクに連続描画できるよう終点を次の始点として保持する
                startPoint = p;
                // プレビューの始点も更新しておく
                if (previewLine != null)
                {
                    previewLine.X1 = startPoint.Value.X;
                    previewLine.Y1 = startPoint.Value.Y;
                    previewLine.X2 = startPoint.Value.X;
                    previewLine.Y2 = startPoint.Value.Y;
                }
            }
        }
        else if (currentMode == Mode.Circle)
        {
            if (startPoint == null)
            {
                // 最初のクリック: 円の中心を記憶しプレビュー表示を開始
                startPoint = p;
                EnsurePreviewEllipse();
            }
            else
            {
                // 2回目のクリックで円を確定（中心=startPoint, 半径=今回のクリックまでの距離）
                double dx = p.X - startPoint.Value.X;
                double dy = p.Y - startPoint.Value.Y;
                double r = Math.Sqrt(dx * dx + dy * dy);

                Ellipse ellipse = new Ellipse();
                ellipse.Width = r * 2;
                ellipse.Height = r * 2;
                ellipse.Stroke = Brushes.Black;
                ellipse.StrokeThickness = 2;

                Canvas.SetLeft(ellipse, startPoint.Value.X - r);
                Canvas.SetTop(ellipse, startPoint.Value.Y - r);

                // 確定した円を Canvas に追加し、描画状態をクリア
                DrawingCanvas.Children.Add(ellipse);
                startPoint = null;
                RemovePreview();
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
        else if (currentMode == Mode.Circle && startPoint != null)
        {
            Point p = e.GetPosition(DrawingCanvas);
            double dx = p.X - startPoint.Value.X;
            double dy = p.Y - startPoint.Value.Y;
            double r = Math.Sqrt(dx * dx + dy * dy);

            EnsurePreviewEllipse();

            previewEllipse!.Width = r * 2;
            previewEllipse!.Height = r * 2;
            Canvas.SetLeft(previewEllipse, startPoint.Value.X - r);
            Canvas.SetTop(previewEllipse, startPoint.Value.Y - r);
        }
    }

    private void CircleButton_Click(object sender, RoutedEventArgs e)
    {
        currentMode = Mode.Circle;
        RemovePreview();
    }
}