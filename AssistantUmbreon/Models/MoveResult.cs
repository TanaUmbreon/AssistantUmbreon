using Discord;

namespace AssistantUmbreon.Models;

/// <summary>
/// ボイスチャンネル間のメンバー移動の結果を格納します。このクラスは不変です。
/// </summary>
/// <param name="IsSuccess">移動が成功した事を示す値。</param>
/// <param name="MovedUsers">移動したメンバーの一覧。</param>
/// <param name="Error">失敗時の例外。移動対象メンバーが存在しない場合は <see langword="null"/>。</param>
public record MoveResult(
    bool IsSuccess,
    IReadOnlyList<IGuildUser> MovedUsers,
    Exception? Error = null
);
