/*
 *  This file is part of the Mirage Desktop Environment.
 *  github.com/mirage-desktop/Mirage
 */

using altroso.Core;
using altroso.GUI.SurfaceKit;
using altroso.GUI.TextKit;
using altroso.GUI.UIKit;

namespace altroso.GUI.DE
{
    /// <summary>
    /// Graphical login manager.
    /// </summary>
    class LoginWindow : UIApplication
    {
        /// <summary>
        /// Initialise the application.
        /// </summary>
        public LoginWindow(SurfaceManager surfaceManager) : base(surfaceManager)
        {
            MainWindow = new UIWindow(surfaceManager, 320, 240, "Login to " + Program.Hostname, resizable: false)
            {
                BackgroundColor = GraphicsKit.Color.White
            };
            UICanvasView icon = new UICanvasView(Resources.Keyboard, alpha: true)
            {
                Location = new(24, 24),
            };
        }

        /// <summary>
        /// The main text field of the text editor.
        /// </summary>
        private readonly UITextView Username;
        private readonly UITextView Password;
        private readonly UITextView UsernameString;
        private readonly UITextView PasswordString;
        private readonly UIButton LoginButton;
    }
}