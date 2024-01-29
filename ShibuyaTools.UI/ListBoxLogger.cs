using System.Text;
using Microsoft.Extensions.Logging;

namespace ShibuyaTools;

internal class ListBoxLogger(ListBox listBox) : ILogger
{
    private IDisposable? scope;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        while (true)
        {
            var currentScope = scope;
            var newScope = new LoggerScope<TState>(this, state, currentScope);

            if (Interlocked.CompareExchange(ref scope, newScope, currentScope) == currentScope)
            {
                return newScope;
            }
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var currentScope = scope;
        listBox.BeginInvoke(() =>
        {
            var line = $"[{logLevel}] {currentScope}{formatter(state, exception)}";
            var index = listBox.Items.Add(line);
            listBox.SelectedIndex = index;
        });
    }

    private class LoggerScope<TState>(ListBoxLogger owner, TState state, IDisposable? parent = null) : IDisposable
    {
        public void Dispose()
        {
            Interlocked.CompareExchange(ref owner.scope, parent, this);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            if (parent is not null)
            {
                builder.Append(parent);
            }

            return builder.Append(state).Append(": ").ToString();
        }
    }
}
