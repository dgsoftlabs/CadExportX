using ModelSpace;

namespace CadExportX.Tests.Models
{
    public class ParamSettTests
    {
        [Fact]
        public void Constructor_InitializesDefaultValues()
        {
            var paramSett = new ParamSettings();

            Assert.Null(paramSett.Name);
            Assert.False(paramSett.Enable);
        }

        [Fact]
        public void Name_CanBeSetAndRetrieved()
        {
            var paramSett = new ParamSettings { Name = "TestParam" };

            Assert.Equal("TestParam", paramSett.Name);
        }

        [Fact]
        public void Enable_CanBeSetAndRetrieved()
        {
            var paramSett = new ParamSettings { Enable = true };

            Assert.True(paramSett.Enable);

            paramSett.Enable = false;
            Assert.False(paramSett.Enable);
        }

        [Fact]
        public void PropertyChanged_RaisedOnNameChange()
        {
            var paramSett = new ParamSettings();
            bool wasRaised = false;
            paramSett.PropertyChanged += (s, e) =>
          {
              if (e.PropertyName == nameof(ParamSettings.Name))
                  wasRaised = true;
          };

            paramSett.Name = "NewName";

            Assert.True(wasRaised);
        }

        [Fact]
        public void PropertyChanged_RaisedOnEnableChange()
        {
            var paramSett = new ParamSettings();
            bool wasRaised = false;
            paramSett.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ParamSettings.Enable))
                    wasRaised = true;
            };

            paramSett.Enable = true;

            Assert.True(wasRaised);
        }
    }
}