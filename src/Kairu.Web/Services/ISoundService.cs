namespace Kairu.Web.Services;

public interface ISoundService
{
    Task PlayRingtoneAsync(string ringtonePreference);
}
