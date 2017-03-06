using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using J2i.Net.XInputWrapper;

namespace League_Controller_Engine
{
    public partial class XControllerManager : Component
    {
        private const bool DEBUG = true;

        // Private class variables
        private const int UPDATE_FREQ = 25; // default value
        private bool LeftStickWasMoving  = false;
        private bool RightStickWasMoving = false;
        private bool LeftTriggerWasPressed = false;
        private bool RightTriggerWasPressed = false;
        private const float MAGNITUDE_DEADZONE = 7300;
        private const float MAGNITUDE_MAX = 32767;
        private const int PRESSURE_REQUIRED = 30;
        private static XControllerManager PrivateInstance;

        // Public class structures
        public enum XboxButton : byte
        {
            empty      = 0,
            Y          = 1,
            X          = 2,
            B          = 3,
            A          = 4,
            RB         = 7,
            LB         = 8,
            RS         = 9,
            LS         = 10,
            Back       = 11,
            Start      = 12,
            DPadright  = 13,
            DPadleft   = 14,
            DPadddown  = 15,
            DPadup     = 16
        };

        public static XControllerManager Instance
        {
            get
            {
                if (PrivateInstance == null)
                {
                    PrivateInstance = new XControllerManager();
                }

                return PrivateInstance;
            }
        }

        // Events
        public event EventHandler<ButtonStateChangedEventArgs> ButtonsPressed;
        public event EventHandler<ButtonStateChangedEventArgs> ButtonsReleased;
        public event EventHandler<StickMovedEventArgs> LeftStickMoved;
        public event EventHandler<StickMovedEventArgs> RightStickMoved;
        public event EventHandler<TriggerStateChangedEventArgs> LeftTriggerPressed;
        public event EventHandler<TriggerStateChangedEventArgs> LeftTriggerReleased;
        public event EventHandler<TriggerStateChangedEventArgs> RightTriggerPressed;
        public event EventHandler<TriggerStateChangedEventArgs> RightTriggerReleased;

        // Constructor
        private XControllerManager()
        {
            XboxController xController;

            InitializeComponent();
            
            // Setup for receival of controller inputs
            xController = XboxController.RetrieveController(0);
            XboxController.UpdateFrequency = UPDATE_FREQ;
            XboxController.StartPolling();

            // Subscripted events
            xController.StateChanged += new EventHandler<XboxControllerStateChangedEventArgs>(OnControllerStateChanged);
        }


        #region Controller State Changed Parsers

        private void OnControllerStateChanged(object sender, XboxControllerStateChangedEventArgs e)
        {
            bool didChange = false;
            XInputGamepad currGamepad, prevGamepad;  // current and previous gamepad states

            currGamepad = e.CurrentInputState.Gamepad;
            prevGamepad = e.PreviousInputState.Gamepad;

            didChange = !(currGamepad.Equals(prevGamepad));

            // Left stick change
            if ((currGamepad.sThumbLX != prevGamepad.sThumbLX) || 
                (currGamepad.sThumbLY != prevGamepad.sThumbLY))
            {
                ParseLeftStickStateChange(e);
            }

            // Right stick change
            if ((currGamepad.sThumbRX != prevGamepad.sThumbRX) || 
                (currGamepad.sThumbRY != prevGamepad.sThumbRY))
            {
                ParseRightStickStateChange(e);
            }

            // Right trigger change
            if (currGamepad.bRightTrigger != prevGamepad.bRightTrigger)
            {
                ParseRightTriggerStateChange(e);
            }

            // Button change
            if (currGamepad.wButtons != prevGamepad.wButtons)
            {
                ParseButtonStateChangeDifference(e);
            }

            // Left trigger change
            if (currGamepad.bLeftTrigger != prevGamepad.bLeftTrigger)
            {

            }

            // Nothing known was changed
            if (!didChange)
            {
                Console.WriteLine("Unknown. ");
            }
        }

