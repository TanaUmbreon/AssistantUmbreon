using Discord;

namespace AssistantUmbreon.Models;

/// <summary>
/// 移動予定を格納します。
/// </summary>
public class ScheduledJob
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 移動元のチャンネルを取得します。
    /// </summary>
    public required IVoiceChannel FromVc { get; init; }

    /// <summary>
    /// 移動先のチャンネルを取得します。
    /// </summary>
    public required IVoiceChannel ToVc { get; init; }

    /// <summary>
    /// 移動を実行する日時を取得します。
    /// </summary>
    public required DateTimeOffset ExecuteAt { get; init; }


    public required ulong RequestedBy { get; init; }
    public required ulong NotifyChannelId { get; init; }
    public CancellationTokenSource CancelToken { get; } = new();
}
