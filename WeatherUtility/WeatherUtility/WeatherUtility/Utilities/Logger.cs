using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace WeatherUtility.Utilities
{

    public static class Logger
    {

        #region ' Global Variables '

        private static ConcurrentQueue<string> _logsQueue;
        private static ConcurrentQueue<string> _logExceptionsQueue;

        private static bool _isProcessingLogs;
        private static bool _isProcessingErrors;

        private static readonly string LogLocalDir = Environment.MachineName == "SWITCHPC-MSI" ? @"C:\Logs" : Directory.GetCurrentDirectory() + @"\Logs";

        #endregion

        #region ' Constructor '

        static Logger( )
        {
            _logsQueue = new ConcurrentQueue<string>( );
            if ( !Directory.Exists( LogLocalDir ) )
                Directory.CreateDirectory( LogLocalDir );
        }

        #endregion

        #region ' Methods / Functions '

        public static void LocalLog( string classname, string method, string logtype, string msg )
        {
            try
            {
                if ( _logsQueue == null )
                    _logsQueue = new ConcurrentQueue<string>( );

                StringBuilder log = new StringBuilder( );
                log.AppendLine( $"{DateTime.Now.ToString( "yyyy-MMM-dd HH:mm:ss" )}   SwitchWeatherService from {classname}/{method}  " );
                log.AppendLine( $"{logtype}: {msg} " );
                log.AppendLine( "    " );

                _logsQueue.Enqueue( log.ToString( ) );
                if ( !_isProcessingLogs )
                {
                    _isProcessingLogs = true;
                    ThreadPool.QueueUserWorkItem( ProcessOutgoingLogs );
                }
            }
            catch ( Exception )
            {
            }
        }

        public static void ErrorLog( string classname, string method, string errormsg )
        {
            try
            {
                if ( _logExceptionsQueue == null )
                    _logExceptionsQueue = new ConcurrentQueue<string>( );

                StringBuilder error = new StringBuilder( );
                error.AppendLine( $"{DateTime.Now.ToString( "yyyy-MMM-dd HH:mm:ss" )}   SwitchWeatherService from {classname}/{method}  " );
                error.AppendLine( $"LogException:  {errormsg} " );
                error.AppendLine( "  " );

                _logExceptionsQueue.Enqueue( error.ToString( ) );
                if ( !_isProcessingErrors )
                {
                    _isProcessingErrors = true;
                    ThreadPool.QueueUserWorkItem( ProcessOutgoingErrors );
                }
            }
            catch ( Exception )
            {
            }
        }

        #endregion

        #region ' Events '

        private static void ProcessOutgoingLogs( object state )
        {
            try
            {
                var str = "";
                var file = Path.Combine( LogLocalDir, DateTime.Now.ToString( "dd_MM_yyyy_" ) + "SwitchWeatherService_ProcessLogs.log" );

                while ( _logsQueue.TryDequeue( out str ) )
                    try
                    {
                        File.AppendAllText( file, str );
                    }
                    catch ( Exception )
                    {
                    }
            }
            catch ( Exception )
            {
            }
            finally
            {
                _isProcessingLogs = false;
            }
        }

        private static void ProcessOutgoingErrors( object state )
        {
            try
            {
                var str = "";
                var file = Path.Combine( LogLocalDir, DateTime.Now.ToString( "dd_MM_yyyy_" ) + "SwitchWeatherService_ErrorLogs.log" );

                while ( _logExceptionsQueue.TryDequeue( out str ) )
                    try
                    {
                        File.AppendAllText( file, str );
                    }
                    catch ( Exception )
                    {
                    }
            }
            catch ( Exception )
            {
            }
            finally
            {
                _isProcessingErrors = false;
            }
        }

        #endregion

    }

}