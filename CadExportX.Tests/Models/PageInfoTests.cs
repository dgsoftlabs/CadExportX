using ModelSpace;

namespace CadExportX.Tests.Models
{
    public class PageInfoTests
    {
        [Fact]
        public void Constructor_InitializesDefaultValues()
        {
            var pageInfo = new PageInfo();

            Assert.Null(pageInfo.Path);
            Assert.Null(pageInfo.Sub);
            Assert.False(pageInfo.IsChanged);
            Assert.Empty(pageInfo.Blocks);
            Assert.Equal(ChangesKind.NotChanged, pageInfo.Change);
        }

        [Fact]
        public void Path_CanBeSetAndRetrieved()
        {
            var pageInfo = new PageInfo { Path = "C:/test/sample.dwg" };

            Assert.Equal("C:/test/sample.dwg", pageInfo.Path);
        }

        [Fact]
        public void GetPage_ReturnsShFromFirstBlock()
        {
            var pageInfo = new PageInfo();
            pageInfo.Blocks.Add(new BlocksInfo { Name = "Block1" });

            var page = pageInfo.GetPage();

            Assert.Equal("0", page);
        }

        [Fact]
        public void GetPage_ReturnsDefaultForNoBlocks()
        {
            var pageInfo = new PageInfo();

            var page = pageInfo.GetPage();

            Assert.Equal("???", page);
        }

        [Fact]
        public void GetFileName_ReturnsFileNameWithoutExtension()
        {
            var pageInfo = new PageInfo { Path = "C:/drawings/test_file.dwg" };

            var fileName = pageInfo.GetFileName();

            Assert.Equal("test_file", fileName);
        }

        [Fact]
        public void GetFileName_ReturnsDefaultForNullPath()
        {
            var pageInfo = new PageInfo { Path = null };

            var fileName = pageInfo.GetFileName();

            Assert.Equal("???", fileName);
        }

        [Fact]
        public void GetFileDesc_ReturnsPlaceholder()
        {
            var pageInfo = new PageInfo();

            var desc = pageInfo.GetFileDesc();

            Assert.Equal("???", desc);
        }

        [Fact]
        public void Blocks_CanAddAndRetrieve()
        {
            var pageInfo = new PageInfo();
            var block1 = new BlocksInfo { Name = "Block1" };
            var block2 = new BlocksInfo { Name = "Block2" };

            pageInfo.Blocks.Add(block1);
            pageInfo.Blocks.Add(block2);

            Assert.Equal(2, pageInfo.Blocks.Count);
            Assert.Equal("Block1", pageInfo.Blocks[0].Name);
            Assert.Equal("Block2", pageInfo.Blocks[1].Name);
        }

        [Fact]
        public void ToString_ReturnsPageNumber()
        {
            var pageInfo = new PageInfo();
            pageInfo.Blocks.Add(new BlocksInfo { Name = "TestBlock" });

            var result = pageInfo.ToString();

            Assert.Equal("0", result);
        }

        [Fact]
        public void Change_CanBeSetAndRetrieved()
        {
            var pageInfo = new PageInfo { Change = ChangesKind.Modfied };

            Assert.Equal(ChangesKind.Modfied, pageInfo.Change);
        }
    }
}