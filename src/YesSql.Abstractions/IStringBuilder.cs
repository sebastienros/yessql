// Inspired from https://raw.githubusercontent.com/dotnet/runtime/59c592cc8d2778bcc6173baa2b25b13190e42990/src/libraries/Common/src/System/Text/ValueStringBuilder.cs

using System;

namespace YesSql
{
    /// <summary>
    /// Represents a mutable string buffer used to build SQL statements efficiently.
    /// </summary>
    public interface IStringBuilder
    {
        /// <summary>
        /// Gets a reference to the character at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the character.</param>
        ref char this[int index] { get; }

        /// <summary>
        /// Gets the number of characters that can be stored before the buffer needs to grow.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets or sets the number of characters currently in the buffer.
        /// </summary>
        int Length { get; set; }

        /// <summary>
        /// Appends a span of characters to the end of the buffer.
        /// </summary>
        /// <param name="value">The characters to append.</param>
        void Append(ReadOnlySpan<char> value);

        /// <summary>
        /// Appends a string to the end of the buffer.
        /// </summary>
        /// <param name="s">The string to append.</param>
        void Append(string s);

        /// <summary>
        /// Returns a span over the entire content of the buffer.
        /// </summary>
        /// <returns>A span over the buffer content.</returns>
        ReadOnlySpan<char> AsSpan();

        /// <summary>
        /// Returns a span over the content of the buffer starting at the specified position.
        /// </summary>
        /// <param name="start">The zero-based starting position.</param>
        /// <returns>A span over the buffer content from <paramref name="start"/>.</returns>
        ReadOnlySpan<char> AsSpan(int start);

        /// <summary>
        /// Returns a span over a range of the content of the buffer.
        /// </summary>
        /// <param name="start">The zero-based starting position.</param>
        /// <param name="length">The number of characters in the span.</param>
        /// <returns>A span over the requested range of the buffer content.</returns>
        ReadOnlySpan<char> AsSpan(int start, int length);

        /// <summary>
        /// Removes all characters from the buffer.
        /// </summary>
        void Clear();

        /// <summary>
        /// Ensures the buffer can hold at least the specified number of characters without growing.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        void EnsureCapacity(int capacity);

        /// <summary>
        /// Inserts a character repeated a number of times at the specified position.
        /// </summary>
        /// <param name="index">The zero-based position to insert at.</param>
        /// <param name="value">The character to insert.</param>
        /// <param name="count">The number of times to insert the character.</param>
        void Insert(int index, char value, int count);

        /// <summary>
        /// Inserts a string at the specified position.
        /// </summary>
        /// <param name="index">The zero-based position to insert at.</param>
        /// <param name="s">The string to insert.</param>
        void Insert(int index, string s);

        /// <summary>
        /// Returns the content of the buffer as a string.
        /// </summary>
        /// <returns>The buffer content.</returns>
        string ToString();

        /// <summary>
        /// Attempts to copy the content of the buffer to the destination span.
        /// </summary>
        /// <param name="destination">The span to copy the content to.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
        /// <returns><c>true</c> if the content was copied successfully; otherwise, <c>false</c>.</returns>
        bool TryCopyTo(Span<char> destination, out int charsWritten);
    }
}