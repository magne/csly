namespace sly.v3.lexer.regex.util
{
    /// <summary>
    /// Implementations of this interface can cache serializable objects that can be used to bypass expensive building
    /// operations by providing pre-built objects.
    /// </summary>
    internal interface IBuilderCache
    {
        /// <summary>
        /// Get a cached item.
        /// </summary>
        /// <param name="key">The key used to identify the item.  The key uniquely identifies all of the source
        /// information that will go into building the item if this call fails to retrieve a cached version.  Typically
        /// this will be a cryptographic hash of the serialized form of that information.</param>
        /// <returns>the item that was previously cached under the key, or null if no such item can be retrieved.</returns>
        object GetCachedItem(string key);

        /// <summary>
        /// This method may be called when an item is built, providing an opportunity to cache it.
        /// </summary>
        /// <param name="key">The key that will be used to identify the item in future calls to <see cref="GetCachedItem"/>.
        /// Only letters, digits, and underscores are valid in keys, and key length is limited to 32 characters.
        /// The behaviour of this method for invalid keys is undefined.
        ///
        /// Keys that differ only by case may or may not be considered equal by this class.</param>
        /// <param name="item">The item to cache, if desired</param>
        void MaybeCacheItem(string key, object item);    }
}