        private void ParseButtonStateChangeDifference(XboxControllerStateChangedEventArgs e)
        {
            int bitFlagIndex, bitMaskSelection = 0;                    // loop and bit mask variables
            int currButtonState, prevButtonState, buttonStatesChanged; // bit arrays for button state
            List<XboxButton> buttonsPressedArr, buttonsReleasedArr;

            buttonsPressedArr  = new List<XboxButton>(0);
            buttonsReleasedArr = new List<XboxButton>(0);

            currButtonState = (int) e.CurrentInputState.Gamepad.wButtons;
            prevButtonState = (int) e.PreviousInputState.Gamepad.wButtons;

            // EXAMPLE - new: A B X Y  old: A B X Y = changed: A B X Y
            //                1 0 0 1  xor  0 1 0 1      =     1 1 0 0 (A and B have changed)   
            buttonStatesChanged = currButtonState ^ prevButtonState;
            
            bitMaskSelection = 0x8000; // MSB for short data type mask
            // aka: bitMaskSelection = 0b00000000000000001000000000000000

            // Loop through buttonStatesChanged bits to find changed button states and 
            //   add newly changed button states to be published
            for (bitFlagIndex = 1; bitMaskSelection > 0; bitFlagIndex++)
            {
                // Has button state changed?
                if ((buttonStatesChanged & bitMaskSelection) != 0)
                {
                    // If so, has button been pressed or released?
                    if ((currButtonState & buttonStatesChanged) != 0)
                    {
                        buttonsPressedArr.Add((XboxButton)bitFlagIndex);
                    }
                    else
                    {
                        buttonsReleasedArr.Add((XboxButton)bitFlagIndex);
                    }
                }

                bitMaskSelection >>= 1; // increment (shift) button mask selection
            }

            ButtonStateChangedEventArgs pressedArgs  = new ButtonStateChangedEventArgs(buttonsPressedArr);
            ButtonStateChangedEventArgs releasedArgs = new ButtonStateChangedEventArgs(buttonsReleasedArr);
                                    
            // Publish events if need be
            if (buttonsPressedArr.Count != 0)  OnButtonsPressed(pressedArgs);
            if (buttonsReleasedArr.Count != 0) OnButtonsReleased(releasedArgs);
        }

        private void ParseLeftStickStateChange(XboxControllerStateChangedEventArgs e)
        {
            float leftStickX, leftStickY, normLSX, normLSY; // stick x and y
            float magnitude, normMag;                       // stick magnitude
            StickMovedEventArgs stickArgs;

            leftStickX = e.CurrentInputState.Gamepad.sThumbLX;
            leftStickY = e.CurrentInputState.Gamepad.sThumbLY;
            magnitude = (float)Math.Sqrt(leftStickX * leftStickX + leftStickY * leftStickY);

            // Normalize x and y values (range: 0.0 - 1.0)
            normLSX = leftStickX / magnitude;
            normLSY = leftStickY / magnitude;

            normLSY = -normLSY; // invert y

            // Within deadzone?
            if (magnitude > MAGNITUDE_DEADZONE)
            {
                LeftStickWasMoving = true;

                // Clip magnitude at expected max value
                if (magnitude > MAGNITUDE_MAX) magnitude = MAGNITUDE_MAX;

                magnitude -= MAGNITUDE_DEADZONE;                            // adjust for relativity
                normMag = magnitude / (MAGNITUDE_MAX - MAGNITUDE_DEADZONE); // normalize magnitude
                
                stickArgs = new StickMovedEventArgs(normLSX, normLSY, normMag, false);
                OnLeftStickMoved(stickArgs);
            }
            else if (LeftStickWasMoving)
            {
                LeftStickWasMoving = false; // reset

                // Publish "released" motion
                stickArgs = new StickMovedEventArgs(0, 0, 0, true);
                OnLeftStickMoved(stickArgs);
            }
            else
            {
                // Do not publish any Stick Moved events if analog has not moved past the deadzone
            }
        }

        private void ParseRightStickStateChange(XboxControllerStateChangedEventArgs e)
        {
            float rightStickX, rightStickY, normRSX, normRSY; // stick x and y
            float magnitude, normMag;                         // stick magnitude
            StickMovedEventArgs stickArgs;

            rightStickX = e.CurrentInputState.Gamepad.sThumbRX;
            rightStickY = e.CurrentInputState.Gamepad.sThumbRY;
            magnitude = (float)Math.Sqrt(rightStickX * rightStickX + rightStickY * rightStickY);

            // Normalize x and y values (1.0 - 0.0)
            normRSX = rightStickX / magnitude;
            normRSY = rightStickY / magnitude;

            normRSY = -normRSY; // invert y

            // Within deadzone?
            if (magnitude > MAGNITUDE_DEADZONE)
            {
                RightStickWasMoving = true;

                if (DEBUG)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Parse: Change");
                }

                // Clip magnitude at expected max value
                if (magnitude > MAGNITUDE_MAX) magnitude = MAGNITUDE_MAX;

                magnitude -= MAGNITUDE_DEADZONE;                            // adjust for relativity
                normMag = magnitude / (MAGNITUDE_MAX - MAGNITUDE_DEADZONE); // normalize magnitude

                stickArgs = new StickMovedEventArgs(normRSX, normRSY, normMag, false);
                OnRightStickMoved(stickArgs);
            }
            else if (RightStickWasMoving)
            {
                RightStickWasMoving = false; // reset

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Parse: Was released ====================================");

                // Publish "stopped" motion
                stickArgs = new StickMovedEventArgs(0, 0, 0, true);
                OnRightStickMoved(stickArgs);
            }
            else
            {
                // Do not publish any Stick Moved events if analog has not moved past the deadzone
            }
        }

