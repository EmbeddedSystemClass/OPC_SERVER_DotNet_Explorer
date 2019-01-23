﻿using System;
using Opc.Ua;
using Opc.Ua.Configuration;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace ServerTest2
{
    public class MyRefServer
    {
        ServerOPC server;
        Task status;
        static bool autoAccept = false;
        static ExitCode exitCode;

        public MyRefServer(bool _autoAccept)
        {
            autoAccept = _autoAccept;
        }

        public void Run()
        {

            try
            {
                exitCode = ExitCode.ErrorServerNotStarted;
                ConsoleSampleServer().Wait();
                Console.WriteLine("Server started. Press Ctrl-C to exit...");
                exitCode = ExitCode.ErrorServerRunning;
            }
            catch (Exception ex)
            {
                Utils.Trace("ServiceResultException:" + ex.Message);
                Console.WriteLine("Exception: {0}", ex.Message);
                exitCode = ExitCode.ErrorServerException;
                return;
            }


            //manually keeps thred into running state 
            ManualResetEvent quitEvent = new ManualResetEvent(false);
            try
            {
                Console.CancelKeyPress += (sender, eArgs) =>
                {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch
            {
            }

            // wait for timeout or Ctrl-C
            quitEvent.WaitOne();

            if (server != null)
            {
                Console.WriteLine("Server stopped. Waiting for exit...");

                using (ServerOPC _server = server)
                {
                    // Stop status thread
                    server = null;
                    status.Wait();
                    // Stop server and dispose
                    _server.Stop();
                }
            }

            exitCode = ExitCode.Ok;
        }

        public static ExitCode ExitCode { get => exitCode; }

        private async Task ConsoleSampleServer()
        {
            // ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationName = "ServerTest2",
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = "ServerTest2"
            };

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false);

            await application.CheckApplicationInstanceCertificate(false, 0);
            server = new ServerOPC();
            await application.Start(server);

            //Console.WriteLine("server is running but endpoints are not exposed");       //for testing

            //print endpoint info
            var endpoints = application.Server.GetEndpoints().Select(e => e.EndpointUrl).Distinct();
            foreach (var endpoint in endpoints)
            {
                Console.WriteLine(endpoint);
            }
        }
    }
}