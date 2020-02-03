using YesSql.Indexes;

namespace YesSql.Tests.Indexes
{
    public class FileInfo
    {
        public string FileId { get; set; }
        public uint Revision { get; set; }
        public string ContentType { get; set; }
        public string SerializedUserGroups { get; set; }
    }
    public class FileInfoIndex : MapIndex
    {
        public string FileId { get; set; }
        public uint Revision { get; set; }
    }

    public class FileIndexProvider : IndexProvider<FileInfo>
    {
        public override void Describe(DescribeContext<FileInfo> context)
        {
            context.For<FileInfoIndex>()
                .Map(fileInfo => new FileInfoIndex
                {
                    FileId = fileInfo.FileId,
                    Revision = fileInfo.Revision
                });
        }
    }
}
