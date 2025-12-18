using ModelSpace;

namespace CadExportX.Tests.Models
{
    public class SettingsTests
    {
        [Fact]
        public void Constructor_InitializesWithBlockInfo()
        {
            var block = new BlocksInfo { Name = "TestBlock" };
            block.Parementers.Add(new BlockParam { Name = "TAG", Value = "KKS" });
            block.Parementers.Add(new BlockParam { Name = "DESC", Value = "Description" });

            var sett = new Settings(block);

            Assert.Equal("TestBlock", sett.Name);
            Assert.True(sett.Enable);
            Assert.Equal(2, sett.Params.Count);
        }

        [Fact]
        public void Enable_PropagatesToAllParameters()
        {
            var block = new BlocksInfo();
            block.Parementers.Add(new BlockParam { Name = "TAG", Value = "KKS" });
            block.Parementers.Add(new BlockParam { Name = "DESC", Value = "Description" });

            var sett = new Settings(block);

            Assert.True(sett.Enable);
            Assert.True(sett.Params.All(p => p.Enable));

            sett.Enable = false;
            Assert.False(sett.Enable);
            Assert.All(sett.Params, p => Assert.False(p.Enable));

            sett.Enable = true;
            Assert.True(sett.Enable);
            Assert.All(sett.Params, p => Assert.True(p.Enable));
        }

        [Fact]
        public void Name_CanBeSetAndRetrieved()
        {
            var sett = new Settings { Name = "TestName" };

            Assert.Equal("TestName", sett.Name);
        }

        [Fact]
        public void Params_AreOrderedByName()
        {
            var block = new BlocksInfo();
            block.Parementers.Add(new BlockParam { Name = "ZEBRA", Value = "Z" });
            block.Parementers.Add(new BlockParam { Name = "ALPHA", Value = "A" });
            block.Parementers.Add(new BlockParam { Name = "BETA", Value = "B" });

            var sett = new Settings(block);

            Assert.Equal("ALPHA", sett.Params[0].Name);
            Assert.Equal("BETA", sett.Params[1].Name);
            Assert.Equal("ZEBRA", sett.Params[2].Name);
        }

        [Fact]
        public void PropertyChanged_RaisedOnNameChange()
        {
            var sett = new Settings();
            bool wasRaised = false;
            sett.PropertyChanged += (s, e) =>
              {
                  if (e.PropertyName == nameof(Settings.Name))
                      wasRaised = true;
              };

            sett.Name = "NewName";

            Assert.True(wasRaised);
        }

        [Fact]
        public void PropertyChanged_RaisedOnEnableChange()
        {
            var sett = new Settings();
            bool wasRaised = false;
            sett.PropertyChanged += (s, e) =>
                      {
                          if (e.PropertyName == nameof(Settings.Enable))
                              wasRaised = true;
                      };

            sett.Enable = false;

            Assert.True(wasRaised);
        }
    }
}