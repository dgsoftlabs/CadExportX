using ModelSpace;

namespace CadExportX.Tests.Models
{
    public class BlocksInfoTests
    {
        [Fact]
        public void GetValue_ReturnsParameterValue()
        {
            var block = new BlocksInfo();
            block.Parementers.Add(new BlockParam { Name = "CUSTOM_PARAM", Value = "CustomValue" });

            Assert.Equal("CustomValue", block.GetValue("CUSTOM_PARAM"));
        }

        [Fact]
        public void GetValue_ReturnsDefaultForUnknownParameter()
        {
            var block = new BlocksInfo();

            Assert.Equal("...", block.GetValue("UNKNOWN_PARAM"));
        }

        [Fact]
        public void SetParam_UpdatesExistingParameter()
        {
            var block = new BlocksInfo();
            block.Parementers.Add(new BlockParam { Name = "TAG", Value = "OldValue" });

            block.SetParam("TAG", "NewValue");

            Assert.Equal("NewValue", block.GetValue("TAG"));
        }

        [Fact]
        public void SetParam_DoesNotAddNewParameter()
        {
            var block = new BlocksInfo();

            block.SetParam("NEW_TAG", "NewValue");

            Assert.Equal("...", block.GetValue("NEW_TAG"));
        }
    }
}