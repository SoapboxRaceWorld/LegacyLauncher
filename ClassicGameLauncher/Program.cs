using GameLauncher.HashPassword;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ClassicGameLauncher {
    static class Program {
        /// <summary>
        /// Główny punkt wejścia dla aplikacji.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //if (SHA.HashFile("nfsw.exe") != "7C0D6EE08EB1EDA67D5E5087DDA3762182CDE4AC") { 
            //    MessageBox.Show("Invalid file was detected, please restore original nfsw.exe", "LegacyLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //} else {
                Application.Run(new Form1());
            //}
        }


    }
}
