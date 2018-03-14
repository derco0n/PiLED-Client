using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace TorLED
{
    class C_LED_Control
    {

        #region variables

        private String _host, _ledcode;
        private int _port, _duration, _okcount=0, _okshouldbe = 2;


        private Co0nUtilZ.C_NetClient tcpclient;
        //private TorLED.C_NetClient tcpclient;
        #endregion

        #region methods

        public void connect(int timeout=-1)
        {
            if (this.tcpclient!=null && !this.tcpclient.IsConnected)
            {
                if (timeout > 0)
                {//Abweichender Timeout wurde angegeben und ist gültig
                    this.tcpclient.ConnectTimeout = timeout; //...diesen setzen
                }
                this.tcpclient.ConnectToServer(); //Verbindung herstellen.
            }          
        }
        #endregion

        #region delegates_and_events
        public delegate void Readyhandler();
        public delegate void Errorhandler(object sender, Co0nUtilZ.ErrorEventArgs Args);
        public delegate void Resulthandler(object sender, int returncode);
        public delegate void Progresshandler(object sender, Co0nUtilZ.ProgressEventArgs Args);
        public event Readyhandler ControllerReady;
        public event Readyhandler Disconnected;
        public event Resulthandler Jobdone;
        public event Errorhandler ErrorOccured;
        public event Progresshandler otherMessagereceived;
        

        #endregion

        #region handlers

        private void Handle_Connected(object sender, Co0nUtilZ.ProgressEventArgs e)
        {
            
        }

        private void Handle_Disconnected(object sender, Co0nUtilZ.ProgressEventArgs e)
        {
            if (this.Disconnected != null)
            {
                this.Disconnected();
            }
        }

        private void Handle_DataReceived(object sender, Co0nUtilZ.ProgressEventArgs e)
        {
            
            if (e.Message.StartsWith("LED-Control ready."))
            {//Schritt eins
                if (this.ControllerReady != null)
                {
                    this.ControllerReady();
                }

                if (this._okcount == 0)
                {
                    this.tcpclient.SendData(this._ledcode + "\r\n"); //Set LEDs on
                }

            }
            else if (e.Message.StartsWith("command ok"))
            {
                this._okcount += 1;
                if (this._okcount == 1)
                { //Schritt zwei
                    if (this._duration > 0)
                    {//Set LEDs back to normal if duration is > 0. if it is set below zero, LEDs will stay on forever
                        Thread.Sleep(this._duration);//Wait
                        this.tcpclient.SendData("000\r\n"); //Set LEDS Off
                    }
                    else
                    {//Exit directly
                        Thread.Sleep(50); //Wait 50ms
                        this.tcpclient.SendData("exit\r\n"); //Verbindung trennen

                    }
                }
                else if (this._okcount == 2)
                {//Duration must be above 
                    Thread.Sleep(50); //Wait 50ms
                    this.tcpclient.SendData("exit\r\n"); //Verbindung trennen
                }
            }
            else if (e.Message.StartsWith("good bye"))
            {
                if (this.Jobdone != null)
                {

                this.Jobdone(this, this._okshouldbe - this._okcount); //0 wenn Erfolg                 
                   
                }

                
            }
            else if (e.Message.StartsWith("command error"))
            {//Fehler: ungültiger Befehl
                if (this.ErrorOccured != null)
                {
                    this.ErrorOccured(this, new Co0nUtilZ.ErrorEventArgs("Error: " + e.Message));
                }
            }
            else
            {
                if (this.otherMessagereceived != null)
                {
                    this.otherMessagereceived(this, new Co0nUtilZ.ProgressEventArgs(0, e.Message));
                }

               
            }

        }

        private void Handle_DataSent(object sender, Co0nUtilZ.ProgressEventArgs e)
        {


        }

        private void Handle_ErrorOccured(object sender, Co0nUtilZ.ErrorEventArgs e)
        {
           if (this.ErrorOccured != null)
            {
                this.ErrorOccured(this, e);
            }
        }

        #endregion

        #region contructor

        public C_LED_Control(String Host, String LEDCODE, int Port, int Duration)
        {

            this._host = Host;
            this._ledcode = LEDCODE;
            this._port = Port;
            this._duration = Duration;

            if (this._duration < 0)
            {
                //Wenn die Dauer negativ ist, sollen die LEDs nicht wieder abgeschaltet werden.
                //Wir benötigen daher ein Empfangs-OK weniger vom Server
                this._okshouldbe = this._okshouldbe-1;
            }


            this.tcpclient = new Co0nUtilZ.C_NetClient(this._host, this._port);
            //this.tcpclient = new TorLED.C_NetClient(this._host, this._port);
            this.tcpclient.ConnectTimeout = 1300; //Maximal x Millisekunden versuchen eine Verbindung aufzubauen.

            //Events abbonieren
            this.tcpclient.Connected += this.Handle_Connected;
            this.tcpclient.Disconnected += this.Handle_Disconnected;
            this.tcpclient.DataReceived += this.Handle_DataReceived;
            this.tcpclient.DataSent += this.Handle_DataSent;
            this.tcpclient.OnError += this.Handle_ErrorOccured;
            
        }

        

        #endregion
    }
}
