namespace Dmb.Model.Enums;

public enum RegisterWithActivationOutcome
{
    Success,
    DuplicateEmail,
    ActivationEmailSendFailed
}

public enum ActivateAccountOutcome
{
    Success,
    InvalidOrExpiredToken
}
