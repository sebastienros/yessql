using System;

namespace YesSql.Core.Data
{
    public interface IIdentifierFactory
    {
        IIdAccessor CreateAccessor(Type tContainer, string name);
    }
}