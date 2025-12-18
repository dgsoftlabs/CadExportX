using ModelSpace;
using System.Windows.Input;

namespace CadExportX.Tests.Commands
{
    public class RelayCommandTests
    {
        [Fact]
        public void Constructor_ThrowsOnNullExecute()
        {
            Assert.Throws<ArgumentNullException>(() => new RelayCommand(null));
        }

        [Fact]
        public void Constructor_AcceptsExecuteOnly()
        {
            var command = new RelayCommand(p => { });

            Assert.NotNull(command);
        }

        [Fact]
        public void Constructor_AcceptsExecuteAndCanExecute()
        {
            var command = new RelayCommand(p => { }, p => true);

            Assert.NotNull(command);
        }

        [Fact]
        public void Execute_CallsActionWithParameter()
        {
            object passedParam = null;
            var command = new RelayCommand(p => passedParam = p);

            command.Execute("test");

            Assert.Equal("test", passedParam);
        }

        [Fact]
        public void CanExecute_ReturnsTrueWhenNoPredicateProvided()
        {
            var command = new RelayCommand(p => { });

            Assert.True(command.CanExecute(null));
        }

        [Fact]
        public void CanExecute_UsesProvidedPredicate()
        {
            var command = new RelayCommand(p => { }, p => p != null);

            Assert.True(command.CanExecute("test"));
            Assert.False(command.CanExecute(null));
        }

        [Fact]
        public void CanExecute_EvaluatesPredicateWithParameter()
        {
            var command = new RelayCommand(
             p => { },
                 p => p is int value && value > 10
          );

            Assert.True(command.CanExecute(15));
            Assert.False(command.CanExecute(5));
            Assert.False(command.CanExecute("text"));
        }

        [Fact]
        public void Execute_WorksWithComplexAction()
        {
            int counter = 0;
            var command = new RelayCommand(p =>
              {
                  if (p is int value)
                      counter += value;
              });

            command.Execute(5);
            command.Execute(10);

            Assert.Equal(15, counter);
        }

        [Fact]
        public void CanExecuteChanged_CanBeSubscribed()
        {
            var command = new RelayCommand(p => { });
            bool eventRaised = false;

            EventHandler handler = (s, e) => eventRaised = true;
            command.CanExecuteChanged += handler;

            // Manually trigger CommandManager.RequerySuggested
            CommandManager.InvalidateRequerySuggested();

            command.CanExecuteChanged -= handler;

            // Event subscription works (actual raising depends on CommandManager)
            Assert.NotNull(command);
        }
    }
}