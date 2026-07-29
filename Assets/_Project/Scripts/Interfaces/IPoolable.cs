namespace PanicAtThePond.Interfaces
{
    /// <summary>
    /// Implemented by any type whose instances are recycled by <c>PoolManager</c> instead of being
    /// created with <c>Instantiate</c> and thrown away with <c>Destroy</c>.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called immediately after the instance is taken from the pool and activated.</summary>
        void OnSpawn();

        /// <summary>Called immediately before the instance is deactivated and returned to the pool.</summary>
        void OnDespawn();
    }
}
