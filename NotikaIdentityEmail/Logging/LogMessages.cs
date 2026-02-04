namespace NotikaIdentityEmail.Logging
{
    public static class LogMessages
    {

        // Ortak
        public const string UnexpectedError = "Beklenmeyen bir sistem hatası oluştu";
        public const string UserNotFound = "Kullanıcı bulunamadı";
        public const string AccessForbidden = "İşlem için yetki bulunamadı";
        public const string MessageNotFound = "Mesaj bulunamadı";
        public const string ActivationCodeInvalid = "Aktivasyon kodu hatalı";
        public const string ActivationEmailFailed = "Aktivasyon e-postası gönderilemedi";
        public const string UserRoleMissing = "Kullanıcıya rol atanmadığı için giriş tamamlanamadı";
    }

      
    public static class AuthLogMessages
    {
        public const string UserLoginSuccess = "Kullanıcı başarıyla giriş yaptı";
        public const string UserLoginFailed = "Kullanıcı giriş yapamadı";
        public const string UserLogout = "Kullanıcı çıkış yaptı";
        public const string UserActivated = "Kullanıcı hesabı aktifleştirildi";
    }

   
    public static class UserLogMessages
    {
        public const string UserCreated = "Yeni kullanıcı oluşturuldu";
        public const string UserUpdated = "Kullanıcı bilgileri güncellendi";
        public const string UserDeleted = "Kullanıcı silindi";
        public const string RoleAssigned = "Kullanıcıya rol atandı";
        public const string RoleRemoved = "Kullanıcıdan rol kaldırıldı";
        public const string UserCreateFailed = "Kullanıcı oluşturulamadı";
    }

    public static class MessageLogMessages
    {
        public const string MessageSent = "Yeni mesaj gönderildi";
        public const string MessageRead = "Mesaj okundu";
        public const string MessageDeleted = "Mesaj silindi";
        public const string MessageMovedToTrash = "Mesaj çöp kutusuna taşındı";
        public const string MessageRestored = "Mesaj çöp kutusundan geri alındı";
        public const string MessageDraftSaved = "Mesaj taslak olarak kaydedildi";
    }

    public static class SystemLogMessages
    {
        public const string SystemStarted = "Uygulama başlatıldı";
        public const string SystemStopped = "Uygulama durduruldu";
    }

    public static class LogContextValues
    {
        public const string OperationAuth = "Auth";
        public const string OperationUser = "User";
        public const string OperationMessage = "Message";
        public const string OperationSystem = "System";

        public const string CategoryFallback = "Kategori Yok";


        public const string MessageStatusRead = "Okundu";
        public const string MessageStatusUnread = "Okunmadı";
        public const string MessageStatusDraft = "Taslak";
        public const string MessageStatusTrash = "Çöp";
    }
}