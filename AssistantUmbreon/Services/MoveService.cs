using AssistantUmbreon.Models;
using Discord;

namespace AssistantUmbreon.Services;

public class MoveService
{
    public async Task<MoveResult> ExecuteAsync(IVoiceChannel fromVc, IVoiceChannel toVc)
    {
        try
        {
            var users = (await fromVc.GetUsersAsync().FlattenAsync()).ToList();

            if (users.Count == 0)
                return new MoveResult(false, 0, "no_members");

            foreach (var user in users)
                await user.ModifyAsync(x => x.Channel = new Optional<IVoiceChannel?>(toVc));

            return new MoveResult(true, users.Count, null);
        }
        catch (Exception ex)
        {
            return new MoveResult(false, 0, ex.Message);
        }
    }
}
