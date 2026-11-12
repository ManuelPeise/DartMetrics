using Shared.Enums.Email;

namespace Logic.Shared.Mail
{
    public static class EmailBodyFactory
    {
        public static string GetEmailBody(EmailBodyTypeEnum emailBodyType, string? url = null)
        {
            switch (emailBodyType)
            {
                case EmailBodyTypeEnum.EmailVerification:
                    return GetEmailVerificationBody(url);
                case EmailBodyTypeEnum.AccountActivation:
                    return GetAccountActivationBody(url);
                default:
                    throw new NotImplementedException($"Email body type {emailBodyType} is not implemented.");
            }
        }

        private static string GetEmailVerificationBody(string? verificationUrl)
        {
            if (string.IsNullOrEmpty(verificationUrl))
            {
                throw new ArgumentNullException(nameof(verificationUrl), "Verification URL cannot be null or empty.");
            }

            return $@"
                <html>
                    <body>
                        <h1>Welcome to Our Service</h1>
                        <p>Thank you for signing up! To complete your registration, please verify your email address.</p>
                        <p>Please click the link below to verify your email address:</p>
                        <a href='{verificationUrl}'>Verify Email</a>
                        <br/>
                        <br/>
                        <p>If you did not sign up for this account, please ignore this email.</p>
                        <br/>
                        <p>Best regards,</p>
                        <p>The Team</p>
                    </body>
                </html>";
        }

        private static string GetAccountActivationBody(string applicationUrl)
        {
            if (string.IsNullOrEmpty(applicationUrl))
            {
                throw new ArgumentNullException(nameof(applicationUrl), "Application URL cannot be null or empty.");
            }
            return $@"
                <html>
                    <body>
                        <h1>Your Account is Now Active</h1>
                        <p>Congratulations! Your account has been successfully activated.</p>
                        <p>You can now log in and start using our services.</p>
                        <a href='{applicationUrl}'>Go to Application</a>
                        <br/>
                        <br/>
                        <p>If you have any questions, feel free to contact our support team.</p>
                        <br/>
                        <p>Best regards,</p>
                        <p>The Team</p>
                    </body>
                </html>";
        }
    }
}
