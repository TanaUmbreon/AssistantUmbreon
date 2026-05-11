using AssistantUmbreon.Models;
using Discord;

namespace AssistantUmbreon.Services;

/// <summary>
/// ボイス チャンネル間のメンバー移動を行うサービスを提供します。
/// </summary>
public class MoveService
{
    /// <summary>
    /// 指定したボイス チャンネルに参加している全メンバーを移動します。この操作は非同期です。
    /// </summary>
    /// <param name="fromVc">移動元のボイス チャンネル。</param>
    /// <param name="toVc">移動先のボイス チャンネル。</param>
    /// <returns>戻り値に移動結果を持つ非同期操作。</returns>
    public async Task<MoveResult> ExecuteAsync(IVoiceChannel fromVc, IVoiceChannel toVc)
    {
        var movedUsers = new List<IGuildUser>();

        try
        {
            // Note: `fromVc.GetUsersAsync()` だけだと、チャット開いているだけで VC に参加していないメンバーも含まれている？
            var users = (await fromVc.GetUsersAsync().FlattenAsync())
                .Where(u => u.VoiceChannel is not null && u.VoiceChannel.Id == fromVc.Id)
                .ToList();

            if (users.Count == 0)
            {
                return new MoveResult(false, movedUsers);
            }

            foreach (var user in users)
            {
                await user.ModifyAsync(x => x.Channel = new Optional<IVoiceChannel?>(toVc));
                movedUsers.Add(user);
            }

            return new MoveResult(true, movedUsers);
        }
        catch (Exception ex)
        {
            return new MoveResult(false, movedUsers, ex);
        }
    }
}
