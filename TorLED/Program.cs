using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TorLED
{
    class Program
    {

        private static String Versioninfo = "Version: 0.4, 14.03.2018";

        private static int returncode = -1;
        private static bool jobdone = false;
        

        private static void Handle_Ready()
        {
            Console.WriteLine("\r\nVerbindung wurde hergestellt. - Controller ist bereit.");
        }

        private static void Handle_Disconnected()
        {
            Console.WriteLine("\r\nVerbindung wurde getrennt.");
            jobdone = true;
        }

        private static void Handle_MsgReceived(object sender, Co0nUtilZ.ProgressEventArgs e)
        {           
           Console.WriteLine("\r\nUnerwartete Antwort: " + e.Message);           
        }

        private static void Handle_Jobdone(object sender, int result)
        {
            Console.WriteLine("\r\nAufgabe wurde beendet.");
            Console.WriteLine("\r\nRückgabecode ist: " + result.ToString());
            returncode = result;
            jobdone = true;
        }
     

        private static void Handle_ErrorOccured(object sender, Co0nUtilZ.ErrorEventArgs e)
        {
            Console.WriteLine("\r\nEs ist ein Fehler aufgetreten: " + e.Err);
            jobdone = true;
        }

    
        /// <summary>
        /// Gibt die Hilfe aus
        /// </summary>
        static private void printHelp()
        {
            Console.WriteLine("\r\n");
            Console.WriteLine("torledcontrol.exe by D. Marx");
            Console.WriteLine("============================");
            Console.WriteLine(Versioninfo);
            Console.WriteLine("");
            Console.WriteLine("Dieses Programm steuert die LED-Beleuchtung an einem Verladetor.");
            Console.WriteLine("Dazu wird ein bestimmter Steuercode an eine (Raspberrypi-basierte)-Steuerung gesendet, auf welcher der passende TCP-Server (dmarxtcp) läuft.");
            Console.WriteLine("");
            Console.WriteLine("Aufruf:");
            Console.WriteLine("#######");
            Console.WriteLine("");
            Console.WriteLine("torledcontrol.exe IP PORT LEDCODE DAUER [TIMEOUT]");
            Console.WriteLine("");
            Console.WriteLine("IP:\tIP-Adresse des Steuergeräts");
            Console.WriteLine("PORT:\tTCP-Port des Steuergeräts");
            Console.WriteLine("LEDCODE:\tSteuercode Rot,Grün,Blau");
            Console.WriteLine("DAUER:\tLeuchtdauer des Steuercodes in Millisekunden (Minimum 250ms). Wenn negativ: Dauerleuchten");
            Console.WriteLine("");
            Console.WriteLine("Beispiel: torledcontrol.exe 192.168.14.124 60666 100 1500 3000");
            Console.WriteLine("Erklärung: Dies versucht für 3000ms eine Verbindung zu 192.168.14.124:60666 aufzubauen. Bei Erfolg werden die LEDs anschließend für 1,5 Sekunden auf Rot gesetzt.");
            Console.WriteLine("");
            Console.WriteLine("LED-Codes:");
            Console.WriteLine("##########");
            Console.WriteLine("");
            Console.WriteLine("Codes bestehen aus drei Bits, mit welchen sich verschiedene Farben darstellen lassen:");
            Console.WriteLine("Erstes Bit: Rot");
            Console.WriteLine("Zweites Bit: Grün");
            Console.WriteLine("Drittes Bit: Blau");
            Console.WriteLine("");
            Console.WriteLine("Kombination:");
            Console.WriteLine("000=Aus");
            Console.WriteLine("100=Rot");
            Console.WriteLine("010=Grün");
            Console.WriteLine("001=Blau");
            Console.WriteLine("110=Gelb");
            Console.WriteLine("011=Türkis");
            Console.WriteLine("101=Violett");
            Console.WriteLine("111=Weiss");
            Console.WriteLine("");
            Console.WriteLine("TIMEOUT (Optional): Gibt an wie lange (in Millisekunden) versucht werden soll eine Verbindung herzustellen.");
            Console.WriteLine("Wird kein Wert angegeben, wird der Standardwert verwendet.");
        }

        

        static int Main(string[] args)
        {


        String _host, _ledcode;
        int _port, _duration;

            C_LED_Control _LED_Control;
            if (args.Count() < 4)
            {
                printHelp();
                return 6;
            }
            else if(args[2].Length != 3)
            {
                Console.WriteLine("\r\nUngültiger RGB-Code\r\n");
                printHelp();
                return 5;
            }
            else
            {

                foreach(char c in args[2]) //Check if RGB-Code is build by 0's and 1's
                {
                    if (c=='1' || c=='0')
                    {
                        //everything is fine 
                    }
                    else
                    {//current char ist not 1 or 0
                        Console.WriteLine("\r\nUngültiger RGB-Code\r\n");
                        printHelp();
                        return 5;
                    }
                }

                try
                {
                   _duration = int.Parse(args[3]);
                    if (_duration>=0 && _duration < 250)
                    {
                        Console.WriteLine("\r\nUngültige Dauer\r\n");
                        return 3;
                    }
                    if (_duration < 0)
                    {
                        Console.WriteLine("\r\nDauer negativ. Schalte LEDs nicht automatisch wieder ab.\r\n");
                    }

                    _port = int.Parse(args[1]);
                    if (_port<=0 || _port > 65535)
                    {
                        Console.WriteLine("\r\nUngültiger Port\r\n");
                        return 3;
                    }
                    _host = args[0];



                    _ledcode = args[2];
                }
                catch
                {
                    printHelp();
                    return 3;
                }
                

                //Main:
                try
                {
                    _LED_Control = new C_LED_Control(_host, _ledcode, _port, _duration); //Neues Objekt erzeugen...
                    //...und konfigurieren
                    _LED_Control.ControllerReady += Handle_Ready;
                    _LED_Control.ErrorOccured += Handle_ErrorOccured;
                    _LED_Control.otherMessagereceived += Handle_MsgReceived;
                    _LED_Control.Jobdone += Handle_Jobdone;
                    _LED_Control.Disconnected += Handle_Disconnected;
                    int timeout = -1;

                    if (args.Count() > 4)
                    {
                        //Fünftes Argument (Timeout) angegeben
                        try
                        {
                            timeout = int.Parse(args[4]);
                        }
                        catch
                        {
                            Console.WriteLine("Ungültiger Wert für Timeout! - Verwende Standardwert...");                            
                        }
                        
                    }
                    if (timeout <= 0)
                    {
                        Console.WriteLine("Ungültiger Wert für Timeout! - Verwende Standardwert...");

                    }
                    
                    _LED_Control.connect(timeout); //Verbindung herstellen. Wenn timeout <=0 ist, wird von der Klasse automatisch der Standardwert verwendet
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return 4;
                }

            }
            
            while (!jobdone)
            {//Warten bis der Job (in welcher Form auch immer) beendet wurde...

            }
            return returncode; //Programm mit dem entsprechenden Rückgabecode beenden
        }
    }
}
