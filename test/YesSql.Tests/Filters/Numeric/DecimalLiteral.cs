using Parlot;
using Parlot.Fluent;
using System;
using System.Globalization;

namespace YesSql.Tests.Filters.Numeric
{
    public sealed class CustomDecimalLiteral : Parser<decimal>
    {
        private readonly NumberOptions _numberOptions;

        public CustomDecimalLiteral(NumberOptions numberOptions = NumberOptions.Default)
        {
            _numberOptions = numberOptions;
        }

        public override bool Parse(ParseContext context, ref ParseResult<decimal> result)
        {
            context.EnterParser(this);

            var reset = context.Scanner.Cursor.Position;
            var start = reset.Offset;

            if ((_numberOptions & NumberOptions.AllowSign) == NumberOptions.AllowSign)
            {
                if (!context.Scanner.ReadChar('-'))
                {
                    // If there is no '-' try to read a '+' but don't read both.
                    context.Scanner.ReadChar('+');
                }
            }

            if (ReadDecimal(context.Scanner, out _))
            {
                var end = context.Scanner.Cursor.Offset;
#if NETSTANDARD2_0
                var sourceToParse = context.Scanner.Buffer.Substring(start, end - start);
#else
                var sourceToParse = context.Scanner.Buffer.AsSpan(start, end - start);
#endif

                if (decimal.TryParse(sourceToParse, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out var currentCulturevalue))
                {
                    result.Set(start, end, currentCulturevalue);
                    return true;
                }

                if (decimal.TryParse(sourceToParse, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var invariantCultureValue))
                {
                    result.Set(start, end, invariantCultureValue);
                    return true;
                }
            }

            context.Scanner.Cursor.ResetPosition(reset);

            return false;
        }

        public bool ReadDecimal(Scanner scanner, out TokenResult result)
        {
            // perf: fast path to prevent a copy of the position

            var currentCultureSeperator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
            var invariantCultureSeperator = '.';
            var rangeSeperator = '.';

            if (!Character.IsDecimalDigit(scanner.Cursor.Current))
            {
                result = TokenResult.Fail();
                return false;
            }

            var start = scanner.Cursor.Position;

            do
            {
                scanner.Cursor.Advance();

            } while (!scanner.Cursor.Eof && Character.IsDecimalDigit(scanner.Cursor.Current));

            if (scanner.Cursor.Match(currentCultureSeperator) && scanner.Cursor.PeekNext(1) != rangeSeperator ||
                scanner.Cursor.Match(invariantCultureSeperator) && scanner.Cursor.PeekNext(1) != rangeSeperator
                )
            {
                scanner.Cursor.Advance();
                if (!Character.IsDecimalDigit(scanner.Cursor.Current))
                {
                    result = TokenResult.Fail();
                    scanner.Cursor.ResetPosition(start);
                    return false;
                }

                do
                {
                    scanner.Cursor.Advance();

                } while (!scanner.Cursor.Eof && Character.IsDecimalDigit(scanner.Cursor.Current));
            }

            result = TokenResult.Succeed(scanner.Buffer, start.Offset, scanner.Cursor.Offset);
            return true;
        }
    }
}
