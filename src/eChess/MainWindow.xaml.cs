using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static eChess.Pieces;
using Point = System.Drawing.Point;
using Button = System.Windows.Controls.Button;
using System.Drawing;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;
using System.Windows.Media.Effects;
using System.Threading.Tasks;
using System.Threading;

namespace eChess
{
    public partial class MainWindow : Window
    {
        Point pos = new Point(0, 0);
        Button slctdBtn;
        bool whitesTurn = true;
        readonly Field[,] board = new Field[8, 8];
        readonly BackgroundWorker GameEndChecker = new BackgroundWorker();
        Button lastSelectedPiece = null;
        bool currentlyInCheck;
        readonly Moves moves = new Moves();

        public MainWindow()
        {
            InitializeComponent();
            PrepareBoard();
            GameEndChecker.DoWork += CheckForGameEnd;
        }

        private void PrepareBoard()
        {
            //To initialize and make actual first call faster
            board.Clone<Field[,]>();

            for (int row = 0; row < 8; row++)
            {
                for (int column = 0; column < 8; column++)
                {
                    Piece p = Piece.Empty;
                    p = SetPieceOnField(column, row, p);
                    board[column, row] = new Field { Point = new Point(column, row), Piece = p };
                }
            }
        }

        private static Piece SetPieceOnField(int column, int row, Piece p)
        {
            if ((column == 0 || column == 7) && row == 0)
            {
                p = Piece.BlackRook;
            }
            if ((column == 1 || column == 6) && row == 0)
            {
                p = Piece.BlackKnight;
            }
            if ((column == 2 || column == 5) && row == 0)
            {
                p = Piece.BlackBishop;
            }
            if (column == 3 && row == 0)
            {
                p = Piece.BlackQueen;
            }
            if (column == 4 && row == 0)
            {
                p = Piece.BlackKing;
            }
            if (row == 1)
            {
                p = Piece.BlackPawn;
            }


            if ((column == 0 || column == 7) && row == 7)
            {
                p = Piece.WhiteRook;
            }
            if ((column == 1 || column == 6) && row == 7)
            {
                p = Piece.WhiteKnight;
            }
            if ((column == 2 || column == 5) && row == 7)
            {
                p = Piece.WhiteBishop;
            }
            if (column == 3 && row == 7)
            {
                p = Piece.WhiteQueen;
            }
            if (column == 4 && row == 7)
            {
                p = Piece.WhiteKing;
            }
            if (row == 6)
            {
                p = Piece.WhitePawn;
            }

            return p;
        }

        private void ResetHints()
        {
            for (int column = 1; column < 9; column++)
            {
                for (int row = 1; row < 9; row++)
                {
                    Button button = (Button)Grid.FindName("Field" + column + "_" + row);
                    button.Visibility = Visibility.Collapsed;
                    button.Style = this.FindResource("EmptyField") as Style;
                }
            }
        }

        private void ShowAvaibleMoves(Piece piece)
        {
            List<Point> validMoves = moves.GetValidMoves(piece, pos, board, whitesTurn, false);

            slctdBtn.Background = this.FindResource("SelectedBrush") as Brush;
            foreach (var move in validMoves)
            {
                int column = move.X + 1;
                int row = move.Y + 1;
                Button button = (Button)Grid.FindName("Field" + column + "_" + row);
                if (board[move.X, move.Y].Piece != Piece.Empty)
                {
                    button.Style = this.FindResource("OccupiedField") as Style;
                }
                button.Visibility = Visibility.Visible;
            }
        }

