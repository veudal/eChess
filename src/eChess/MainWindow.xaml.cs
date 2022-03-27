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
using System.Windows.Shapes;
using Image = System.Windows.Controls.Image;
using System.Windows.Media.Imaging;
using eChessServer.Entities;
using Newtonsoft.Json;
using System.Linq;
using System.Diagnostics;
using DK.WshRuntime;

namespace eChess
{
    public partial class MainWindow : Window
    {
        readonly string currentVersion = "v1.3";
        readonly string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\eChess\\";
        readonly Field[,] board = new Field[8, 8];
        readonly BackgroundWorker GameEndChecker = new BackgroundWorker();
        readonly Moves moves = new Moves();
        Point currentPos = new Point(0, 0);
        Point newPos = new Point(0, 0);
        readonly List<Point> markedFields = new List<Point>();
        List<string> promotedPieces = new List<string>();
        Button lastSelectedPiece = new Button();
        Button slctdBtn;
        bool currentlyInCheck;
        bool doubleMoveForEnPassent;
        bool whitesTurn = true;
        bool onlineGame;
        bool waitingForOpponent;
        string playerName = string.Empty;
        Guid playerGuid = Guid.NewGuid();
        GameEntity game = new GameEntity();
        BackgroundWorker backgroundMatchFinder = new BackgroundWorker();
        BackgroundWorker sendMove = new BackgroundWorker();
        BackgroundWorker receiveMove = new BackgroundWorker();
        MoveEntity opponentsMove = new MoveEntity();
        List<Button> allPieces = new List<Button>();

        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(path);
            SetupChessBoard();
            SetUsername();
            Update();
            CreateShortcuts();
            GameEndChecker.DoWork += CheckForGameEnd;
        }


        private void Update()
        {
            Updater updater = new Updater();
            if (updater.NewVersionAvailable(currentVersion) == true)
            {
                UpdatePage.Content = updater;
                UpdatePage.Visibility = Visibility.Visible;
            }
        }

