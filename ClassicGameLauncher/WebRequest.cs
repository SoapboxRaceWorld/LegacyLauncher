using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO.Compression;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;

namespace GameLauncherReborn {
    public class WebClientWithTimeout : WebClient {
        protected override WebRequest GetWebRequest(Uri address) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
            request.UserAgent = "GameLauncher (+https://github.com/SoapboxRaceWorld/GameLauncher_NFSW)";
            request.Headers["X-HWID"] = Security.FingerPrint.Value();
            request.Headers["X-UserAgent"] = "LegacyLauncher " + Application.ProductVersion + " WinForms (+https://github.com/metonator/legacylauncher)";
            request.Timeout = 1000;

            return request;
        }
    }
}