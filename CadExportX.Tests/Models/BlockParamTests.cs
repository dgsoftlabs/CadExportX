using ModelSpace;

namespace CadExportX.Tests.Models
{
    public class BlockParamTests
    {
        [Fact]
        public void Constructor_InitializesProperties()
        {
            var param = new BlockParam
            {
                Name = "TAG",
                Value = "KKS-001"
            };

            Assert.Equal("TAG", param.Name);
            Assert.Equal("KKS-001", param.Value);
        }

        [Fact]
        public void Name_CanBeSetAndRetrieved()
        {
            var param = new BlockParam { Name = "TestParam" };

            Assert.Equal("TestParam", param.Name);
        }

        [Fact]
        public void Value_CanBeSetAndRetrieved()
        {
            var param = new BlockParam { Value = "TestValue" };

            Assert.Equal("TestValue", param.Value);
        }
    }
}