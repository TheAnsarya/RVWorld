using System;

namespace RVXCore
{
    public static class ReportError
    {
        public delegate void ShowError(string message);

        public static ShowError ErrorForm;

        public static void UnhandledExceptionHandler(Exception e)
        {
            try
            {
                // Create Error Message
                string message = $"An Application Error has occurred.\r\n\r\nEXCEPTION:\r\nSource: {e.Source}\r\nMessage: {e.Message}\r\n";
                if (e.InnerException != null)
                {
                    message += $"\r\nINNER EXCEPTION:\r\nSource: {e.InnerException.Source}\r\nMessage: {e.InnerException.Message}\r\n";
                }

                message += $"\r\nSTACK TRACE:\r\n{e.StackTrace}";


                ErrorForm?.Invoke(message);
            }
            catch
            {
            }
        }
    }
}
