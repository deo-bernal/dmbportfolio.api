namespace Dmb.Model.Enums;

public enum RegisterWithActivationOutcome
{
    Success,
    DuplicateEmail,
    DuplicateUsernameFirstLast,
    ActivationEmailSendFailed
}

public enum ActivateAccountOutcome
{
    Success,
    InvalidOrExpiredToken
}
