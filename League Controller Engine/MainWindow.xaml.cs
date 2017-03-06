using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsInput;
using WindowsInput.Native;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace League_Controller_Engine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KeyboardSimulator Keybd;
        private MouseSimulator Mouse;

        private XControllerManager xcm;
        private MovementManager mm;

        public MainWindow()
        {   
            InitializeComponent();

            Keybd = new KeyboardSimulator(new InputSimulator());
            Mouse = new MouseSimulator(new InputSimulator());

            xcm = XControllerManager.Instance;
            mm  = MovementManager.Instance;

            xcm.ButtonsPressed  += new EventHandler<ButtonStateChangedEventArgs>(OnButtonsPressed);
            xcm.ButtonsReleased += new EventHandler<ButtonStateChangedEventArgs>(OnButtonsReleased);
            xcm.LeftTriggerPressed += new EventHandler<TriggerStateChangedEventArgs>(OnLeftTriggerPressed);
            xcm.RightTriggerPressed += new EventHandler<TriggerStateChangedEventArgs>(OnRightTriggerPressed);
        }

        protected void OnButtonsPressed(Object sender, ButtonStateChangedEventArgs e)
        {
            VirtualKeyCode key;
            bool valid = true;

            foreach (XControllerManager.XboxButton button in e.ButtonsPressedArr)
            {
                Console.WriteLine("Pressed: " + button);
                
                switch (button)
                {
                    case XControllerManager.XboxButton.A:
                        key = VirtualKeyCode.VK_Q;
                        break;
                    case XControllerManager.XboxButton.B:
                        key = VirtualKeyCode.VK_W;
                        break;
                    case XControllerManager.XboxButton.X:
                        key = VirtualKeyCode.VK_E;
                        break;
                    case XControllerManager.XboxButton.Y:
                        key = VirtualKeyCode.VK_R;
                        break;
                    case XControllerManager.XboxButton.Back:
                        key = VirtualKeyCode.TAB;
                        break;
                    case XControllerManager.XboxButton.Start:
                        key = VirtualKeyCode.ESCAPE;
                        break;
                    case XControllerManager.XboxButton.RS:
                        key = VirtualKeyCode.VK_F;  // summoner slot 1
                        break;
                    case XControllerManager.XboxButton.LS:
                        key = VirtualKeyCode.VK_D;  // summoner slot 2
                        break;
                    case XControllerManager.XboxButton.DPadddown:
                        key = VirtualKeyCode.VK_C;  // on my way
                        break;
                    case XControllerManager.XboxButton.DPadup:
                        key = VirtualKeyCode.VK_Z;  // assist me
                        break;
                    case XControllerManager.XboxButton.DPadleft:
                        key = VirtualKeyCode.VK_V;  // retreat
                        break;
                    case XControllerManager.XboxButton.DPadright:
                        key = VirtualKeyCode.VK_X;  // alert
                        break;
                    default:
                        key = VirtualKeyCode.NONAME;
                        break;
                }
                
                Keybd.KeyPress(key);
            }
        }

        protected void OnButtonsReleased(Object sender, ButtonStateChangedEventArgs e)
        {
            foreach (XControllerManager.XboxButton button in e.ButtonsPressedArr)
            {
                Console.WriteLine("Released: " + button);
            }
        }

        protected void OnLeftTriggerPressed(Object sender, TriggerStateChangedEventArgs e)
        {
            Mouse.LeftButtonClick();
        }

        protected void OnRightTriggerPressed(Object sender, TriggerStateChangedEventArgs e)
        {
            Mouse.RightButtonClick();
        }

        //protected void OnLeftStickMoved(object sender, StickMovedEventArgs e)
        //{
        //    //Console.WriteLine("Left stick: " + e.PosX + ", " + e.PosY + "; " + e.Magnitude);
        //}

        //protected void OnRightStickMoved(object sender, StickMovedEventArgs e)
        //{
        //    //Console.WriteLine("Right stick: " + e.PosX + ", " + e.PosY + "; " + e.Magnitude);
        //}
    }
}