        private void Field_Click(object sender, RoutedEventArgs e)
        {
            //Reset check
            WK.Background = Brushes.Transparent;
            BK.Background = Brushes.Transparent;
            lastSelectedPiece.Background = this.FindResource("DefaultBrush") as Brush;

            var clickedBtn = sender as Button;
            Point p = new Point(Grid.GetColumn(clickedBtn), Grid.GetRow(clickedBtn));
            if (board[p.X, p.Y].Piece != Piece.Empty)
            {
                PlaySound(Properties.Resources.Capture);
                RemovePiece(p);
            }
            else
            {
                PlaySound(Properties.Resources.Move);
            }
            board[p.X, p.Y].Piece = board[pos.X, pos.Y].Piece;
            board[pos.X, pos.Y].Piece = Piece.Empty;

            Grid.SetColumn(slctdBtn, p.X);
            Grid.SetRow(slctdBtn, p.Y);
            ResetHints();
            RotateBoardAndPieces();
            GameEndChecker.RunWorkerAsync();
        }

        private void CheckForGameEnd(object sender, DoWorkEventArgs e)
        {
            bool wT = !whitesTurn;
            currentlyInCheck = Check.IsCheck(board, wT);
            if (currentlyInCheck)
            {
                if (wT)
                    Dispatcher.BeginInvoke(new Action(() => BK.Background = Brushes.Red));
                else
                    Dispatcher.BeginInvoke(new Action(() => WK.Background = Brushes.Red));

                if (AtLeastOneMove(wT) == false)
                {
                    string player = "Black";
                    if (wT)
                    {
                        player = "White";
                    }
                    Dispatcher.BeginInvoke(new Action(() => GameEndingAnimation(player)));
                }
            }
            else
            {
                if (AtLeastOneMove(wT) == false)
                {
                    Dispatcher.BeginInvoke(new Action(() => GameEndingAnimation("")));
                }
            }
        }

        private void GameEndingAnimation(string player)
        {
            Grid.Effect = new BlurEffect();
            if (String.IsNullOrEmpty(player))
            {
                ReasonText.Text = "by stalemate";
                WinnerText.Text = "Draw";
            }
            else
            {
                ReasonText.Text = "by checkmate";
                WinnerText.Text = player + " won";
            }
            EndScreenGrid.Visibility = Visibility.Visible;
            BeginStoryboard sb = this.FindResource("EndScreenAnimation") as BeginStoryboard;
            sb.Storyboard.Begin();
        }

