namespace NotikaIdentityEmail.Logging
{
    public static class LogMessages
    {
        
        public const string UserRegisterStarted = "User register started. Email: {Email}, Username: {Username}";
        public const string UserRegisterSucceeded = "User register succeeded. UserId: {UserId}, Email: {Email}";
        public const string UserRegisterFailed = "User register failed. Email: {Email}, Errors: {Errors}";

        public const string ActivationStarted = "Activation attempt. Email: {Email}, Code: {Code}";
        public const string ActivationSucceeded = "Activation succeeded. UserId: {UserId}, Email: {Email}";
        public const string ActivationFailedWrongCode = "Activation failed - wrong code. Email: {Email}";
        public const string ActivationFailedUserNotFound = "Activation failed - user not found. Email: {Email}";

        public const string LoginSucceeded = "Login succeeded. UserId: {UserId}, Username: {Username}";
        public const string LoginFailed = "Login failed. UsernameOrEmail: {UsernameOrEmail}";

      
        public const string EmailSendStarted = "Email send started. To: {To}, Subject: {Subject}";
        public const string EmailSendSucceeded = "Email send succeeded. To: {To}";
        public const string EmailSendFailed = "Email send failed. To: {To}";

        
        public const string MessageComposeStarted = "Message compose started. Sender: {Sender}, Receiver: {Receiver}, IsDraft: {IsDraft}, CategoryId: {CategoryId}";
        public const string MessageComposeSucceeded = "Message compose succeeded. MessageId: {MessageId}";
        public const string MessageMoveToTrash = "Message moved to trash. MessageId: {MessageId}, User: {User}";
        public const string MessageRead = "Message marked as read. MessageId: {MessageId}, User: {User}";
    }
}
