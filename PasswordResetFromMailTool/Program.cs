using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordResetFromMailTool
{
    internal class Program
    {
        static string[] Scopes = { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailSend, GmailService.Scope.MailGoogleCom };
        static string ApplicationName = "Gmail API .NET Quickstart";

        static async Task Main(string[] args)
        {
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            while (true)
            {
                await FetchEmails(service);
                System.Threading.Thread.Sleep(30000); // Poll every 30 seconds
            }
        }

        static async Task FetchEmails(GmailService service)
        {
            try
            {
                DateTime dateFrom = DateTime.UtcNow.AddMinutes(-30);
                string query = $"is:unread from:{}";

                Console.WriteLine("Checking for new emails...");
                var request = service.Users.Messages.List("me");
                request.Q = query;

                var response = await request.ExecuteAsync();
                IList<Message> messages = response.Messages;

                if (messages != null && messages.Count > 0)
                {
                    foreach (var messageItem in messages)
                    {
                        var emailPool = new EmailPool();
                        bool shouldProcess = emailPool.CheckAndAddEmailIdAsync(messageItem.Id);
                        if (shouldProcess)
                        {
                            var message = await service.Users.Messages.Get("me", messageItem.Id).ExecuteAsync();
                            var emailBody = await GetMessageBody(service, message.Id);
                            var username = ExtractUsername(emailBody);
                            var emailDetails = await GetEmailDetails(service, message.Id);

                            Console.WriteLine($"{{Email: {messageItem.Id}, Username: {username}}}"); // EmailBody Add Funtion is to Reset PW
                            Console.WriteLine($"To: {emailDetails.To} | From: {emailDetails.From}");//Debug Line for checking email From and To

                            await SendEmail(service, emailDetails.From, "Password Reset Received", "This is a test email from C# via Gmail API.");//Send Email After Reset
                        }
                        else
                        {
                            Console.WriteLine($"Unique ID Found: {messageItem.Id}. Skipping.");
                        }
                   

                    }
                }
                else
                {
                    Console.WriteLine("No new messages found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to fetch emails: " + ex.Message);
            }
        }

        static async Task<string> GetMessageBody(GmailService service, string messageId)
        {
            // Create a request to get the message with the specified format
            var request = service.Users.Messages.Get("me", messageId);
            request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;  // Set the format to Full here

            var message = await request.ExecuteAsync();
            if (message.Payload?.Parts == null && message.Payload?.Body != null)
            {
                return DecodeBase64String(message.Payload.Body.Data);
            }

            if (message.Payload?.Parts != null)
            {
                foreach (var part in message.Payload.Parts)
                {
                    if (part.MimeType == "text/plain")
                    {
                        return DecodeBase64String(part.Body.Data);
                    }
                    else if (part.Parts != null)
                    {
                        foreach (var subpart in part.Parts)
                        {
                            if (subpart.MimeType == "text/plain")
                            {
                                return DecodeBase64String(subpart.Body.Data);
                            }
                        }
                    }
                }
            }

            return null; // Return null if no suitable part is found
        }

        static string ExtractUsername(string emailBody)
        {
            // Simple regex to extract username before any signature
            Regex regex = new Regex(@"^\s*(\S+)");
            var match = regex.Match(emailBody);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return "No username found";
        }

        static string DecodeBase64String(string base64Encoded)
        {
            if (string.IsNullOrEmpty(base64Encoded))
                return string.Empty;

            var base64EncodedBytes = Convert.FromBase64String(base64Encoded.Replace("-", "+").Replace("_", "/"));
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }


        static async Task SendEmail(GmailService service, string to, string subject, string bodyText)
        {
            try
            {
                var emailBody = new StringBuilder();
                emailBody.AppendLine("From: me");
                emailBody.AppendLine($"To: {to}");
                emailBody.AppendLine($"Subject: {subject}");
                emailBody.AppendLine("Content-Type: text/plain; charset=utf-8");
                emailBody.AppendLine();
                emailBody.AppendLine(bodyText);

                var message = new Message
                {
                    Raw = Base64UrlEncode(emailBody.ToString())
                };

                try
                {
                    var request = service.Users.Messages.Send(message, "me");
                    await request.ExecuteAsync();
                    Console.WriteLine("Email sent successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send email: {ex.Message}");
                }
            }
            catch (GoogleApiException ex)
            {
                Console.WriteLine($"API returned error: {ex.Error.Message}");
                Console.WriteLine($"Status Code: {ex.HttpStatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }

        static string Base64UrlEncode(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            // Base64Url encode the input
            return Convert.ToBase64String(inputBytes)
                .Replace('+', '-')  // 62nd char of encoding
                .Replace('/', '_')  // 63rd char of encoding
                .Replace("=", "");  // Remove padding
        }


        static async Task<(string Body, string To, string From)> GetEmailDetails(GmailService service, string messageId)
        {
            var request = service.Users.Messages.Get("me", messageId);
            request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full; // Fetch the full message data
            var message = await request.ExecuteAsync();

            string body = "";
            string to = "";
            string from = "";

            if (message.Payload.Parts == null && message.Payload.Body != null)
            {
                body = DecodeBase64String(message.Payload.Body.Data);
            }
            else if (message.Payload.Parts != null)
            {
                foreach (var part in message.Payload.Parts)
                {
                    if (part.MimeType == "text/plain")
                    {
                        body = DecodeBase64String(part.Body.Data);
                    }
                }
            }

            // Retrieve headers for To and From
            foreach (var header in message.Payload.Headers)
            {
                if (header.Name == "To")
                {
                    to = header.Value;
                }
                else if (header.Name == "From")
                {
                    from = header.Value;
                }
            }

            return (Body: body, To: to, From: from);
        }



    }
}
