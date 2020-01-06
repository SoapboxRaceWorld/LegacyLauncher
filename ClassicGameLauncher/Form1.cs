using GameLauncher.App.Classes;
using GameLauncher.App.Classes.Auth;
using GameLauncher.App.Classes.HashPassword;
using GameLauncher.App.Classes.ModNetReloaded;
using GameLauncher.HashPassword;
using GameLauncherReborn;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace ClassicGameLauncher {
    public partial class Form1 : Form {
        private bool _modernAuthSupport = false;
        private bool _ticketRequired;

        int CountFiles = 0;
        int CountFilesTotal = 0;

        SimpleJSON.JSONNode result;

        public Form1() {
            InitializeComponent();

            Load += new EventHandler(Form1_Load);
            serverText.SelectedIndexChanged += new EventHandler(serverPick_SelectedIndexChanged);

            actionText.Text = "Ready!";
        }

        private void Form1_Load(object sender, EventArgs e) {
            var response = "";
            try {
                WebClient wc = new WebClient();
                string serverurl = "http://launcher.worldunited.gg/serverlist.txt";
                response = wc.DownloadString(serverurl);
            } catch (Exception) { }

            serverText.DisplayMember = "Text";
            serverText.ValueMember = "Value";

            List<Object> items = new List<Object>();

            String[] substrings = response.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var substring in substrings) {
                if (!String.IsNullOrEmpty(substring)) {
                    String[] substrings22 = substring.Split(new string[] { ";" }, StringSplitOptions.None);
                    items.Add(new { Text = substrings22[0], Value = substrings22[1] });
                }
            }

            serverText.DataSource = items;
            serverText.SelectedIndex = 0;
        }

        private void serverPick_SelectedIndexChanged(object sender, EventArgs e) {
            Tokens.Clear();
            actionText.Text = "Loading info...";

            try {
                button1.Enabled = true;
                button2.Enabled = true;

                WebClientWithTimeout serverval = new WebClientWithTimeout();
                var stringToUri = new Uri(serverText.SelectedValue.ToString() + "/GetServerInformation");
                String serverdata = serverval.DownloadString(stringToUri);

                result = SimpleJSON.JSON.Parse(serverdata);

                actionText.Text = "Players on server: " + result["onlineNumber"];

                try {
                    if (string.IsNullOrEmpty(result["modernAuthSupport"])) {
                        _modernAuthSupport = false;
                    } else if (result["modernAuthSupport"]) {
                        if (stringToUri.Scheme == "https") {
                            _modernAuthSupport = true;
                        } else {
                            _modernAuthSupport = false;
                        }
                    } else {
                        _modernAuthSupport = false;
                    }
                } catch {
                    _modernAuthSupport = false;
                }

                try {
                    _ticketRequired = (bool)result["requireTicket"];
                } catch {
                    _ticketRequired = true; //lets assume yes, we gonna check later if ticket is empty or not.
                }
            } catch {
                button1.Enabled = false;
                button2.Enabled = false;
                actionText.Text = "Server is offline.";
            }


            ticketBox.Enabled = _ticketRequired;
        }

        private void button1_Click(object sender, EventArgs e) {
            Tokens.Clear();
            if (!validateEmail(loginEmailBox.Text)) {
                actionText.Text = "Please type your email!";
            } else if (String.IsNullOrEmpty(loginPasswordBox.Text)) {
                actionText.Text = "Please type your password!";
            } else {
                Tokens.IPAddress = serverText.SelectedValue.ToString();
                Tokens.ServerName = serverText.SelectedItem.ToString();

                if (_modernAuthSupport == false) {
                    ClassicAuth.Login(loginEmailBox.Text, SHA.HashPassword(loginPasswordBox.Text).ToLower());
                } else {
                    ModernAuth.Login(loginEmailBox.Text, loginPasswordBox.Text);
                }

                if (String.IsNullOrEmpty(Tokens.Error)) {
                    if (!String.IsNullOrEmpty(Tokens.Warning)) {
                        MessageBox.Show(null, Tokens.Warning, "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    //TODO: MODS GOES HERE
                    DoModNetJob();
                    //
                } else {
                    MessageBox.Show(null, Tokens.Error, "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    actionText.Text = (String.IsNullOrEmpty(Tokens.Error)) ? "An error occurred." : Tokens.Error;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            if (!validateEmail(registerEmail.Text)) {
                actionText.Text = "Please type your email!";
            } else if (String.IsNullOrEmpty(registerPassword.Text)) {
                actionText.Text = "Please type your password!";
            } else if (String.IsNullOrEmpty(registerPassword2.Text)) {
                actionText.Text = "Please type your confirmation password!";
            } else if (registerPassword.Text != registerPassword2.Text) {
                actionText.Text = "Password doesn't match!";
            } else if(_ticketRequired) {
                if(String.IsNullOrEmpty(ticketBox.Text)) {
                    actionText.Text = "Ticket is required to play on this server!";
                } else {
                    createAccount();
                }
            } else {
                createAccount();
            }
        }

        private void createAccount() {
            String token = (_ticketRequired) ? ticketBox.Text : null;
            Tokens.IPAddress = serverText.SelectedValue.ToString();
            Tokens.ServerName = serverText.SelectedItem.ToString();

            if (_modernAuthSupport == false) {
                ClassicAuth.Register(registerEmail.Text, SHA.HashPassword(registerPassword.Text), token);
            } else {
                ModernAuth.Register(registerEmail.Text, registerPassword.Text, token);
            }

            if (!String.IsNullOrEmpty(Tokens.Success)) {
                MessageBox.Show(null, Tokens.Success, "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                actionText.Text = Tokens.Success;

                tabControl1.Visible = true;
            } else {
                MessageBox.Show(null, Tokens.Error, "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                actionText.Text = Tokens.Error;
            }
        }

        public static bool validateEmail(string email) {
            if (String.IsNullOrEmpty(email)) return false;

            String theEmailPattern = @"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*"
                                   + "@"
                                   + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$";

            return Regex.IsMatch(email, theEmailPattern);
        }

        public void launchGame() {
            actionText.Text = "Launching game...";
            Application.DoEvents();
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.WorkingDirectory = Directory.GetCurrentDirectory();
            psi.FileName = "nfsw.exe";
            psi.Arguments = "EU " + Tokens.IPAddress + " " + Tokens.LoginToken + " " + Tokens.UserId;

            Process.Start(psi);
        }

        private string FormatFileSize(long byteCount) {
            var numArray = new double[] { 1000000000, 1000000, 1000, 0 };
            var strArrays = new[] { "GB", "MB", "KB", "Bytes" };
            for (var i = 0; i < numArray.Length; i++) {
                if (byteCount >= numArray[i]) {
                    return string.Concat($"{byteCount / numArray[i]:0.00} ", strArrays[i]);
                }
            }

            return "0 Bytes";
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            this.BeginInvoke((MethodInvoker)delegate {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                actionText.Text = ("Downloaded " + FormatFileSize(e.BytesReceived) + " of " + FormatFileSize(e.TotalBytesToReceive));
            });
        }

        public void DoModNetJob() {
            File.Delete("ModManager.dat");

            if (!Directory.Exists("modules")) Directory.CreateDirectory("modules");
            if (!Directory.Exists("scripts")) Directory.CreateDirectory("scripts");
            String[] GlobalFiles = new string[] { "dinput8.dll", "global.ini" };
            String[] ModNetReloadedFiles = new string[] { "7z.dll", "PocoFoundation.dll", "PocoNet.dll", "ModLoader.asi" };
            String[] ModNetLegacyFiles = new string[] { "modules/udpcrc.soapbox.module", "modules/udpcrypt1.soapbox.module", "modules/udpcrypt2.soapbox.module", "modules/xmppsubject.soapbox.module",
                    "scripts/global.ini", "lightfx.dll", "ModManager.asi", "global.ini" };

            String[] RemoveAllFiles = GlobalFiles.Concat(ModNetReloadedFiles).Concat(ModNetLegacyFiles).ToArray();

            foreach (string file in RemoveAllFiles) {
                if (File.Exists(file)) {
                    try {
                        File.Delete(file);
                    } catch {
                        MessageBox.Show($"File {file} cannot be deleted.", "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            actionText.Text = "Detecting ModNetSupport for " + serverText.SelectedItem.ToString();
            String jsonModNet = ModNetReloaded.ModNetSupported(serverText.SelectedValue.ToString());

            if (jsonModNet != String.Empty) {
                actionText.Text = "ModNetReloaded support detected, downloading required files...";

                string[] newFiles = GlobalFiles.Concat(ModNetReloadedFiles).ToArray();
                try {
                    try { if (File.Exists("lightfx.dll")) File.Delete("lightfx.dll"); } catch { }

                    WebClientWithTimeout newModNetFilesDownload = new WebClientWithTimeout();
                    foreach (string file in newFiles) {
                        actionText.Text = "Fetching ModNetReloaded Files: " + file;
                        Application.DoEvents();
                        newModNetFilesDownload.DownloadFile("https://cdn.soapboxrace.world/modules/" + file + ".dll", file + ".dll");
                    }

                    try {
                        newModNetFilesDownload.DownloadFile("https://launcher.worldunited.gg/legacy/global.ini", "global.ini");
                    }
                    catch { }

                    SimpleJSON.JSONNode MainJson = SimpleJSON.JSON.Parse(jsonModNet);

                    Uri newIndexFile = new Uri(MainJson["basePath"] + "/index.json");
                    String jsonindex = new WebClientWithTimeout().DownloadString(newIndexFile);

                    SimpleJSON.JSONNode IndexJson = SimpleJSON.JSON.Parse(jsonindex);

                    CountFilesTotal = IndexJson["entries"].Count;

                    String path = Path.Combine("MODS", MDFive.HashPassword(MainJson["serverID"]).ToLower());
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    foreach (JSONNode modfile in IndexJson["entries"]) {
                        if (SHA.HashFile(path + "/" + modfile["Name"]).ToLower() != modfile["Checksum"]) {
                            WebClientWithTimeout client2 = new WebClientWithTimeout();
                            client2.DownloadFileAsync(new Uri(MainJson["basePath"] + "/" + modfile["Name"]), path + "/" + modfile["Name"]);

                            client2.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                            client2.DownloadFileCompleted += (test, stuff) => {
                                if (SHA.HashFile(path + "/" + modfile["Name"]).ToLower() == modfile["Checksum"]) {
                                    CountFiles++;

                                    if (CountFiles == CountFilesTotal) {
                                        launchGame();
                                    }
                                } else {
                                    File.Delete(path + "/" + modfile["Name"]);
                                    Console.WriteLine(modfile["Name"] + " must be removed.");
                                    DoModNetJob();
                                }
                            };
                        } else {
                            CountFiles++;

                            if (CountFiles == CountFilesTotal) {
                                launchGame();
                            }
                        }
                    }

                } catch (Exception ex) {
                    MessageBox.Show(ex.Message);
                }
            } else {
                string[] newFiles = GlobalFiles.Concat(ModNetLegacyFiles).ToArray();

                WebClientWithTimeout newModNetFilesDownload = new WebClientWithTimeout();
                foreach (string file in newFiles) {
                    actionText.Text = "Fetching LegacyModnet Files: " + file;
                    Application.DoEvents();
                    newModNetFilesDownload.DownloadFile("http://launcher.worldunited.gg/legacy/" + file, file);
                }

                if (result["modsUrl"] != null) {
                    actionText.Text = "Electron support detected, checking mods...";

                    Uri newIndexFile = new Uri(result["modsUrl"] + "/index.json");
                    String jsonindex = new WebClientWithTimeout().DownloadString(newIndexFile);
                    SimpleJSON.JSONNode IndexJson = SimpleJSON.JSON.Parse(jsonindex);

                    CountFilesTotal = IndexJson.Count;

                    String electronpath = (new Uri(serverText.SelectedValue.ToString()).Host).Replace(".", "-");
                    String path = Path.Combine("MODS", electronpath);
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    File.WriteAllText(path + ".json", jsonindex);

                    using (var fs = new FileStream("ModManager.dat", FileMode.Create))
                    using (var bw = new BinaryWriter(fs)) {
                        bw.Write(CountFilesTotal);

                        foreach (JSONNode file in IndexJson) {
                            var originalPath = Path.Combine(file["file"]).Replace("/", "\\").ToUpper();
                            var modPath = Path.Combine(path, file["file"]).Replace("/", "\\").ToUpper();

                            bw.Write(originalPath.Length);
                            bw.Write(originalPath.ToCharArray());
                            bw.Write(modPath.Length);
                            bw.Write(modPath.ToCharArray());
                        }
                    }

                    foreach (JSONNode modfile in IndexJson) {
                        String directorycreate = Path.GetDirectoryName(path + "/" + modfile["file"]);
                        Directory.CreateDirectory(directorycreate);

                        if (ElectronModNet.calculateHash(path + "/" + modfile["file"]) != modfile["hash"]) {
                            WebClientWithTimeout client2 = new WebClientWithTimeout();
                            client2.DownloadFileAsync(new Uri(result["modsUrl"] + "/" + modfile["file"]), path + "/" + modfile["file"]);

                            client2.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                            client2.DownloadFileCompleted += (test, stuff) => {
                                if (ElectronModNet.calculateHash(path + "/" + modfile["file"]) == modfile["hash"]) {
                                    CountFiles++;

                                    if (CountFiles == CountFilesTotal) {
                                        launchGame();
                                    }
                                } else {
                                    File.Delete(path + "/" + modfile["file"]);
                                    Console.WriteLine(modfile["file"] + " must be removed.");
                                    DoModNetJob();
                                }
                            };
                        } else {
                            CountFiles++;

                            if (CountFiles == CountFilesTotal) {
                                launchGame();
                            }
                        }
                    }

                } else if ((bool)result["rwacallow"] == true) {
                    actionText.Text = "RWAC support detected, checking mods...";

                    String rwacpath = MDFive.HashPassword(new Uri(serverText.SelectedValue.ToString()).Host);
                    String path = Path.Combine("MODS", rwacpath);
                    Uri rwac_wev2 = new Uri(result["homePageUrl"] + "/rwac/fileschecker_sbrw.xml");
                    String getcontent = new WebClient().DownloadString(rwac_wev2);

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(getcontent);
                    var nodes = xmlDoc.SelectNodes("rwac/files/file");

                    CountFilesTotal = nodes.Count;

                    //ModManager.dat
                    using (var fs = new FileStream("ModManager.dat", FileMode.Create))
                    using (var bw = new BinaryWriter(fs)) {
                        bw.Write(nodes.Count);

                        foreach (XmlNode files in nodes) {
                            string realfilepath = Path.Combine(files.Attributes["path"].Value, files.Attributes["name"].Value);
                            String directorycreate = Path.GetDirectoryName(path + "/" + realfilepath);

                            var originalPath = Path.Combine(realfilepath).Replace("/", "\\").ToUpper();
                            var modPath = Path.Combine(path, realfilepath).Replace("/", "\\").ToUpper();

                            bw.Write(originalPath.Length);
                            bw.Write(originalPath.ToCharArray());
                            bw.Write(modPath.Length);
                            bw.Write(modPath.ToCharArray());

                            Directory.CreateDirectory(directorycreate);
                            if (files.Attributes["download"].Value != String.Empty)
                            {
                                if (MDFive.HashFile(path + "/" + realfilepath).ToLower() != files.InnerText)
                                {
                                    WebClientWithTimeout client2 = new WebClientWithTimeout();
                                    client2.DownloadFileAsync(new Uri(files.Attributes["download"].Value), path + "/" + realfilepath);

                                    client2.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                                    client2.DownloadFileCompleted += (test, stuff) => {
                                        if (MDFive.HashFile(path + "/" + realfilepath).ToLower() == files.InnerText) {
                                            CountFiles++;

                                            if (CountFiles == CountFilesTotal) {
                                                launchGame();
                                            }
                                        } else {
                                            File.Delete(path + "/" + realfilepath);
                                            Console.WriteLine(realfilepath + " must be removed.");
                                            DoModNetJob();
                                        }
                                    };
                                } else {
                                    CountFiles++;

                                    if (CountFiles == CountFilesTotal) {
                                        launchGame();
                                    }
                                }
                            } else {
                                CountFiles++;

                                if (CountFiles == CountFilesTotal) {
                                    launchGame();
                                }
                            }
                        }
                    }
                } else  {
                    actionText.Text = "Deprecated modnet detected. Aborting...";
                    launchGame();
                }
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            string send = Prompt.ShowDialog("Please specify your email address.", "LegacyLauncher");

            if (send != String.Empty) {
                String responseString;
                try {
                    Uri resetPasswordUrl = new Uri(serverText.SelectedValue.ToString() + "/RecoveryPassword/forgotPassword");

                    var request = (HttpWebRequest)System.Net.WebRequest.Create(resetPasswordUrl);
                    var postData = "email=" + send;
                    var data = Encoding.ASCII.GetBytes(postData);
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = data.Length;

                    using (var stream = request.GetRequestStream()) {
                        stream.Write(data, 0, data.Length);
                    }

                    var response = (HttpWebResponse)request.GetResponse();
                    responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                } catch {
                    responseString = "Failed to send email!";
                }

                MessageBox.Show(null, responseString, "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
