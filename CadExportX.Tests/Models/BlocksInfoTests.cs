using ModelSpace;

namespace CadExportX.Tests.Models
{
    public class BlocksInfoTests
    {
        [Fact]
        public void GetValue_ReturnsId()
        {
            var block = new BlocksInfo { Id = 42 };

            Assert.Equal("42", block.GetValue("ID"));
        }

        [Fact]
        public void GetValue_ReturnsBlockName()
        {
            var block = new BlocksInfo { Name = "SIG-01-MAIN" };

            Assert.Equal("SIG-01-MAIN", block.GetValue("BLOCK_NAME"));
        }

        [Fact]
        public void GetValue_ReturnsPagePath()
        {
            var block = new BlocksInfo { PagePath = "C:/dwg/sample.dwg" };

            Assert.Equal("C:/dwg/sample.dwg", block.GetValue("BL_PATH"));
        }

        [Fact]
        public void GetValue_ReturnsFolder()
        {
            var block = new BlocksInfo { Sub = "SUB1" };

            Assert.Equal("SUB1", block.GetValue("FOLDER"));
        }

        [Fact]
        public void GetValue_ReturnsCoordinates()
        {
            var block = new BlocksInfo
            {
                X = 12.3456,
                Y = 65.4321
            };

            var expectedX = block.X.ToString("F2");
            var expectedY = block.Y.ToString("F2");

            Assert.Equal(expectedX, block.GetValue("CORD_X"));
            Assert.Equal(expectedY, block.GetValue("CORD_Y"));
        }

        [Fact]
        public void GetValue_ReturnsScope()
        {
            var block = new BlocksInfo { Name = "SIG-01-MAIN" };
            block.Parementers.Add(new BlockParam { Name = "TAB-01-10-SCOPE", Value = "ScopeValue" });

            Assert.Equal("ScopeValue", block.GetValue("SCOPE"));
        }

        [Fact]
        public void GetValue_ReturnsKKS()
        {
            var block = new BlocksInfo();
            block.Parementers.Add(new BlockParam { Name = "TAG_MAIN", Value = "KKS-001" });

            Assert.Equal("KKS-001", block.GetValue("KKS"));
        }

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
        public void GetValueAprox_ForSignalPath_CombinesEndpoints()
        {
            var block = new BlocksInfo { Name = "SIG-01-ANY" };
            block.Parementers.Add(new BlockParam { Name = "SIGNAL_FROM_DEVICE", Value = "Start" });
            block.Parementers.Add(new BlockParam { Name = "SIGNAL_TO_DEVICE", Value = "End" });

            var path = block.GetValueAprox("PATH");

            Assert.Equal("Start<=>End", path);
        }

        [Fact]
        public void GetValueAprox_ForCablePath_CombinesEndpoints()
        {
            var block = new BlocksInfo { Name = "CAB-01-ANY" };
            block.Parementers.Add(new BlockParam { Name = "CABLE_FROM_DEVICE", Value = "PointA" });
            block.Parementers.Add(new BlockParam { Name = "CABLE_TO_DEVICE", Value = "PointB" });

            var path = block.GetValueAprox("PATH");

            Assert.Equal("PointA<=>PointB", path);
        }

        [Fact]
        public void GetValueAprox_ForUnknownPath_ReturnsDefault()
        {
            var block = new BlocksInfo { Name = "OTHER-01-ANY" };

            var path = block.GetValueAprox("PATH");

            Assert.Equal("...", path);
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

        [Fact]
        public void GetScope_ReturnsCorrectScope()
        {
            var block = new BlocksInfo { Name = "SIG-05-MAIN" };
            block.Parementers.Add(new BlockParam { Name = "TAB-05-10-SCOPE", Value = "TestScope" });

            Assert.Equal("TestScope", block.GetScope());
        }

        [Fact]
        public void ToString_ReturnsNameAndTag()
        {
            var block = new BlocksInfo { Name = "SIG-01" };
            block.Parementers.Add(new BlockParam { Name = "TAG_INFO", Value = "TestTag" });

            var result = block.ToString();

            Assert.Contains("SIG-01", result);
            Assert.Contains("TestTag", result);
        }
    }
}