        private bool AtLeastOneMove(bool wT)
        {
            List<Piece> pieces = new List<Piece> { Piece.WhiteBishop, Piece.WhiteKnight, Piece.WhitePawn, Piece.WhiteQueen, Piece.WhiteRook, Piece.WhiteKing };
            if (wT == true)
                pieces = new List<Piece> { Piece.BlackBishop, Piece.BlackKnight, Piece.BlackPawn, Piece.BlackQueen, Piece.BlackRook, Piece.BlackKing };

            foreach (var field in board)
            {
                if (pieces.Contains(field.Piece))
                {
                    if (moves.GetValidMoves(field.Piece, field.Point, board, !wT, false).Count > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        private void PlaySound(UnmanagedMemoryStream stream)
        {
            SoundPlayer player = new SoundPlayer(stream);
            player.Play();
        }

        private void RotateBoardAndPieces()
        {
            DoubleAnimation gridAnimation;
            DoubleAnimation btnsAnimation;
            if (whitesTurn == true)
            {
                whitesTurn = false;
                btnsAnimation = new DoubleAnimation(0, 180, new Duration(TimeSpan.FromSeconds(0.5)));
                gridAnimation = new DoubleAnimation(0, 180, new Duration(TimeSpan.FromSeconds(0.5)));
            }
            else
            {
                whitesTurn = true;
                btnsAnimation = new DoubleAnimation(180, 0, new Duration(TimeSpan.FromSeconds(0.5)));
                gridAnimation = new DoubleAnimation(180, 0, new Duration(TimeSpan.FromSeconds(0.5)));
            }

            List<Button> allPieces = new List<Button> { BB1, BB2, BK, BN1, BN2, BP1, BP2, BP3, BP4, BP5, BP6, BP7, BP8, BQ, BR1, BR2, WB1, WB2, WK, WN1, WN2, WP1, WP2, WP3, WP4, WP5, WP6, WP7, WP8, WQ, WR1, WR2 };

            RotateTransform gridRT = new RotateTransform();
            RotateTransform btnsRT = new RotateTransform();

            Grid.RenderTransform = gridRT;
            Grid.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            foreach (var button in allPieces)
            {
                button.RenderTransform = btnsRT;
                button.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            }

            gridRT.BeginAnimation(RotateTransform.AngleProperty, gridAnimation);



            btnsRT.BeginAnimation(RotateTransform.AngleProperty, btnsAnimation);
        }

        private void RemovePiece(Point p)
        {
            List<Button> buttons;
            if (whitesTurn == true)
            {
                buttons = new List<Button> { BB1, BB2, BK, BN1, BN2, BP1, BP2, BP3, BP4, BP5, BP6, BP7, BP8, BQ, BR1, BR2 };
            }
            else
            {
                buttons = new List<Button> { WB1, WB2, WK, WN1, WN2, WP1, WP2, WP3, WP4, WP5, WP6, WP7, WP8, WQ, WR1, WR2 };
            }
            foreach (var button in buttons)
            {
                if (button.Visibility == Visibility.Visible)
                {
                    if (p.X == Grid.GetColumn(button) && p.Y == Grid.GetRow(button))
                    {
                        button.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void Piece_Click(object sender, RoutedEventArgs e)
        {
            slctdBtn = sender as Button;

            if (lastSelectedPiece != null)
            {
                if (currentlyInCheck == true && (lastSelectedPiece.Name == "BK" || lastSelectedPiece.Name == "WK"))
                {
                    lastSelectedPiece.Background = Brushes.Red;
                }
                else
                {
                    lastSelectedPiece.Background = this.FindResource("DefaultBrush") as Brush;
                }
            }
            lastSelectedPiece = slctdBtn;

            pos.X = Grid.GetColumn(slctdBtn);
            pos.Y = Grid.GetRow(slctdBtn);

            ResetHints();

            if (!whitesTurn)
            {
                if (slctdBtn.Name.Contains("BB"))
                {
                    ShowAvaibleMoves(Piece.BlackBishop);
                }

                if (slctdBtn.Name.Contains("BN"))
                {
                    ShowAvaibleMoves(Piece.BlackKnight);
                }

                if (slctdBtn.Name.Contains("BR"))
                {
                    ShowAvaibleMoves(Piece.BlackRook);
                }

                if (slctdBtn.Name.Contains("BQ"))
                {
                    ShowAvaibleMoves(Piece.BlackQueen);
                }

                if (slctdBtn.Name.Contains("BK"))
                {
                    ShowAvaibleMoves(Piece.BlackKing);
                }

                if (slctdBtn.Name.Contains("BP"))
                {
                    ShowAvaibleMoves(Piece.BlackPawn);
                }
            }
            else
            {
                if (slctdBtn.Name.Contains("WP"))
                {
                    ShowAvaibleMoves(Piece.WhitePawn);
                }
                if (slctdBtn.Name.Contains("WK"))
                {
                    ShowAvaibleMoves(Piece.WhiteKing);
                }
                if (slctdBtn.Name.Contains("WQ"))
                {
                    ShowAvaibleMoves(Piece.WhiteQueen);
                }
                if (slctdBtn.Name.Contains("WR"))
                {
                    ShowAvaibleMoves(Piece.WhiteRook);
                }
                if (slctdBtn.Name.Contains("WN"))
                {
                    ShowAvaibleMoves(Piece.WhiteKnight);
                }
                if (slctdBtn.Name.Contains("WB"))
                {
                    ShowAvaibleMoves(Piece.WhiteBishop);
                }
            }
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => this.Close());
        }

        private void NewGameBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Application.Restart();
            Application.Current.Dispatcher.Invoke(() => this.Close());
        }
    }
}
