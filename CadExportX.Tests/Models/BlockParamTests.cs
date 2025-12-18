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
        public void Change_PropertyCanBeSet()
        {
            var param = new BlockParam
      {
     Change = ChangesKind.Added
      };

            Assert.Equal(ChangesKind.Added, param.Change);

    param.Change = ChangesKind.Modfied;
            Assert.Equal(ChangesKind.Modfied, param.Change);
        }

        [Fact]
  public void DefaultChange_IsNotChanged()
 {
       var param = new BlockParam();

       Assert.Equal(ChangesKind.NotChanged, param.Change);
        }
    }
}
