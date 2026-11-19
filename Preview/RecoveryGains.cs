namespace YogurtLid.Preview;

/// <summary>食用一份食物后的预计恢复量（已按当前上限裁剪，且不会是负数）。</summary>
internal readonly struct RecoveryGains
{
    public static readonly RecoveryGains None = new(0f, 0f);

    public RecoveryGains(float stamina, float health)
    {
        this.Stamina = stamina;
        this.Health = health;
    }

    /// <summary>体力预计恢复量。</summary>
    public float Stamina { get; }

    /// <summary>生命预计恢复量。</summary>
    public float Health { get; }

    public bool IsEmpty => this.Stamina <= 0f && this.Health <= 0f;
}
