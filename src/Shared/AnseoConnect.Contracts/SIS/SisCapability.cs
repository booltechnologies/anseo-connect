namespace AnseoConnect.Contracts.SIS;

/// <summary>
/// Represents the sync capabilities that a SIS connector can support.
/// </summary>
public enum SisCapability
{
    /// <summary>
    /// Can sync student roster data.
    /// </summary>
    RosterSync,

    /// <summary>
    /// Can sync guardian/contact information.
    /// </summary>
    ContactsSync,

    /// <summary>
    /// Can sync attendance marks.
    /// </summary>
    AttendanceSync,

    /// <summary>
    /// Can sync class/group information.
    /// </summary>
    ClassesSync,

    /// <summary>
    /// Can sync timetable/period information.
    /// </summary>
    TimetableSync
}