        private void ParseRightTriggerStateChange(XboxControllerStateChangedEventArgs e)
        {
            byte triggerPressure = e.CurrentInputState.Gamepad.bRightTrigger;
            TriggerStateChangedEventArgs triggerArgs;

            if (triggerPressure > PRESSURE_REQUIRED) // beyond threshold
            {
                if (RightTriggerWasPressed) // user is holding it down
                {
                    // ignore
                }
                else
                {
                    triggerArgs = new TriggerStateChangedEventArgs(true);
                    OnRightTriggerPressed(triggerArgs);
                }
            }
            else
            {
                if (!RightTriggerWasPressed) // trigger not pressed before
                {
                    // ignore
                }
                else
                {
                    triggerArgs = new TriggerStateChangedEventArgs(false);
                    OnRightTriggerReleased(triggerArgs);
                }
            }
        }

        private void ParseLeftTriggerStateChange(XboxControllerStateChangedEventArgs e)
        {
            byte triggerPressure = e.CurrentInputState.Gamepad.bLeftTrigger;
            TriggerStateChangedEventArgs triggerArgs;

            if (triggerPressure > PRESSURE_REQUIRED) // beyond threshold
            {
                if (LeftTriggerWasPressed) // user is holding it down
                {
                    // ignore
                }
                else
                {
                    triggerArgs = new TriggerStateChangedEventArgs(true);
                    OnLeftTriggerPressed(triggerArgs);
                }
            }
            else
            {
                if (!LeftTriggerWasPressed) // trigger not pressed before
                {
                    // ignore
                }
                else
                {
                    triggerArgs = new TriggerStateChangedEventArgs(false);
                    OnLeftTriggerReleased(triggerArgs);
                }
            }
        }

        #endregion

        #region Event Raisers

        protected virtual void OnButtonsPressed(ButtonStateChangedEventArgs e)
        {
            EventHandler<ButtonStateChangedEventArgs> handler = ButtonsPressed;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnButtonsReleased(ButtonStateChangedEventArgs e)
        {
            EventHandler<ButtonStateChangedEventArgs> handler = ButtonsReleased;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnLeftStickMoved(StickMovedEventArgs e)
        {
            EventHandler<StickMovedEventArgs> handler = LeftStickMoved;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnRightStickMoved(StickMovedEventArgs e)
        {
            EventHandler<StickMovedEventArgs> handler = RightStickMoved;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnLeftTriggerPressed(TriggerStateChangedEventArgs e)
        {
            EventHandler<TriggerStateChangedEventArgs> handler = LeftTriggerPressed;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnLeftTriggerReleased(TriggerStateChangedEventArgs e)
        {
            EventHandler<TriggerStateChangedEventArgs> handler = LeftTriggerReleased;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnRightTriggerPressed(TriggerStateChangedEventArgs e)
        {
            EventHandler<TriggerStateChangedEventArgs> handler = RightTriggerPressed;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnRightTriggerReleased(TriggerStateChangedEventArgs e)
        {
            EventHandler<TriggerStateChangedEventArgs> handler = RightTriggerReleased;

            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
    #endregion

    #region Event Classes

    public class ButtonStateChangedEventArgs : EventArgs
    {
        public List<XControllerManager.XboxButton> ButtonsPressedArr { get; set; }

        public ButtonStateChangedEventArgs(List<XControllerManager.XboxButton> buttonsPressedArr)
        {
           ButtonsPressedArr = buttonsPressedArr;
        }
    }

    public class StickMovedEventArgs
    {
        public float PosX { get; }
        public float PosY { get; }
        public float Magnitude { get; }
        public bool WasReleased { get; }

        public StickMovedEventArgs(float posX, float posY, float magnitude, bool wasReleased)
        {
            PosX = posX;
            PosY = posY;
            Magnitude = magnitude;
            WasReleased = wasReleased;
        }

        public bool Equals(StickMovedEventArgs other)
        {
            return (PosX == other.PosX &&
                    PosY == other.PosY &&
                    Magnitude == other.Magnitude &&
                    WasReleased == other.WasReleased);
        }
    }

    public class TriggerStateChangedEventArgs
    {
        public bool isPressed;

        public TriggerStateChangedEventArgs(bool pressed)
        {
            isPressed = pressed;
        }
    }

    #endregion
}
