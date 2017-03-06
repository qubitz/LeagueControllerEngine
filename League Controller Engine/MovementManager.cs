using System;
using System.ComponentModel;
using System.Threading;
using WindowsInput;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace League_Controller_Engine
{
    public partial class MovementManager : Component
    {
        private const bool DEBUG = true;

        // Class variables
        private MouseSimulator Mouse;

        private static double PrimaryScreenWidth  = System.Windows.SystemParameters.PrimaryScreenWidth;
        private static double PrimaryScreenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;

        private Location CursorOrigin;
        private Location CurrentCursorLocation;
        private const double MID_SCREEN_COORD = 32767.5;
        private const int PERIOD_MAX = 100;

        private static object CursorMoveRequestsLock;
        private static object ChampionMoveRequestsLock;
        private static object MovingCursorLock;

        private CursorMovementRequest CurrentCursorMoveRequest;
        private CursorMovementRequest PreviousCursorMoveRequest;

        private ChampionMovementRequest CurrentChampMoveRequest;
        private ChampionMovementRequest PreviousChampMoveRequest;

        private static Thread MovementHandlerThread;

        public static MovementManager Instance
        {
            get
            {
                if (PrivateInstance == null)
                {
                    PrivateInstance = new MovementManager();
                }

                return PrivateInstance;
            }
        }       
        private static MovementManager PrivateInstance;

        private MovementManager()
        {
            InitializeComponent();

            // Cursor presets
            CursorOrigin = new Location(MID_SCREEN_COORD, MID_SCREEN_COORD);

            // Subscribed events
            XControllerManager xcm = XControllerManager.Instance;
            xcm.LeftStickMoved  += new EventHandler<StickMovedEventArgs>(OnLeftStickMoved);
            xcm.RightStickMoved += new EventHandler<StickMovedEventArgs>(OnRightStickMoved);

            // Mouse stuff
            Mouse = new MouseSimulator(new InputSimulator());
            CurrentCursorLocation = CursorOrigin;

            // Restrictors
            MovingCursorLock = new object();
            CursorMoveRequestsLock = new object();
            ChampionMoveRequestsLock = new object();
            MovementHandlerThread = new Thread(HandleMovementRequests);
            MovementHandlerThread.Start();

            // Requests
            StickMovedEventArgs temp = new StickMovedEventArgs(0, 0, 0, true);
            CurrentCursorMoveRequest  = new CursorMovementRequest(temp, 1);
            PreviousCursorMoveRequest = new CursorMovementRequest(temp, 1);
            CurrentChampMoveRequest = new ChampionMovementRequest(temp, 1);
            PreviousChampMoveRequest = new ChampionMovementRequest(temp, 1);
        }
        
        // Thread loop to restrict frequency of cursor outputs
        private void HandleMovementRequests()
        {
            bool isRightStale, isRightAtRest;
            bool isLeftStale, isLeftAtRest;
            CursorMovementRequest currCursorReqest;
            ChampionMovementRequest currChampRequest;
            int champMoveTimer = PERIOD_MAX;

            while (true)
            {
                lock (CursorMoveRequestsLock)
                {
                    isRightStale = (CurrentCursorMoveRequest == PreviousCursorMoveRequest);
                    isRightAtRest = CurrentCursorMoveRequest.StickEventData.WasReleased;
                    currCursorReqest = CurrentCursorMoveRequest;

                    PreviousCursorMoveRequest = CurrentCursorMoveRequest;
                }

                lock (ChampionMoveRequestsLock)
                {
                    isLeftStale = (CurrentChampMoveRequest == PreviousChampMoveRequest);
                    isLeftAtRest = CurrentChampMoveRequest.StickEventData.WasReleased;
                    currChampRequest = CurrentChampMoveRequest;

                    PreviousChampMoveRequest = CurrentChampMoveRequest;
                }                

                if (DEBUG)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("isStale: {0}\nisAtrest: {1}", isRightStale, isRightAtRest);
                }                

                if (!(isRightStale && isRightAtRest))  // user is actively moving stick
                {
                    MoveCursor(currCursorReqest.StickEventData, currCursorReqest.SensitivityFactor);
                }
                else
                {
                    if (DEBUG)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Ignored");
                    }
                }

                if (!(isLeftStale && isLeftAtRest))
                {
                    if (currChampRequest.isUrgent || champMoveTimer <= 0)
                    {
                        MoveChampion(currChampRequest.StickEventData, currChampRequest.DistanceFactor);
                        champMoveTimer = 100;  // reset timer
                    }
                }

                Thread.Sleep(40);

                champMoveTimer--;
            }
        }

        #region Request Submitters

        private void SubmitCursorMovementRequest(StickMovedEventArgs e)
        {
            double sensitivityFactor;

            sensitivityFactor = (e.Magnitude <= 0.8) ? 0.5 : 1;

            if (DEBUG)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Submit: Waiting...");
            }

            lock (CursorMoveRequestsLock)
            {
                if (DEBUG) Console.WriteLine("Submit: Changing...");

                CurrentCursorMoveRequest = new CursorMovementRequest(e, sensitivityFactor);

                if (DEBUG) CurrentCursorMoveRequest.Print();
            }
        }

        private void SubmitChampionMovementRequest(StickMovedEventArgs e)
        {
            bool isUrgent = false;
            double distanceFactor;
            double degreeRotated, degreeDelta = 0;

            distanceFactor = (e.Magnitude <= 0.8) ? 0.5 : 1;

            if (e.WasReleased)
            {
                degreeRotated = 0;
                degreeDelta = 0;
            }
            else
            {
                degreeRotated = Math.Atan(e.PosX / e.PosY);
            }

            degreeDelta = CurrentChampMoveRequest.DegreeRotated - degreeRotated;
            //isUrgent = (degreeDelta > )
            Console.WriteLine("Degree: " + degreeDelta);

            //MoveChampion(e, distanceFactor);
        }
        #endregion

        #region Movement Actions

        private void MoveCursor(StickMovedEventArgs e, double sensitivityFactor)
        {
            int locDeltaX, locDeltaY;
            double locX, locY;           

            sensitivityFactor *= 2000;
            
            if (e.WasReleased)
            {            
                // Move back to origin
                lock (MovingCursorLock)
                {
                    Mouse.MoveMouseTo(CursorOrigin.X, CursorOrigin.Y); 
                }

                if (DEBUG)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Move: To origin");
                }

                CurrentCursorLocation = CursorOrigin;  // update
            }
            else
            {
                // Move mouse relative to current location
                locDeltaX = (int)(e.PosX * sensitivityFactor);
                locDeltaY = (int)(e.PosY * sensitivityFactor);

                // Compensate for horizontal aspect ratio (16:9)
                locDeltaX = (int)(locDeltaX * (9.0 / 16.0));

                locX = CurrentCursorLocation.X + locDeltaX;
                locY = CurrentCursorLocation.Y + locDeltaY;

                // Reset if past screen boundary
                if (locX > 65535)  locX = CurrentCursorLocation.X;
                if (locY > 65535) locY = CurrentCursorLocation.Y;
                                
                lock (MovingCursorLock)
                {
                    Mouse.MoveMouseTo(locX, locY);
                }

                CurrentCursorLocation = new Location(locX, locY); // update
            }
        }

        private void MoveChampion(StickMovedEventArgs e, double distanceFactor)
        {
            double locX, locY;

            // Impose circle limitations within main monitor (circle within rectangle)
            locX = 32767.5 + (e.PosX * 18431.71875 * distanceFactor);
            locY = 32767.5 + (e.PosY * 32767.5 * distanceFactor);

            Mouse.MoveMouseTo(locX, locY);
            Mouse.RightButtonDown();
            Mouse.MoveMouseBy(10, 10);
            Mouse.RightButtonUp();
        }
        #endregion

        #region Event Listeners

        protected void OnLeftStickMoved(Object sender, StickMovedEventArgs e)
        {
            SubmitChampionMovementRequest(e);
        }

        protected void OnRightStickMoved(Object sender, StickMovedEventArgs e)
        {
            SubmitCursorMovementRequest(e);
        }
        #endregion

        #region Movement Request Classes
        
        private class CursorMovementRequest
        {
            public double SensitivityFactor;
            public StickMovedEventArgs StickEventData;

            public CursorMovementRequest(StickMovedEventArgs e, double sensitivityFactor)
            {
                StickEventData = e;
                SensitivityFactor = sensitivityFactor;
            }

            public void Print()
            {
                Console.WriteLine("--------Cursor Movement Request-------------");
                Console.WriteLine("Cursor moved: ({0}, {1})", StickEventData.PosX, StickEventData.PosY);
                Console.WriteLine("Stick Released: {0}", StickEventData.WasReleased);
                Console.WriteLine("Sensitivity: {0} \n", SensitivityFactor);
            }
        }

        private class ChampionMovementRequest
        {
            public double DistanceFactor;
            public double DegreeRotated;
            public bool isUrgent;
            public StickMovedEventArgs StickEventData;

            public ChampionMovementRequest(StickMovedEventArgs e, double distanceFactor)
            {
                StickEventData = e;
                DistanceFactor = distanceFactor;
                DegreeRotated = 0;
                isUrgent = false;
            }

            public void Print()
            {
                Console.WriteLine("--------Champion Movement Request-------------");
                Console.WriteLine("Cursor moved: ({0}, {1})", StickEventData.PosX, StickEventData.PosY);
                Console.WriteLine("Stick Released: {0}", StickEventData.WasReleased);
                Console.WriteLine("Sensitivity: {0} \n", DistanceFactor);
            }
        }
        #endregion
    }

    public struct Location
    {
        public double X;
        public double Y;

        public Location(double x, double y)
        {
            X = x;
            Y = y;
        }
    }    
}
