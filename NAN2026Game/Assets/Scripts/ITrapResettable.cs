namespace NAN2026.Showroom
{
    /// <summary>
    /// A trap that can be returned to its untriggered state, so every respawn
    /// gives the player the exact same level to learn from.
    /// </summary>
    public interface ITrapResettable
    {
        void ResetTrap();
    }
}