        private void BackgroundMatchFinder_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (game is null)
            {
                CancelSearch();
            }
            else if (game.GameID != Guid.Empty)
            {
                PrepareOnlineGame();
            }
            StartOnlineGame.IsEnabled = true;
        }

        private void PrepareOnlineGame()
        {
            StartOnlineGame.IsEnabled = true;
            StartOnlineGame.Background = Brushes.DarkSlateBlue;
            StartOnlineGame.Content = "Online game";
            ProgressRing.Visibility = Visibility.Collapsed;
            Menu.Visibility = Visibility.Collapsed;
            OpponentName.Text = game.OpponentName;
            OpponentName.Visibility = Visibility.Visible;
            OpponentText.Visibility = Visibility.Visible;
            Grid.Visibility = Visibility.Visible;
            if (game.White == false)
            {
                RotateBoardAndPieces();
                ReceiveMove();
            }
        }

        private void BackgroundMatchFinder_DoWork(object sender, DoWorkEventArgs e)
        {
            MatchFinder matchFinder = new MatchFinder();
            game = matchFinder.FindMatch(playerGuid, playerName, backgroundMatchFinder).Result;
        }

        private void SetUsername()
        {
            if (File.Exists(path + "username"))
            {
                string name = File.ReadAllText(path + "username");
                NameTextBox.Text = name;
            }
        }

        private void SetupChessBoard()
        {
            allPieces = new List<Button> { BB1, BB2, BK, BN1, BN2, BP1, BP2, BP3, BP4, BP5, BP6, BP7, BP8, BQ, BR1, BR2, WB1, WB2, WK, WN1, WN2, WP1, WP2, WP3, WP4, WP5, WP6, WP7, WP8, WQ, WR1, WR2 };

            //To initialize and make actual first call faster
            board.Clone<Field[,]>();

            for (int row = 0; row < 8; row++)
            {
                for (int column = 0; column < 8; column++)
                {
                    Piece p = SetPieceOnField(column, row);
                    board[column, row] = new Field { Point = new Point(column, row), Piece = p };
                }
            }

            Dictionary<Button, Point> keyValuePairs = new Dictionary<Button, Point>()
            {
                {BB1, new Point(5,0) },
                {BB2, new Point(2,0) },
                {BK, new Point(4,0) },
                {BN1, new Point(6,0) },
                {BN2, new Point(1,0) },
                {BQ, new Point(3,0) },
                {BR1, new Point(0,0) },
                {BR2, new Point(7,0) },
                {BP1, new Point(0,1) },
                {BP2, new Point(1,1) },
                {BP3, new Point(2,1) },
                {BP4, new Point(3,1) },
                {BP5, new Point(4,1) },
                {BP6, new Point(5,1) },
                {BP7, new Point(6,1) },
                {BP8, new Point(7,1) },
                {WB1, new Point(2,7) },
                {WB2, new Point(5,7) },
                {WK, new Point(4,7) },
                {WN1, new Point(1,7) },
                {WN2, new Point(6,7) },
                {WQ, new Point(3,7) },
                {WR1, new Point(0,7) },
                {WR2, new Point(7,7) },
                {WP1, new Point(0,6) },
                {WP2, new Point(1,6) },
                {WP3, new Point(2,6) },
                {WP4, new Point(3,6) },
                {WP5, new Point(4,6) },
                {WP6, new Point(5,6) },
                {WP7, new Point(6,6) },
                {WP8, new Point(7,6) },
            };
            foreach (var piece in allPieces)
            {
                Grid.SetColumn(piece, keyValuePairs[piece].X);
                Grid.SetRow(piece, keyValuePairs[piece].Y);
                piece.Visibility = Visibility.Visible;
            }
        }


        private void ShowAvaibleMoves(Piece piece)
        {
            List<Point> validMoves = moves.GetValidMoves(piece, currentPos, board, whitesTurn, false, currentlyInCheck);

            foreach (var move in validMoves)
            {
                Button button = (Button)Grid.FindName("Field" + (move.X + 1) + "_" + (move.Y + 1));
                if (board[move.X, move.Y].Piece != Piece.Empty)
                {
                    button.Style = this.FindResource("OccupiedField") as Style;
                }

                button.Visibility = Visibility.Visible;
            }
            slctdBtn.Background = this.FindResource("SelectedBrush") as Brush;
        }

        private void Field_Click(object sender, RoutedEventArgs e)
        {
            Button clickedBtn = (Button)sender;
            newPos = new Point(Grid.GetColumn(clickedBtn), Grid.GetRow(clickedBtn));
            MakeMove();
            PlaySound();
            if (onlineGame == true)
            {
                PostMove();
                ReceiveMove();
            }
            whitesTurn = !whitesTurn;
            GameEndChecker.RunWorkerAsync();
        }

        private void ReceiveMove()
        {
            waitingForOpponent = true;
            receiveMove = new BackgroundWorker();
            receiveMove.DoWork += ReceiveMove_DoWork;
            receiveMove.RunWorkerAsync();
            receiveMove.RunWorkerCompleted += ReceiveMove_RunWorkerCompleted;
        }

        private void ReceiveMove_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            waitingForOpponent = false;
            newPos = opponentsMove.newPos;
            currentPos = opponentsMove.currentPos;
            slctdBtn = GetButton();
            MakeMove();
            PlaySound();
            whitesTurn = !whitesTurn;
            GameEndChecker.RunWorkerAsync();
        }

        private Button GetButton()
        {
            List<Button> buttons;
            if (game.White == true)
            {
                buttons = new List<Button> { BB1, BB2, BK, BN1, BN2, BP1, BP2, BP3, BP4, BP5, BP6, BP7, BP8, BQ, BR1, BR2 };
            }
            else
            {
                buttons = new List<Button> { WB1, WB2, WK, WN1, WN2, WP1, WP2, WP3, WP4, WP5, WP6, WP7, WP8, WQ, WR1, WR2 };
            }
            foreach (var button in buttons)
            {
                int column = Grid.GetColumn(button);
                int row = Grid.GetRow(button);
                if (column == currentPos.X && row == currentPos.Y && button.Visibility == Visibility.Visible)
                {
                    return button;
                }
            }
            return new Button();
        }

        private void ReceiveMove_DoWork(object sender, DoWorkEventArgs e)
        {
            opponentsMove = GameController.ReceiveMove(game.GameID, playerGuid).Result;
        }

        private void PostMove()
        {
            sendMove = new BackgroundWorker();
            sendMove.DoWork += SendMove_DoWork;
            sendMove.RunWorkerAsync();
        }

        private async void SendMove_DoWork(object sender, DoWorkEventArgs e)
        {
            while (await GameController.PostMove(game.GameID, playerGuid, currentPos, newPos) == false) ;
        }

        private void MakeMove()
        {
            HighlightFields();
            RemovePiece();
            SpecialRules();
            MovePiece();
            ResetHints();
            if (onlineGame == false)
            {
                RotateBoardAndPieces();
            }
        }

        private void SpecialRules()
        {
            Promoting();
            EnPassent();
            Castling();
        }

        private void Castling()
        {
            if (whitesTurn)
            {
                if (slctdBtn.Name == "WK")
                {
                    //Castle short
                    if (currentPos.X < newPos.X && newPos.X - currentPos.X == 2)
                    {
                        Grid.SetColumn(WR2, 5);
                        board[newPos.X - 1, newPos.Y].Piece = Piece.WhiteRook;
                        board[newPos.X + 1, newPos.Y].Piece = Piece.Empty;
                    }
                    //Castle long
                    else if (currentPos.X > newPos.X && currentPos.X - newPos.X == 2)
                    {
                        Grid.SetColumn(WR1, 3);
                        board[newPos.X + 1, newPos.Y].Piece = Piece.WhiteRook;
                        board[newPos.X - 2, newPos.Y].Piece = Piece.Empty;
                    }
                }
            }
            else
            {
                if (slctdBtn.Name == "BK")
                {
                    //Castle short
                    if (currentPos.X < newPos.X && newPos.X - currentPos.X == 2)
                    {
                        Grid.SetColumn(BR2, 5);
                        board[newPos.X - 1, newPos.Y].Piece = Piece.BlackRook;
                        board[newPos.X + 1, newPos.Y].Piece = Piece.Empty;
                    }
                    //Castle long
                    else if (currentPos.X > newPos.X && currentPos.X - newPos.X == 2)
                    {
                        Grid.SetColumn(BR1, 3);
                        board[newPos.X + 1, newPos.Y].Piece = Piece.BlackRook;
                        board[newPos.X - 2, newPos.Y].Piece = Piece.Empty;
                    }
                }
            }
        }

        private void Promoting()
        {
            if (slctdBtn.Name.Contains("WP") && newPos.Y == 0)
            {
                Image image = (Image)this.FindName(slctdBtn.Name + "Image");
                image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/WQ.png"));
                board[newPos.X, newPos.Y].Piece = Piece.WhiteQueen;
                promotedPieces.Add(slctdBtn.Name);

            }
            else if (slctdBtn.Name.Contains("BP") && newPos.Y == 7)
            {
                Image image = (Image)this.FindName(slctdBtn.Name + "Image");
                image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/BQ.png"));
                board[newPos.X, newPos.Y].Piece = Piece.BlackQueen;
                promotedPieces.Add(slctdBtn.Name);
            }
        }

        private void MovePiece()
        {
            Grid.SetColumn(slctdBtn, newPos.X);
            Grid.SetRow(slctdBtn, newPos.Y);
            board[newPos.X, newPos.Y].FieldActivated = true;
            board[newPos.X, newPos.Y].Piece = board[currentPos.X, currentPos.Y].Piece;
            board[currentPos.X, currentPos.Y].Piece = Piece.Empty;
        }

        private static Piece SetPieceOnField(int column, int row)
        {
            if ((column == 0 || column == 7) && row == 0)
                return Piece.BlackRook;

            if ((column == 1 || column == 6) && row == 0)
                return Piece.BlackKnight;

            if ((column == 2 || column == 5) && row == 0)
                return Piece.BlackBishop;

            if (column == 3 && row == 0)
                return Piece.BlackQueen;

            if (column == 4 && row == 0)
                return Piece.BlackKing;

            if (row == 1)
                return Piece.BlackPawn;


            if ((column == 0 || column == 7) && row == 7)
                return Piece.WhiteRook;

            if ((column == 1 || column == 6) && row == 7)
                return Piece.WhiteKnight;

            if ((column == 2 || column == 5) && row == 7)
                return Piece.WhiteBishop;

            if (column == 3 && row == 7)
                return Piece.WhiteQueen;

            if (column == 4 && row == 7)
                return Piece.WhiteKing;

            if (row == 6)
                return Piece.WhitePawn;

            return Piece.Empty;
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
        private void PlaySound()
        {
            if (board[newPos.X, newPos.Y].Piece != Piece.Empty)
                PlaySound(Properties.Resources.Capture);
            else
                PlaySound(Properties.Resources.Move);
        }

        private void EnPassent()
        {
            foreach (var field in board)
            {
                if (field.DoubleMoved == true)
                {
                    if (doubleMoveForEnPassent == true)
                    {
                        board[field.Point.X, field.Point.Y].DoubleMoved = false;
                        doubleMoveForEnPassent = false;
                    }
                    else
                    {
                        doubleMoveForEnPassent = true;
                    }
                }
            }

            if (slctdBtn.Name.Contains("BP"))
            {
                Point currentPos = new Point(Grid.GetColumn(slctdBtn), Grid.GetRow(slctdBtn));
                if (newPos.Y - currentPos.Y == 2)
                {
                    board[newPos.X, newPos.Y].DoubleMoved = true;
                }
            }
            else if (slctdBtn.Name.Contains("WP"))
            {
                Point currentPos = new Point(Grid.GetColumn(slctdBtn), Grid.GetRow(slctdBtn));
                if (currentPos.Y - newPos.Y == 2)
                {
                    board[newPos.X, newPos.Y].DoubleMoved = true;
                }
            }
        }

        private void HighlightFields()
        {
            var currentPos = new Point(this.currentPos.X + 1, this.currentPos.Y + 1);
            var newPos = new Point(this.newPos.X + 1, this.newPos.Y + 1);

            //Reset check
            WK.Background = Brushes.Transparent;
            BK.Background = Brushes.Transparent;
            lastSelectedPiece.Background = this.FindResource("DefaultBrush") as Brush;
            foreach (var field in markedFields)
            {
                var button = Grid.FindName("Hint" + field.X + "_" + field.Y) as Shape;
                button.SetValue(Shape.FillProperty, DependencyProperty.UnsetValue);
            }
            markedFields.Clear();

            (Grid.FindName("Hint" + currentPos.X + "_" + currentPos.Y) as System.Windows.Shapes.Rectangle).Fill = Brushes.MediumSpringGreen;
            markedFields.Add(currentPos);

            (Grid.FindName("Hint" + newPos.X + "_" + newPos.Y) as System.Windows.Shapes.Rectangle).Fill = Brushes.SpringGreen;
            markedFields.Add(newPos);
        }

        private void CheckForGameEnd(object sender, DoWorkEventArgs e)
        {
            bool wT = !whitesTurn;
            currentlyInCheck = Check.IsCheck(board, wT, currentlyInCheck);
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
                //Check for stalemate
                if (AtLeastOneMove(wT) == false)
                {
                    Dispatcher.BeginInvoke(new Action(() => GameEndingAnimation("")));
                }
            }
        }

        private void GameEndingAnimation(string player)
        {
            Grid.Effect = new BlurEffect();
            OpponentName.Visibility = Visibility.Collapsed;
            OpponentText.Visibility = Visibility.Collapsed;
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
            sb.Storyboard.Completed += Storyboard_Completed;
            sb.Storyboard.Begin();
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            EndScreenGrid.BeginAnimation(Grid.WidthProperty, null);
            EndScreenGrid.Width = double.NaN;
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
                    var legalMoves = moves.GetValidMoves(field.Piece, field.Point, board, !wT, false, currentlyInCheck);
                    if (legalMoves.Count > 0)
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
            if (whitesTurn)
            {
                btnsAnimation = new DoubleAnimation(0, 180, new Duration(TimeSpan.FromSeconds(0.5)));
                gridAnimation = new DoubleAnimation(0, 180, new Duration(TimeSpan.FromSeconds(0.5)));
            }
            else
            {
                btnsAnimation = new DoubleAnimation(180, 0, new Duration(TimeSpan.FromSeconds(0.5)));
                gridAnimation = new DoubleAnimation(180, 0, new Duration(TimeSpan.FromSeconds(0.5)));
            }


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

        private void RemovePiece()
        {
            int y = 1;
            if (whitesTurn == false)
            {
                y = -1;
            }

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
                    if (newPos.X == Grid.GetColumn(button) && newPos.Y == Grid.GetRow(button))
                    {
                        button.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        RemovePieceWhenEnPassent(newPos, y, button);
                    }
                }
            }
        }

        private void RemovePieceWhenEnPassent(Point p, int y, Button button)
        {
            Point pawnPos = new Point(-1, -1);
            foreach (var field in board)
            {
                if (field.DoubleMoved == true)
                {
                    pawnPos = field.Point;
                }
            }
            if (pawnPos.X != -1 && pawnPos.Y != -1)
            {
                var column = Grid.GetColumn(button);
                var row = Grid.GetRow(button);
                if (column == p.X && p.Y + y == row && pawnPos.X == p.X && pawnPos.Y == p.Y + y)
                {
                    //Capture pawn because it is on a different field when en passent happens
                    button.Visibility = Visibility.Collapsed;
                    board[column, row].Piece = Piece.Empty;
                }
            }
        }

        private void Piece_Click(object sender, RoutedEventArgs e)
        {
            if (onlineGame == false || (onlineGame == true && waitingForOpponent == false))
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

                currentPos.X = Grid.GetColumn(slctdBtn);
                currentPos.Y = Grid.GetRow(slctdBtn);

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
                        if (!promotedPieces.Contains(slctdBtn.Name))
                        {
                            ShowAvaibleMoves(Piece.BlackPawn);
                        }
                        else
                        {
                            ShowAvaibleMoves(Piece.BlackQueen);
                        }
                    }
                }
                else
                {
                    if (slctdBtn.Name.Contains("WP"))
                    {
                        if (!promotedPieces.Contains(slctdBtn.Name))
                        {
                            ShowAvaibleMoves(Piece.WhitePawn);
                        }
                        else
                        {
                            ShowAvaibleMoves(Piece.WhiteQueen);
                        }
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
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => this.Close());
        }

        private void NewGameBtn_Click(object sender, RoutedEventArgs e)
        {
            Grid.Visibility = Visibility.Collapsed;
            SetupChessBoard();
            ResetEndScreen();
            ResetHighlights();
            ResetVariables();
            ResetBoardRotation();
        }

        private void ResetBoardRotation()
        {
            Grid.RenderTransform = new RotateTransform(0);
            foreach (var button in allPieces)
            {
                button.RenderTransform = new RotateTransform(0);
                button.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            }
        }

        private void ResetVariables()
        {
            currentPos = new Point(0, 0);
            newPos = new Point(0, 0);
            promotedPieces = new List<string>();
            lastSelectedPiece = new Button();
            currentlyInCheck = false;
            doubleMoveForEnPassent = false;
            whitesTurn = true;
            onlineGame = false;
            waitingForOpponent = false;
            markedFields.Clear();
            game = new GameEntity();
        }

        private void ResetHighlights()
        {
            WK.Background = Brushes.Transparent;
            BK.Background = Brushes.Transparent;
            Menu.Visibility = Visibility.Visible;
            lastSelectedPiece.Background = this.FindResource("DefaultBrush") as Brush;
            foreach (var field in markedFields)
            {
                var button = Grid.FindName("Hint" + field.X + "_" + field.Y) as Shape;
                button.SetValue(Shape.FillProperty, DependencyProperty.UnsetValue);
            }
        }

        private void ResetEndScreen()
        {
            EndScreenGrid.Visibility = Visibility.Collapsed;
            EndScreenGrid.Width = 0;
            Grid.Effect = null;
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NameTextBox.Text) && NameTextBox.Text != "Anonymous")
            {
                Directory.CreateDirectory(path);
                File.WriteAllText(path + "username", NameTextBox.Text);
            }
        }

        private void StartOnlineGame_Click(object sender, RoutedEventArgs e)
        {
            if (ProgressRing.Visibility != Visibility.Visible)
            {
                onlineGame = true;
                playerName = NameTextBox.Text;
                StartOnlineGame.Background = Brushes.Red;
                StartOnlineGame.Content = "Cancel search";
                ProgressRing.Visibility = Visibility.Visible;
                StartBackgroundworker();
            }
            else
            {
                CancelSearch();
            }
        }

        private void CancelSearch()
        {
            //Cancel search
            onlineGame = false;
            StartOnlineGame.Background = Brushes.DarkSlateBlue;
            StartOnlineGame.Content = "Online game";
            ProgressRing.Visibility = Visibility.Collapsed;
            backgroundMatchFinder.CancelAsync();
            StartOnlineGame.IsEnabled = false;
        }

        private void StartBackgroundworker()
        {
            backgroundMatchFinder = new BackgroundWorker();
            backgroundMatchFinder.RunWorkerCompleted += BackgroundMatchFinder_RunWorkerCompleted;
            backgroundMatchFinder.DoWork += BackgroundMatchFinder_DoWork;
            backgroundMatchFinder.WorkerSupportsCancellation = true;
            backgroundMatchFinder.RunWorkerAsync();
        }

        private void StartLocalGame_Click(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Visibility.Collapsed;
            Grid.Visibility = Visibility.Visible;
        }


        void CreateShortcuts()
        {
            CreateDesktopShortcut();
            CreateStartMenuShortcut();
        }

        void CreateDesktopShortcut()
        {
            string pathToExe = Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe";
            if (pathToExe == Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\eChess\\" + currentVersion + "\\eChess.exe")
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutLocation = System.IO.Path.Combine(desktop, "eChess" + ".lnk");
                if (!File.Exists(shortcutLocation))
                {
                    WshInterop.CreateShortcut(shortcutLocation, "eChess", pathToExe, null, null);
                }
            }
        }

        private void CreateStartMenuShortcut()
        {
            string pathToExe = Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe";
            if (pathToExe == Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\eChess\\" + currentVersion + "\\eChess.exe")
            {
                string shortcutLocation = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\Programs\\", "eChess" + ".lnk");
                if (!File.Exists(shortcutLocation))
                {
                    WshInterop.CreateShortcut(shortcutLocation, null, pathToExe, null, null);
                }
            }
        }
    }
}
