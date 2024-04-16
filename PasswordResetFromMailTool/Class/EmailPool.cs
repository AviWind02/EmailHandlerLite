using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PasswordResetFromMailTool
{
    internal class EmailPool
    {
        private readonly string filePath = "ProcessedEmailIDs.txt";

        public EmailPool()
        {
            // Ensure the file exists
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }
        }

        public bool CheckAndAddEmailIdAsync(string emailId)
        {
            var processedEmailIds = File.ReadAllLines(filePath);
            if (Array.IndexOf(processedEmailIds, emailId) != -1)
            {
                return false; // Email ID already processed
            }

            File.AppendAllText(filePath, emailId + Environment.NewLine);
            return true; // Email ID added to the pool, processing can proceed
        }
    }
}
