namespace VeterinaryBackend.Domain.Entities;

/// <summary>
/// Lifecycle of an incoming bot request as the admin processes it.
/// Stored as int for forward-compatibility — adding a new state never breaks existing rows.
/// </summary>
public enum BotMessageStatus
{
    /// <summary>Just received — admin has not started processing.</summary>
    Pending = 0,

    /// <summary>Admin is handling the request.</summary>
    InProgress = 1,

    /// <summary>Successfully resolved.</summary>
    Completed = 2,

    /// <summary>Rejected — admin cannot or will not process.</summary>
    Rejected = 3
}
