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
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Image = System.Windows.Controls.Image;
using System.Windows.Media.Imaging;
using eChessServer.Entities;
using DK.WshRuntime;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace eChess
{
    public partial class MainWindow : Window
    {
        readonly string currentVersion = "v1.6";
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
        Dictionary<Button, Piece> PiecesOfButtons = new Dictionary<Button, Piece>();
        readonly MatchFinder matchFinder = new MatchFinder();
        readonly System.Timers.Timer abortTimer = new System.Timers.Timer(250000);
        bool gameEnded;


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
                onlineGame = true;
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
            abortTimer.Elapsed += Timeout_Elapsed;
            gameEnded = false;
            EnableAllPieces();
            ResetHints();
            if (game.White == false)
            {
                DisableAllPieces();
                RotateBoardAndPieces();
                ReceiveMove();
            }
            else
            {
                abortTimer.Start();
                SwitchEnabledPieces(!game.White);
            }
        }

        private void BackgroundMatchFinder_DoWork(object sender, DoWorkEventArgs e)
        {
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
                if (promotedPieces.Contains(piece.Name))
                {
                    Image image = (Image)this.FindName(piece.Name + "Image");
                    if (piece.Name[0] == char.Parse("W"))
                    {
                        image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/WP.png"));
                    }
                    else
                    {
                        image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/BP.png"));
                    }
                }
            }
            PiecesOfButtons = new Dictionary<Button, Piece>
            {
                { BB1, Piece.BlackBishop },
                { BB2, Piece.BlackBishop },
                { BK, Piece.BlackKing},
                { BN1, Piece.BlackKnight},
                { BN2, Piece.BlackKnight},
                { BQ, Piece.BlackQueen},
                { BR1, Piece.BlackRook},
                { BR2, Piece.BlackRook},
                { BP1, Piece.BlackPawn},
                { BP2, Piece.BlackPawn},
                { BP3, Piece.BlackPawn},
                { BP4, Piece.BlackPawn},
                { BP5, Piece.BlackPawn},
                { BP6, Piece.BlackPawn},
                { BP7, Piece.BlackPawn},
                { BP8, Piece.BlackPawn},
                { WB1, Piece.WhiteBishop},
                { WB2, Piece.WhiteBishop},
                { WK, Piece.WhiteKing},
                { WN1, Piece.WhiteKnight},
                { WN2, Piece.WhiteKnight},
                { WQ, Piece.WhiteQueen},
                { WR1, Piece.WhiteRook},
                { WR2, Piece.WhiteRook},
                { WP1, Piece.WhitePawn},
                { WP2, Piece.WhitePawn},
                { WP3, Piece.WhitePawn},
                { WP4, Piece.WhitePawn},
                { WP5, Piece.WhitePawn},
                { WP6, Piece.WhitePawn},
                { WP7, Piece.WhitePawn},
                { WP8, Piece.WhitePawn},
            };
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
        }

        private void Field_Click(object sender, RoutedEventArgs e)
        {
            Button clickedBtn = (Button)sender;
            newPos = new Point(Grid.GetColumn(clickedBtn), Grid.GetRow(clickedBtn));
            PlaySound();
            MakeMove();
            if (onlineGame == true)
            {
                lastSelectedPiece = new Button();
                abortTimer.Stop();
                PostMove();
                ReceiveMove();
                EnableOrDisableOwnPieces(false);
            }
            else
            {
                SwitchEnabledPieces(whitesTurn);
            }
            whitesTurn = !whitesTurn;
            GameEndChecker.RunWorkerAsync();
        }

        private void EnableOrDisableOwnPieces(bool enabled)
        {
            string color = "W";
            if (game.White == false)
            {
                color = "B";
            }
            foreach (var piece in allPieces)
            {
                if (piece.Name.StartsWith(color))
                {
                    piece.IsEnabled = enabled;
                }
            }
        }

        private void DisableAllPieces()
        {
            foreach (var piece in allPieces)
            {
                piece.IsEnabled = false;
            }
        }

        private void EnableAllPieces()
        {
            foreach (var piece in allPieces)
            {
                piece.IsEnabled = true;
            }
        }

        private void SwitchEnabledPieces(bool whoseTurn)
        {
            string color = "W";
            if (whoseTurn == false)
            {
                color = "B";
            }
            foreach (var piece in allPieces)
            {
                if (piece.Name.StartsWith(color))
                {
                    piece.IsEnabled = false;
                }
                else
                {
                    piece.IsEnabled = true;
                }
            }
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
            if (opponentsMove.currentPos.X == 44 && opponentsMove.newPos.X == 44)
            {
                if (gameEnded == false)
                {
                    GameEndingAnimation(string.Empty, true);
                }
            }
            else if(gameEnded == false)
            {
                abortTimer.Start();
                waitingForOpponent = false;
                newPos = opponentsMove.newPos;
                currentPos = opponentsMove.currentPos;
                slctdBtn = GetButton();
                PlaySound();
                MakeMove();
                EnableOrDisableOwnPieces(true);
                whitesTurn = !whitesTurn;
                GameEndChecker.RunWorkerAsync();
            }
        }

        private void Timeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => GameEndingAnimation(string.Empty, true)));
            abortTimer.Stop();
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
            if (await GameController.PostMove(game.GameID, playerGuid, currentPos, newPos) == false)
            {
                MessageBox.Show("Move could not be posted.");
            }
        }

        private void MakeMove()
        {
            HighlightFields();
            CapturePiece();
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
                board[currentPos.X, currentPos.Y].Piece = Piece.WhiteQueen;
                promotedPieces.Add(slctdBtn.Name);
                PiecesOfButtons[slctdBtn] = Piece.WhiteQueen;

            }
            else if (slctdBtn.Name.Contains("BP") && newPos.Y == 7)
            {
                Image image = (Image)this.FindName(slctdBtn.Name + "Image");
                image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/BQ.png"));
                board[currentPos.X, currentPos.Y].Piece = Piece.BlackQueen;
                promotedPieces.Add(slctdBtn.Name);
                PiecesOfButtons[slctdBtn] = Piece.BlackQueen;
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
                        doubleMoveForEnPassent = false;
                    }
                    else
                    {
                        foreach (var f in board)
                        {
                            f.DoubleMoved = false;
                        }
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
                    string player = string.Empty;
                    if (onlineGame == false)
                    {
                        player = "Black";
                        if (wT)
                        {
                            player = "White";
                        }
                    }
                    else
                    {
                        player = game.OpponentName;
                        if (waitingForOpponent == true)
                        {
                            player = playerName;
                        }
                    }
                    Dispatcher.BeginInvoke(new Action(() => GameEndingAnimation(player, false)));
                }
            }
            else
            {
                //Check for stalemate
                if (AtLeastOneMove(wT) == false)
                {
                    Dispatcher.BeginInvoke(new Action(() => GameEndingAnimation("", false)));
                }
            }
        }

        private void GameEndingAnimation(string player, bool aborted)
        {
            gameEnded = true;
            DisableAllPieces();
            Grid.Effect = new BlurEffect();
            OpponentName.Visibility = Visibility.Collapsed;
            OpponentText.Visibility = Visibility.Collapsed;
            if (aborted == false)
            {
                if (String.IsNullOrEmpty(player))
                {
                    ReasonText.Text = "by stalemate";
                    WinnerText.Text = "Draw";
                }
                else
                {
                    ReasonText.Text = "by checkmate";
                    if (onlineGame == false)
                    {
                        WinnerText.Text = player + " won";
                    }
                    else
                    {
                        if (waitingForOpponent == true)
                        {
                            WinnerText.Text = "You won";
                        }
                        else
                        {
                            WinnerText.Text = "You lost";
                        }
                    }
                }
            }
            else
            {
                if (waitingForOpponent == true)
                {
                    WinnerText.Text = "You won";
                    ReasonText.Text = "Opponent aborted the game";
                }
                else
                {
                    WinnerText.Text = "You lost";
                    ReasonText.Text = "You aborted the game";
                }
            }
            Activate();
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

        private void CapturePiece()
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
                    if (newPos.X == Grid.GetColumn(button) && newPos.Y == Grid.GetRow(button))
                    {
                        button.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        CaptureEnPassentPawn(button);
                    }
                }
            }
        }

        private void CaptureEnPassentPawn(Button button)
        {
            int y = 1;
            if (whitesTurn == false)
            {
                y = -1;
            }
            Point pawn = new Point(Grid.GetColumn(button), Grid.GetRow(button));
            Piece movedPiece = board[currentPos.X, currentPos.Y].Piece;
            if (movedPiece == Piece.BlackPawn || movedPiece == Piece.WhitePawn)
            {
                Point enemyPawn = Point.Empty;
                foreach (var field in board)
                {
                    if (field.DoubleMoved == true)
                    {
                        enemyPawn = field.Point;
                        break;
                    }
                }
                if (enemyPawn != Point.Empty)
                {
                    if (pawn.X == newPos.X && newPos.Y + y == pawn.Y && pawn.X == newPos.X && pawn.Y == newPos.Y + y)
                    {
                        //Capture pawn because it is on a different field when en passent happens
                        board[enemyPawn.X, enemyPawn.Y].Piece = Piece.Empty;
                        button.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void Piece_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            btn.Background = this.FindResource("SelectedBrush") as Brush;
            if (lastSelectedPiece != btn)
            {
                if (onlineGame == false || (onlineGame == true && waitingForOpponent == false))
                {
                    slctdBtn = btn;

                    if (lastSelectedPiece != null)
                    {
                        if (currentlyInCheck == true)
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
                    ShowAvaibleMoves(PiecesOfButtons[slctdBtn]);
                }
            }
            else
            {
                btn.Background = Brushes.Transparent;
                lastSelectedPiece = new Button();
                ResetHints();

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
            backgroundMatchFinder.CancelAsync();
            onlineGame = false;
            StartOnlineGame.Background = Brushes.DarkSlateBlue;
            StartOnlineGame.Content = "Online game";
            ProgressRing.Visibility = Visibility.Collapsed;
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
            if (backgroundMatchFinder.IsBusy == true)
            {
                CancelSearch();
            }
            gameEnded = false;
            Menu.Visibility = Visibility.Collapsed;
            Grid.Visibility = Visibility.Visible;
            EnableAllPieces();
            SwitchEnabledPieces(!whitesTurn);
        }


        void CreateShortcuts()
        {
            CreateDesktopShortcut();
            CreateStartMenuShortcut();
        }

        void CreateDesktopShortcut()
        {
            string pathToExe = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\eChess\\" + currentVersion + "\\eChess.exe";
            if (File.Exists(pathToExe))
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutLocation = System.IO.Path.Combine(desktop, "eChess " + currentVersion + ".lnk");
                if (!File.Exists(shortcutLocation))
                {
                    WshInterop.CreateShortcut(shortcutLocation, "eChess", pathToExe, null, null);
                }
            }
        }

        private void CreateStartMenuShortcut()
        {
            string pathToExe = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\eChess\\" + currentVersion + "\\eChess.exe";
            if (File.Exists(pathToExe))
            {
                string shortcutLocation = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\Programs\\", "eChess " + currentVersion + ".lnk");
                if (!File.Exists(shortcutLocation))
                {
                    WshInterop.CreateShortcut(shortcutLocation, null, pathToExe, null, null);
                }
            }
        }
    }
}
