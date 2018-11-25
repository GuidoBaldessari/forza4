﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Forza4.Classi;
using Forza4.forms;
using Forza4.Properties;
using System.Net.Sockets;
using System.Threading;
using System.Net;
//client.send(cose) mandas i dati al server e lui li rimanda a tutti lol

namespace Forza4
{
    public partial class FormPrincipale : Form
    {
        #region variabili e proprietà

        Socket socket;
        string msg ="aa";
        static public int righe = 6, colonne = 7, CountPlayer = 0, CountAvv = 0;
        public Forza4Logic logica = new Forza4Logic();

        Image PedinaPlayer = FoRzA_4.Properties.Resources.Pedina_1, PedinaAvversario = FoRzA_4.Properties.Resources.Pedina__1, PedinaVuota = FoRzA_4.Properties.Resources.Pedina_vuota;
        static protected int larghezzaPedina = 125, altezzaPedina = 125;

        public string turnoPlayer = "Me", turnoAvversario = "Avversario", stato = "wait";
        IPAddress ip;
        public int porta;


        public int AltezzaPedina { get => altezzaPedina; set => altezzaPedina = value; }
        public int LarghezzaPedina { get => larghezzaPedina; set => larghezzaPedina = value; }

        #endregion

        #region form principale
        public FormPrincipale()
        {
            InitializeComponent();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            System.Windows.Forms.Timer GameTimer = new System.Windows.Forms.Timer();
            GameTimer.Interval = 20;
            GameTimer.Tick += new EventHandler(GameTick);
            GameTimer.Start();

            login();

        }

        private void FormPrincipale_KeyPress(object sender, KeyEventArgs e) // da togliere nella release, nel designer ho aggiunto this.KeyDown = true; per il controllo tasti
        {
            if (e.Control && e.Alt && e.KeyCode == Keys.R)       // Ctrl-S Save
            {
                logica.Turno = 1;
                Reset();
                e.SuppressKeyPress = true;  // Stops other controls on the form receiving event.
            }
        }

        public void login()
        {
            Log FormLogin = new Log();
            if (FormLogin.ShowDialog() == DialogResult.OK)
            {
                ip = FormLogin.ip;
                porta = FormLogin.port;
                Thread t;
                if (FormLogin.mode == "host") t = new Thread(HostThread);
                else t = new Thread(JoinerThread);
                t.Start();
            }
            else //se viene chiuso il form del login si chiude tutto e bom
            {
                this.Close();
            }
        }
        #endregion

        #region server

        public void HostThread()
        {
            //AddToConsoleBox("Host thread started");
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, porta);
            socket.Bind(localEndPoint);
            socket.Listen(1);
            socket = socket.Accept();
            //AddToConsoleBox("Checkpoint 0");
            stato = "game";
            GetChanges();
        }



        #endregion

        #region client
        public void JoinerThread()
        {
            //ConsoleBox.Items.Add("Joiner thread started");
            IPEndPoint remoteEP = new IPEndPoint(ip, porta);
            socket.Connect(remoteEP);
            //AddToConsoleBox("Checkpoint 0");
            stato = "game";
            byte[] msg = Encoding.ASCII.GetBytes("connesso brooh|");
            int bytesSent = socket.Send(msg);
            GetChanges();
        }
        #endregion

        #region logica
        public void GameTick(object sender, EventArgs e)
        {
            if (stato == "game")
            {                
                label4.Visible = false;
                dgv.Size = new System.Drawing.Size(larghezzaPedina * colonne, altezzaPedina * righe);
                this.dgv.RowTemplate.Height = altezzaPedina; //
                this.dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
                dgv.RowCount = righe;
                dgv.ColumnCount = colonne;
                Aggiorna();                
            }


        }
        private void dgv_CellContentClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            //controlloVittoria();
            int stato = logica.eseguiMossa(e.ColumnIndex);            
            msg = ""+e.ColumnIndex.ToString()+"|";
            socket.Send(Encoding.ASCII.GetBytes(msg));
            Aggiorna();
            switch (stato)
            {
                case -2:
                    MessageBox.Show("Colonna Piena:)");
                    break;
                case -1:
                    CambiaTurnolbl();
                    break;
                case 0:
                    MessageBox.Show("Pareggio :|");
                    CountAvv++;
                    CountPlayer++;
                    break;
                case 1:
                    MessageBox.Show("Giocatore 1 vince :)");
                    CountPlayer++;
                    break;
                case 2:
                    MessageBox.Show("Giocatore 2 vince :)");
                    CountAvv++;
                    break;
            }

            if (stato >= 0)
            {
                Reset();
            }

        }
        private void CambiaTurnolbl()
        {
            if (logica.Turno == 1)
                label3.Text = "Turno: " + turnoPlayer;
            else
                label3.Text = "Turno: " + turnoAvversario;
            //Partita in corso
        }
        public void Aggiorna()
        {
            MatriceSuDgv(logica.Campo);
            ColoraCelle();
        }
        private void Reset()
        {
            logica = new Forza4Logic(righe, colonne);
            Aggiorna();
            CambiaTurnolbl();
        }
        #endregion

        #region celle




        void ColoraCelle()
        {
            for (int i = 0; i < dgv.RowCount; i++)
            {
                for (int j = 0; j < dgv.ColumnCount; j++)
                {

                    int value = Convert.ToInt32(dgv[j, i].Value);
                    if (value == 0)
                        dgv[j, i].Style.BackColor = System.Drawing.Color.Blue;
                    if (value == 1)
                        dgv[j, i].Style.BackColor = System.Drawing.Color.Green;
                    if (value == -1)
                        dgv[j, i].Style.BackColor = System.Drawing.Color.Red;
                }
            }
        }
        private void dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            Rectangle cella = e.CellBounds;

            if (dgv[e.ColumnIndex, e.RowIndex].Style.BackColor == Color.Blue)
            {
                e.Graphics.DrawImage(PedinaVuota, cella);
            }
            if (dgv[e.ColumnIndex, e.RowIndex].Style.BackColor == Color.Red)
            {
                e.Graphics.DrawImage(PedinaAvversario, cella);
            }
            if (dgv[e.ColumnIndex, e.RowIndex].Style.BackColor == Color.Green)
            {
                e.Graphics.DrawImage(PedinaPlayer, cella);
            }
            //inserisco i bordi delle celle
            e.Graphics.DrawRectangle(Pens.Black, cella);
            e.Handled = true;
        }

        void MatriceSuDgv(int[,] matrice)
        {

            for (int i = 0; i < righe; i++)
            {
                for (int j = 0; j < colonne; j++)
                {
                    dgv[j, i].Value = matrice[i, j];
                }
            }
        }

        #endregion

        public void GetChanges()
        {
            while (true)
            {
                string data = null;
                byte[] bytes = new Byte[1024];
                while (true)
                {
                    int bytesRec = socket.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf('|') > -1)
                    {
                        break;
                    }
                }
                Console.WriteLine(data.ToString());
                
            }
        }//legge finchè non trova |
    }
}
