namespace ForTeacher.Services;

public interface IConfirmationService
{
    bool Confirm(string message, string caption);
}
