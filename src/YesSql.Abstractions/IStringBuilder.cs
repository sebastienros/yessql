// Inspired from https://raw.githubusercontent.com/dotnet/runtime/59c592cc8d2778bcc6173baa2b25b13190e42990/src/libraries/Common/src/System/Text/ValueStringBuilder.cs

using System;

namespace YesSql
{
    public interface IStringBuilder
    {
        ref char this[int index] { get; }

        int Capacity { get; }
        int Length { get; set; }

        void Append(ReadOnlySpan<char> value);
        void Append(string s);
        ReadOnlySpan<char> AsSpan();
        ReadOnlySpan<char> AsSpan(int start);
        ReadOnlySpan<char> AsSpan(int start, int length);
        void Clear();
        void EnsureCapacity(int capacity);
        void Insert(int index, char value, int count);
        void Insert(int index, string s);
        string ToString();
        bool TryCopyTo(Span<char> destination, out int charsWritten);
    }
}