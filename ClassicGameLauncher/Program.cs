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

            if (!System.IO.File.Exists("nfsw.exe"))
            {
                MessageBox.Show("nfsw.exe not found! Please put this launcher in the game directory. " +
                    "If you don't have the game installed yet use the new launcher to install it (visit https://soapboxrace.world/)",
                    "LegacyLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (SHA.HashFile("nfsw.exe") != "7C0D6EE08EB1EDA67D5E5087DDA3762182CDE4AC") { 
                MessageBox.Show("Invalid file was detected, please restore original nfsw.exe", "LegacyLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } else {
                Application.Run(new Form1());
            }
        }


    }
}
