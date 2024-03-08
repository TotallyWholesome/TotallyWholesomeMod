#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WholesomeLoader;

namespace TotallyWholesome.Utils;

public static class TwTask
{
    public static Task Run(Func<Task?> function, CancellationToken cancellationToken = default, [CallerFilePath] string file = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = -1) => Task.Run(function, cancellationToken).ContinueWith(
        t =>
        {
            if (!t.IsFaulted) return;
            var index = file.LastIndexOf('\\');
            if (index == -1) index = file.LastIndexOf('/');
            Con.Error(
                $"Error during task execution. ${file.Substring(index + 1, file.Length - index - 1)}::{member}:{line} " +
                t.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    
    public static Task Run(Task? function, CancellationToken cancellationToken = default, [CallerFilePath] string file = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = -1) => Task.Run(() => function, cancellationToken)
        .ContinueWith(
            t =>
            {
                if (!t.IsFaulted) return;
                var index = file.LastIndexOf('\\');
                if (index == -1) index = file.LastIndexOf('/');
                Con.Error(
                    $"Error during task execution. ${file.Substring(index + 1, file.Length - index - 1)}::{member}:{line} " +
                    t.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
}