using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NLog;

namespace Nebulator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Add the event handler for handling UI thread exceptions to the event.
            Application.ThreadException += Application_ThreadException;

            // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Add the event handler for handling non-UI thread exceptions to the event. 
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Go!
            MainForm f = new MainForm();
            Application.Run(f);
        }

        /// <summary>
        /// Exception handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            // Unhandled exception, log the stack of application context.
            string s = $"Unhandled exception:{e.Exception.Message}{Environment.NewLine}{e.Exception.StackTrace}{Environment.NewLine}";
            LogManager.GetCurrentClassLogger().Error("Unhandled exception: " + s);
            MessageBox.Show($"Unhandled exception:{Environment.NewLine}{s}{Environment.NewLine}");
        }

        /// <summary>
        /// Exception handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Unhandled exception, log the stack of application context.
            string s = $"Unhandled domain exception:{e}{Environment.NewLine}{e.ExceptionObject}{Environment.NewLine}";
            LogManager.GetCurrentClassLogger().Error("Unhandled domain exception: " + s);
            MessageBox.Show($"Unhandled domain exception:{Environment.NewLine}{s}{Environment.NewLine}Sorry, you are screwed - goodbye.");
        }
    }
}
