namespace YesSql
{
    public interface IBlockIdGenerator
    {
        long GetNextId(string dimension);
    }
}