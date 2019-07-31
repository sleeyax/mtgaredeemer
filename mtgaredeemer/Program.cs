using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using mtgalib.Local;
using mtgalib.Player;
using mtgalib.Server;

namespace mtgaredeemer
{
    class Program
    {
        private static int _redeemedSuccess;
        private static int _redeemFailure;
        private static string _ticket;

        static void Main(string[] args)
        {
            Console.Title = "MTGA code redeemer by Sleeyax";

            string inputFile = "codes.txt";
            while (!File.Exists(inputFile))
            {
                Console.Write("Path to .txt file containing the codes to redeem: ");
                inputFile = Console.ReadLine();
            }

            RedeemCodes(inputFile).GetAwaiter().GetResult();
            Console.Write("Press any key to continue...");
            Console.ReadKey();
            Environment.Exit(0);
        }

        static void RedeemSuccess(string code)
        {
            _redeemedSuccess++;
            Console.WriteLine($"[+] successfully redeemed code '{code}'");
        }

        static void RedeemFailure(string code)
        {
            _redeemFailure++;
            Console.WriteLine($"[-] failed to redeem code '{code}'");
        }

        static void ShowSummary()
        {
            Console.WriteLine();
            Console.WriteLine("Amount of codes redeemed: " + _redeemedSuccess);
            Console.WriteLine("Amount of codes that didn't work : " + _redeemFailure);
        }

        static async Task Login()
        {
            InstalledGame game = new InstalledGame("");
            string token = game.GetRefreshToken();

            PlayerCredentials credentials;
            if (token != null)
            {
                credentials = new PlayerCredentials(token);
            }
            else
            {
                Console.WriteLine("Please login (or try to launch the game once)");
                Console.Write("Email: ");
                string email = Console.ReadLine();
                Console.Write("Password: ");
                string password = Console.ReadLine();

                credentials = new PlayerCredentials(email, password);
            }

            bool verified = await credentials.VerifyAsyncTask();
            
            if (!verified)
            {
                
                Console.Write("[!] Authentication failed!");
               
                // if we tried to login using a token, it's probably expired
                if (token != null)
                    Console.WriteLine("Please launch your game, login once and then run this tool again!");

                Console.ReadKey();
                Environment.Exit(0);
            }

            _ticket = credentials.AccessToken;
        }

        static async Task RedeemCodes(string codesList)
        {
            try
            {
                await Login();

                var server = new GameServer(await PlayerEnvironment.GetDefaultEnvironmentAsyncTask());
                await server.ConnectTask();
                await server.AuthenticateAsyncTask(_ticket);

                using (StreamReader sr = new StreamReader(codesList))
                {
                    while (!sr.EndOfStream)
                    {
                        string code = sr.ReadLine();

                        var response = await server.RedeemCodeAsyncTask(_ticket, code);
                        if (response.result != null)
                        {
                            RedeemSuccess(code);
                        }
                        else
                        {
                            RedeemFailure(code);
                        }
                    }
                }

                ShowSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[!] Unexpected error: " + ex.Message + ", " + ex.StackTrace);
            }
        }
    }
}
