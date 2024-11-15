using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql
{
    public static class SessionExtensions
    {
        /// <summary>
        /// Loads an object by its id.
        /// </summary>
        /// <returns>The object or <c>null</c>.</returns>
        public static async Task<T> GetAsync<T>(this ISession session, long id, string collection = null)
            where T : class
            => (await session.GetAsync<T>([id], collection)).FirstOrDefault();

        /// <summary>
        /// Loads objects by id.
        /// </summary>
        /// <returns>A collection of objects in the same order they were defined.</returns>
        public static Task<IEnumerable<T>> GetAsync<T>(this ISession session, int[] ids, string collection = null)
            where T : class
            => session.GetAsync<T>(ids.Select(x => (long)x).ToArray(), collection);

        /// <summary>
        /// Imports an object in the local identity map.
        /// </summary>
        /// <remarks>
        /// This method can be used to re-attach an object that exists in the database
        /// but was not loaded from this session, or has been duplicated. If not imported
        /// in a session a duplicate record would tentatively be created in the database
        /// and a duplicate primary key constraint would fail.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the object was imported, <c>false</c> otherwise.
        /// </returns>
        public static bool Import(this ISession session, object item, string collection = null)
            => session.Import(item, 0, 0, collection);

        /// <summary>
        /// Registers index providers that are used only during the lifetime of this session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="indexProviders">The index providers to register.</param>
        /// <returns>The <see cref="ISession"/> instance.</returns>
        public static ISession RegisterIndexes(this ISession session, params IIndexProvider[] indexProviders)
            => session.RegisterIndexes(indexProviders, null);

        /// <summary>
        /// Registers index providers that are used only during the lifetime of this session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="indexProvider">The index provider to register.</param>
        /// <param name="collection">The name of the collection.</param>
        /// <returns>The <see cref="ISession"/> instance.</returns>
        public static ISession RegisterIndexes(this ISession session, IIndexProvider indexProvider, string collection = null)
            => session.RegisterIndexes([indexProvider], collection);

        /// <summary>
        /// Saves a new or existing object to the store, and updates
        /// the corresponding indexes.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="obj">The entity to save.</param>
        /// <param name="collection">The name of the collection.</param>
        [Obsolete($"Instead, utilize the {nameof(SaveAsync)} method. This current method is slated for removal in upcoming releases.")]
        public static void Save(this ISession session, object obj, string collection = null)
            => session.SaveAsync(obj, collection).GetAwaiter().GetResult();

        public static Task SaveAsync(this ISession session, object obj, string collection = null)
            => session.SaveAsync(obj, false, collection);
    }
}
