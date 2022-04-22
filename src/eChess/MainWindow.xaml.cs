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
using System.Diagnostics;
using MessageBox = ModernWpf.MessageBox;
using System.Threading;

namespace eChess
{
    public partial class MainWindow : Window
    {
        readonly string currentVersion = "v1.8";
        static readonly int abortTime = 225300;
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
        bool gameEnded;
        int moveIndex = 0;
        string PGN = string.Empty;
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
        readonly TimerModel timerModel = new TimerModel();
        System.Windows.Forms.Timer localTimer = new System.Windows.Forms.Timer();
        DateTime startDateTime;


        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(path);
            Timer.DataContext = timerModel;
            SetupChessBoard();
            SetUsername();
            Update();
            DeleteOldFilesAndShortcuts();
            CreateNewShortcuts();
            GameEndChecker.DoWork += CheckForGameEnd;
        }

        private void StartLocalTimer()
        {
            localTimer.Tick += LocalTimer_Tick;
            startDateTime = DateTime.Now;
            localTimer.Start();
        }

        private void ResetLocalTimer()
        {
            localTimer.Stop();
            localTimer.Dispose();
            localTimer = new System.Windows.Forms.Timer();
        }

        private void LocalTimer_Tick(object sender, EventArgs e)
        {
            var time = TimeSpan.FromMilliseconds(abortTime) - DateTime.Now.Subtract(startDateTime);
            if (time.TotalMilliseconds < 1)
            {
                timerModel.Timer = "00:00";
                Thread handleTimeForfeit = new Thread(HandleTimeForfeit);
                handleTimeForfeit.Start();
            }
            else
            {
                if (time < TimeSpan.FromSeconds(20))
                {
                    if (localTimer.Interval != 25)
                    {
                        localTimer.Interval = 25;
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Activate();
                            Topmost = true;
                            Topmost = false;
                            Focus();
                            Timer.Foreground = Brushes.OrangeRed;
                        }));
                    }
                    timerModel.Timer = string.Format("{0:ss\\:ff}", time);
                }
                else
                {
                    if (localTimer.Interval != 1000)
                    {
                        Timer.Foreground = Brushes.LightGray;
                        localTimer.Interval = 1000;
                    }
                    timerModel.Timer = string.Format("{0:mm\\:ss}", time);
                }
            }
        }

        private void HandleTimeForfeit()
        {
            ResetLocalTimer();
            Dispatcher.BeginInvoke(new Action(() => GameEndingAnimation(string.Empty, true)));
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
            Timer.Visibility = Visibility.Visible;
            OpponentText.Visibility = Visibility.Visible;
            Grid.Visibility = Visibility.Visible;
            gameEnded = false;
            EnableAllPieces();
            ResetHints();
            if (game.White == false)
            {
                DisableAllPieces();
                RotateBoardAndPieces();
                ReceiveMove();
                ResetVisualTimer();
            }
            else
            {
                StartLocalTimer();
                SwitchEnabledPieces(!game.White);
            }
        }

        private void ResetVisualTimer()
        {
            timerModel.Timer = string.Format("{0:mm\\:ss}", TimeSpan.FromMilliseconds(abortTime));
            Timer.Foreground = Brushes.LightGray;
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

            Dictionary<Button, Point> keyValuePairs = ButtonPieces.GetPositions(BB1, BB2, BK, BN1, BN2, BQ, BR1, BR2, BP1, BP2, BP3, BP4, BP5, BP6, BP7, BP8, WB1, WB2, WK, WN1, WN2, WQ, WR1, WR2, WP1, WP2, WP3, WP4, WP5, WP6, WP7, WP8);
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
            PiecesOfButtons = ButtonPieces.GetPieces(BB1, BB2, BK, BN1, BN2, BQ, BR1, BR2, BP1, BP2, BP3, BP4, BP5, BP6, BP7, BP8, WB1, WB2, WK, WN1, WN2, WQ, WR1, WR2, WP1, WP2, WP3, WP4, WP5, WP6, WP7, WP8);

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
            if (onlineGame)
            {
                lastSelectedPiece = new Button();
                ShowSandClock();
                ResetVisualTimer();
                ResetLocalTimer();
                PostMove();
                ReceiveMove();
                EnableOrDisableOwnPieces(false);
            }
            else
            {
                RotateBoardAndPieces();
                SwitchEnabledPieces(whitesTurn);
            }
            whitesTurn = !whitesTurn;
            GameEndChecker.RunWorkerAsync();
        }

        private void ShowSandClock()
        {
            SandClock.Visibility = Visibility.Visible;
            BeginStoryboard sb = this.FindResource("FadeIn") as BeginStoryboard;
            sb.Storyboard.Completed += FadeInSb_Completed;
            sb.Storyboard.Begin();
        }

        private void FadeInSb_Completed(object sender, EventArgs e)
        {
            SandClock.Visibility = Visibility.Visible;
        }

        private void HideSandClock()
        {
            BeginStoryboard sb = this.FindResource("FadeOut") as BeginStoryboard;
            sb.Storyboard.Completed += FadeOutSb_Completed;
            sb.Storyboard.Begin();
        }

        private void FadeOutSb_Completed(object sender, EventArgs e)
        {
            SandClock.Visibility = Visibility.Collapsed;
        }

        private void PlaySound_DoWork(object sender, DoWorkEventArgs e)
        {
            if (board[newPos.X, newPos.Y].Piece != Piece.Empty)
                PlaySound(Properties.Resources.Capture);
            else
                PlaySound(Properties.Resources.Move);
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
            ShowSandClock();
            receiveMove = new BackgroundWorker();
            receiveMove.DoWork += ReceiveMove_DoWork;
            receiveMove.RunWorkerAsync(game.GameID);
            receiveMove.RunWorkerCompleted += ReceiveMove_RunWorkerCompleted;
        }

        private void ReceiveMove_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
        {
            HideSandClock();
            if ((Guid)e.Result == game.GameID)
            {
                if (opponentsMove.currentPos.X == 44 && opponentsMove.newPos.X == 44)
                {
                    if (gameEnded == false)
                    {
                        GameEndingAnimation(string.Empty, true);
                    }
                }
                else if (gameEnded == false)
                {
                    StartLocalTimer();
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
            e.Result = e.Argument;
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
            PortableGameNotation.PGN_Writer(board, currentPos, newPos, whitesTurn, ref moveIndex, ref PGN);
            HighlightFields();
            CapturePiece();
            SpecialRules();
            MovePiece();
            ResetHints();
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
                        PortableGameNotation.PGN_Castling("O-O ", ref PGN);
                        Grid.SetColumn(WR2, 5);
                        board[newPos.X - 1, newPos.Y].Piece = Piece.WhiteRook;
                        board[newPos.X + 1, newPos.Y].Piece = Piece.Empty;
                    }
                    //Castle long
                    else if (currentPos.X > newPos.X && currentPos.X - newPos.X == 2)
                    {
                        PortableGameNotation.PGN_Castling("O-O-O ", ref PGN);
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
                        PortableGameNotation.PGN_Castling("O-O ", ref PGN);
                        Grid.SetColumn(BR2, 5);
                        board[newPos.X - 1, newPos.Y].Piece = Piece.BlackRook;
                        board[newPos.X + 1, newPos.Y].Piece = Piece.Empty;
                    }
                    //Castle long
                    else if (currentPos.X > newPos.X && currentPos.X - newPos.X == 2)
                    {
                        PortableGameNotation.PGN_Castling("O-O-O ", ref PGN);
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
            BackgroundWorker playSound = new BackgroundWorker();
            playSound.DoWork += PlaySound_DoWork;
            playSound.RunWorkerAsync();
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
            RetrievePGN();
            DisableAllPieces();
            ResetLocalTimer();

            Grid.Effect = new BlurEffect();
            HideSandClock();
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

        private void RetrievePGN()
        {
            try
            {
                if (onlineGame == true)
                {
                    string pgn = GameController.GetPGN(game.GameID).Result;
                    if (pgn.StartsWith("[Event"))
                    {
                        PGN = pgn;
                    }
                }
            }
            catch
            {

            }
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
                btnsAnimation = new DoubleAnimation(0, 180, new Duration(TimeSpan.FromSeconds(0.7)));
                gridAnimation = new DoubleAnimation(0, 180, new Duration(TimeSpan.FromSeconds(0.7)));
            }
            else
            {
                btnsAnimation = new DoubleAnimation(180, 0, new Duration(TimeSpan.FromSeconds(0.7)));
                gridAnimation = new DoubleAnimation(180, 0, new Duration(TimeSpan.FromSeconds(0.7)));
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
            PGN = string.Empty;
            moveIndex = 0;
            game = new GameEntity();
            Timer.Visibility = Visibility.Collapsed;
            CopyGameBtn.Content = "📋";
            CopyGameBtn.IsChecked = false;
            CopyGameBtn.IsEnabled = true;
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
            if (NameTextBox.IsFocused == true)
            {
                string name = NameTextBox.Text;
                if (CharactersAllowed(name))
                {
                    playerName = name;
                    Directory.CreateDirectory(path);
                    File.WriteAllText(path + "username", name);
                }
                else
                {
                    NameTextBox.Text = playerName;
                    NameTextBox.CaretIndex = name.Length;
                }
            }
        }

        private bool CharactersAllowed(string name)
        {
            string allowableLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890-_.";
            foreach (char c in name)
            {
                if (!allowableLetters.Contains(c.ToString()))
                    return false;
            }
            return true;

        }

        private string IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Your username cannot be empty.";
            }
            else if (name.Length > 15)
            {
                return "Your username must not exceed 15 characters.";
            }
            else if (name.Length < 3)
            {
                return "Your username must consist of at least 3 characters.";
            }
            else
            {
                return "Valid";
            }
        }

        private void StartOnlineGame_Click(object sender, RoutedEventArgs e)
        {
            if (ProgressRing.Visibility != Visibility.Visible)
            {
                playerName = NameTextBox.Text;
                string result = IsValidName(playerName);
                if (result == "Valid")
                {
                    onlineGame = true;
                    StartOnlineGame.Background = Brushes.Red;
                    StartOnlineGame.Content = "Cancel search";
                    ProgressRing.Visibility = Visibility.Visible;
                    StartBackgroundworker();
                }
                else
                {
                    MessageBox.Show(result, "Username is not valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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


        private void DeleteOldFilesAndShortcuts()
        {
            DeleteOldVersions();
            DeleteOldDesktopShortcuts();
            DeleteOldStartMenuShortcut();
        }

        private void DeleteOldVersions()
        {
            var directories = Directory.GetDirectories(path);
            foreach (string directory in directories)
            {
                string name = directory.Replace(path, "");
                if (name.StartsWith("v") && name.Contains(".") && name != currentVersion)
                {
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void DeleteOldDesktopShortcuts()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var files = Directory.GetFiles(desktop);
            foreach (string file in files)
            {
                string name = System.IO.Path.GetFileName(file);
                if (name.Contains("eChess v") && !name.Contains(currentVersion))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void DeleteOldStartMenuShortcut()
        {
            string startMenuFolder = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\Programs\\";
            var files = Directory.GetFiles(startMenuFolder);
            foreach (string file in files)
            {
                string name = System.IO.Path.GetFileName(file);
                if (name.Contains("eChess v") && !name.Contains(currentVersion))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void CreateNewShortcuts()
        {
            CreateDesktopShortcut();
            CreateStartMenuShortcut();
        }

        private void CreateDesktopShortcut()
        {
            string pathToExe = path + currentVersion + "\\eChess.exe";
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
            string pathToExe = path + currentVersion + "\\eChess.exe";
            if (File.Exists(pathToExe))
            {
                string shortcutLocation = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\Programs\\", "eChess " + currentVersion + ".lnk");
                if (!File.Exists(shortcutLocation))
                {
                    WshInterop.CreateShortcut(shortcutLocation, null, pathToExe, null, null);
                }
            }
        }

        private void CopyGameBtn_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker handleClick = new BackgroundWorker();
            handleClick.DoWork += HandleClick_DoWork;
            handleClick.RunWorkerCompleted += HandleClick_RunWorkerCompleted;
            handleClick.RunWorkerAsync();
        }

        private void HandleClick_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var result = MessageBox.Show("Do you want to open lichess.org to import your game?", "Portable game notation was copied", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Process.Start("https://lichess.org/paste");
            }
        }

        private void HandleClick_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(500);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CopyGameBtn.IsEnabled = false;
                string serverPGN = GameController.GetPGN(game.GameID).Result;
                if (serverPGN.Length > 100)
                {
                    PGN = serverPGN;
                }
                Clipboard.SetText(PGN);
            }));
        }
    }
}
