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
    // 用途の詳細:
    // - 線モード: startPoint は "始点" を表します。ユーザーが最初にクリックした座標を保存し、
    //   次回のクリックでそこから現在のクリック位置までを実線で確定します。
    // - 円モード: startPoint は "中心" を表します。中心から現在のマウス位置までの距離を半径として扱います。
    // - 四角モード: startPoint は矩形の一隅 (corner) を表します。もう一方の対角点は次のクリック位置で決まります。
    // 値が null の場合は「現在描画中の図形はない（待機状態）」を意味します。
    private Point? startPoint = null;

    // 現在のツールモードを表す簡易列挙体
    // 解説:
    // - None: ツール未選択。描画イベントは無視される想定。
    // - Line: 2クリックで線を作成。連続描画（終点を次の始点にする）をサポートしている。
    // - Circle: 1クリックで中心を決め、2クリック目で半径を確定して円を作成する。
    // - Rect: 1クリックで一隅を決め、2クリック目で対角点を指定して矩形を作成する。
    // - Select: 将来的な選択／編集モード。現在はプレースホルダ。
    private enum Mode { None, Line, Circle, Rect, Select }

    // アプリ起動時は線描画モードにする（必要なら初期値を変更する）
    // currentMode は UI のツール選択に応じて描画挙動を切り替えます。
    private Mode currentMode = Mode.Line;

    // プレビュー用の一時要素を保持する。
    // 目的: ユーザーが次のクリックやマウス移動でどのような図形が生成されるか視覚的に把握できるようにする。
    // - previewLine: 線描画時に使用する破線の Line。実体の Line を追加するまでは編集の対象にしない。
    // - previewEllipse: 円描画時に使用する破線の Ellipse。中心と半径は startPoint とマウス位置から算出する。
    // - previewRect: 矩形描画時に使用する破線の Rectangle。startPoint とマウス位置で左上/幅/高さを決定する。
    // いずれも IsHitTestVisible=false にして、ユーザー操作の邪魔にならないようにしている。
    private Line? previewLine = null;
    private Ellipse? previewEllipse = null;
    private Rectangle? previewRect = null;

    public MainWindow()
    {
        InitializeComponent();
        // Esc キー押下で描画コマンドをキャンセルするためのハンドラを登録
        this.PreviewKeyDown += Window_PreviewKeyDown;
        // アクティブなキーボードフォーカスをウィンドウに設定しておくことで
        // キーイベント（Esc 等）を確実に受け取れるようにする
        this.Loaded += (s, e) =>
        {
            Keyboard.Focus(this);
            UpdateModeVisuals();
        };
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Esc は現在のコマンドを完全にキャンセルする用途に使う
            // - 画面上に残っているプレビュー要素（破線など）を削除
            // - startPoint を null にして内部状態をリセット
            // これによりユーザーは新たに描画を開始できる
            RemovePreview();
            startPoint = null;
            // 他のコントロールへイベントを伝搬させない
            e.Handled = true;
        }
    }

    private void DrawingCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 右クリックで現在の描画途中状態をキャンセルする
        // CADでは Esc や右クリックでコマンドキャンセルが一般的なので簡易対応
        startPoint = null;
        RemovePreview();
        UpdateModeVisuals();
    }

    private void LineButton_Click(object sender, RoutedEventArgs e)
    {
        // ツールを線描画に切り替える
        // 将来的にはボタンの見た目をトグルにしたり、ホットキーで切り替えられるようにする
        currentMode = Mode.Line;
        // モード切替時はプレビューをリセット
        RemovePreview();
        UpdateModeVisuals();
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
        UpdateModeVisuals();
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
        // プレビュー用の要素（線・楕円・矩形）を安全に削除する
        // 理由: プレビューは一時的に Canvas.Children に追加して視覚化しているため、
        // 確定・取消のたびに明示的に削除する必要がある。
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

        if (previewRect != null)
        {
            if (DrawingCanvas.Children.Contains(previewRect))
            {
                DrawingCanvas.Children.Remove(previewRect);
            }

            previewRect = null;
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
                // 破線スタイルはプレビューであることをユーザーに示すため
                StrokeDashArray = new DoubleCollection() { 4, 2 },
                // プレビューはヒットテスト対象にしない（操作対象を邪魔しない）
                IsHitTestVisible = false
            };

            // Canvas に追加して視覚化する。削除は RemovePreview() で行う。
            DrawingCanvas.Children.Add(previewLine);
        }
    }

    // UI フィードバック: 現在のモードに応じてカーソルとウィンドウタイトルを更新する
    private void UpdateModeVisuals()
    {
        // カーソル: 描画モードではクロスにする
        if (currentMode == Mode.Line || currentMode == Mode.Circle || currentMode == Mode.Rect)
        {
            DrawingCanvas.Cursor = Cursors.Cross;
        }
        else
        {
            DrawingCanvas.Cursor = Cursors.Arrow;
        }

        // アプリ名は XAML の Title を使うため、ここではタイトルを上書きしない
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

    private void EnsurePreviewRect()
    {
        if (previewRect == null)
        {
            previewRect = new Rectangle
            {
                Stroke = Brushes.Gray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 4, 2 },
                // 破線と薄い色でプレビューを示す。操作対象にはしない。
                IsHitTestVisible = false
            };

            DrawingCanvas.Children.Add(previewRect);
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
        // クリックで確定するパターン（モード別に挙動を分岐）
        // 各モードの共通設計:
        // - 1回目クリック: startPoint を保存してプレビューを開始
        // - マウス移動: プレビューを更新
        // - 2回目クリック: 図形を確定して Canvas に追加、startPoint をクリア
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
                // これによりユーザーは続けてクリックするだけで折れ線を引ける
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
                // 最初のクリック: 円の中心を決定
                // 以後の MouseMove/2回目クリックで半径を決定する
                startPoint = p;
                EnsurePreviewEllipse();
            }
            else
            {
                // 確定処理:
                // 半径 r はユークリッド距離 sqrt((dx)^2 + (dy)^2) で計算する
                double dx = p.X - startPoint.Value.X;
                double dy = p.Y - startPoint.Value.Y;
                double r = Math.Sqrt(dx * dx + dy * dy);

                // Ellipse の Width/Height は直径に相当するため 2*r を設定
                Ellipse ellipse = new Ellipse();
                ellipse.Width = r * 2;
                ellipse.Height = r * 2;
                ellipse.Stroke = Brushes.Black;
                ellipse.StrokeThickness = 2;

                // Canvas は左上基準なので、左上位置を中心 - r に合わせる
                Canvas.SetLeft(ellipse, startPoint.Value.X - r);
                Canvas.SetTop(ellipse, startPoint.Value.Y - r);

                // Canvas に追加して確定、内部状態をクリア
                DrawingCanvas.Children.Add(ellipse);
                startPoint = null;
                RemovePreview();
            }
        }
        else if (currentMode == Mode.Rect)
        {
            System.Diagnostics.Debug.WriteLine($"[Debug] Rect mode MouseLeftButtonDown at {p}");
            if (startPoint == null)
            {
                System.Diagnostics.Debug.WriteLine("[Debug] Rect mode: setting startPoint");
                // 最初のクリック: 矩形の一隅を記憶してプレビュー開始
                startPoint = p;
                EnsurePreviewRect();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Debug] Rect mode: confirming rect from {startPoint} to {p}");
                // 2回目のクリックで矩形を確定する。startPoint と今回クリック位置 p を対角点とする
                double x1 = startPoint.Value.X;
                double y1 = startPoint.Value.Y;
                double x2 = p.X;
                double y2 = p.Y;

                double left = Math.Min(x1, x2);
                double top = Math.Min(y1, y2);
                double w = Math.Abs(x2 - x1);
                double h = Math.Abs(y2 - y1);

                Rectangle rect = new Rectangle();
                rect.Width = w;
                rect.Height = h;
                rect.Stroke = Brushes.Black;
                rect.StrokeThickness = 2;

                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);

                DrawingCanvas.Children.Add(rect);

                // 確定後は状態をリセット
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
        // MouseMove はプレビュー更新専用: マウス座標からプレビュー図形の見た目を調整する。
        // 注意: Canvas 上の座標系は GetPosition で得た値と一致するため、変換は不要。
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
            // プレビュー更新: 中心=startPoint、半径=中心から現在マウス位置までの距離
            Point p = e.GetPosition(DrawingCanvas);
            double dx = p.X - startPoint.Value.X;
            double dy = p.Y - startPoint.Value.Y;
            double r = Math.Sqrt(dx * dx + dy * dy);

            EnsurePreviewEllipse();

            // Ellipse の Width/Height は直径を表す
            previewEllipse!.Width = r * 2;
            previewEllipse!.Height = r * 2;
            // 左上座標は中心 - r
            Canvas.SetLeft(previewEllipse, startPoint.Value.X - r);
            Canvas.SetTop(previewEllipse, startPoint.Value.Y - r);
        }
        else if (currentMode == Mode.Rect && startPoint != null)
        {
            Point p = e.GetPosition(DrawingCanvas);
            double x1 = startPoint.Value.X;
            double y1 = startPoint.Value.Y;
            double x2 = p.X;
            double y2 = p.Y;

            double left = Math.Min(x1, x2);
            double top = Math.Min(y1, y2);
            double w = Math.Abs(x2 - x1);
            double h = Math.Abs(y2 - y1);

            EnsurePreviewRect();
            previewRect!.Width = w;
            previewRect!.Height = h;
            Canvas.SetLeft(previewRect, left);
            Canvas.SetTop(previewRect, top);
        }
    }

    private void CircleButton_Click(object sender, RoutedEventArgs e)
    {
        currentMode = Mode.Circle;
        RemovePreview();
        UpdateModeVisuals();
    }

    private void RectButton_Click(object sender, RoutedEventArgs e)
    {
        currentMode = Mode.Rect;
        RemovePreview();
        UpdateModeVisuals();
    }
}