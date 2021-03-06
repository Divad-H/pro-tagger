﻿using ProTagger.Utilities;
using ReacitveMvvm;
using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

[assembly: InternalsVisibleTo("ProTaggerTest")]
namespace ProTagger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class Schedulers : ISchedulers
        {
            public Schedulers(DispatcherScheduler dispatcher, DefaultScheduler threadPool, ImmediateScheduler immediate)
                => (_dispatcher, _threadPool, _immediate) = (dispatcher, threadPool, immediate);

            private readonly DispatcherScheduler _dispatcher;
            public IScheduler Dispatcher => _dispatcher;

            private readonly DefaultScheduler _threadPool;
            public IScheduler ThreadPool => _threadPool;

            private readonly ImmediateScheduler _immediate;
            public IScheduler Immediate => _immediate;
        }

        private class RepositoryFactory : IRepositoryFactory
        {
            public IRepositoryWrapper CreateRepository(string path)
                => new RepositoryWrapper(path);

            public string? DiscoverRepository(string path)
                => LibGit2Sharp.Repository.Discover(path);

            public bool IsValidRepository(string path)
                => LibGit2Sharp.Repository.IsValid(path);

            public string? RepositoryNameFromPath(string path)
            {
                var repoPath = DiscoverRepository(path);
                if (repoPath == null)
                    return null;
                return new DirectoryInfo(repoPath).Parent.Name;
            }
        }

        private class FileSystemWatcherWrapper : IFileSystemWatcher
        {
            private readonly FileSystemWatcher _fileSystemWatcher;
            public FileSystemWatcherWrapper(string directory, string filter)
            {
                _fileSystemWatcher = new FileSystemWatcher(directory, filter)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true,
                };
                _fileSystemWatcher.Changed += OnChanged;
                _fileSystemWatcher.Deleted += OnChanged;
                _fileSystemWatcher.Created += OnChanged;
                _fileSystemWatcher.Renamed += OnRenamed;
            }

            private void OnRenamed(object sender, RenamedEventArgs e)
                => Changed?.Invoke(this, null);

            private void OnChanged(object sender, FileSystemEventArgs e)
                => Changed?.Invoke(this, null);

            public event EventHandler<object?>? Changed;

            public void Dispose()
            {
                _fileSystemWatcher.Changed -= OnChanged;
                _fileSystemWatcher.Deleted -= OnChanged;
                _fileSystemWatcher.Created -= OnChanged;
                _fileSystemWatcher.Renamed -= OnRenamed;
                _fileSystemWatcher.Dispose();
            }
        }

        private class FileSystemService : IFileSystem
        {
            readonly Window _dialogOwnerWindow;

            public FileSystemService(Window dialogOwnerWindow)
                => _dialogOwnerWindow = dialogOwnerWindow;

            public IFileSystemWatcher CreateFileSystemWatcher(string directory, string filter)
                => new FileSystemWatcherWrapper(directory, filter);

            public string? SelectGitRepositoryDialog(string description, Func<string, bool> validationCallback)
            {
                using var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select a git repository",
                    AutoUpgradeEnabled = true,
                    UseDescriptionForTitle = true,
                };

                while (true)
                {
                    var win32Parent = new System.Windows.Forms.NativeWindow();
                    win32Parent.AssignHandle(new System.Windows.Interop.WindowInteropHelper(_dialogOwnerWindow).Handle);
                    var result = dialog.ShowDialog(win32Parent);
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        if (validationCallback(dialog.SelectedPath))
                            return dialog.SelectedPath;
                        dialog.RootFolder = Environment.SpecialFolder.Recent;
                        if (MessageBox.Show(_dialogOwnerWindow,
                                            "The selected folder is not a valid git repository, please select a git repository.",
                                            "Invalid Folder",
                                            MessageBoxButton.OKCancel,
                                            MessageBoxImage.Exclamation)
                                == MessageBoxResult.Cancel)
                            return null;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public MainWindow()
        {
            AllowDrop = true;

            var dropObservable = Observable
                .FromEvent<DragEventHandler, DragEventArgs>(
                    h => (sender, e) => h(e),
                    h => Drop += h,
                    h => Drop -= h)
                .Where(e => e.Data.GetDataPresent(DataFormats.FileDrop))
                .SelectMany(e => (string[])e.Data.GetData(DataFormats.FileDrop))
                .SkipNull();

            var pTagger = new PTagger(
                new RepositoryFactory(),
                new Schedulers(DispatcherScheduler.Current, Scheduler.Default, Scheduler.Immediate),
                new FileSystemService(this),
                dropObservable);

            Closed += (sender, args) => pTagger.Dispose();
            DataContext = pTagger;

            InitializeComponent();
        }

        private void CloseCommandHandler(object sender, RoutedEventArgs e)
            => Close();

        private void MinimizeCommandHandler(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeCommandHandler(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
    }
}
