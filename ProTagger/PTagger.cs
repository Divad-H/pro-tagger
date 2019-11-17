﻿using static ReactiveMvvm.DisposableExtensions;
using static ReacitveMvvm.ObservableExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveMvvm;
using ReacitveMvvm;
using ProTagger.Utilities;

namespace ProTagger
{
    public class PTagger : INotifyPropertyChanged, IDisposable
    {
        private readonly ISchedulers _schedulers;

        private string _repositoryPath = @"G:\Projects\pro-tagger";
        public string RepositoryPath
        {
            get { return _repositoryPath; }
            set
            {
                if (_repositoryPath == value)
                    return;
                _repositoryPath = value;
                NotifyPropertyChanged();
            }
        }

        private RepositoryViewModel? _repository;
        public RepositoryViewModel? Repository
        {
            get { return _repository; }
            set
            {
                if (_repository == value)
                    return;
                _repository?.Dispose();
                _repository = value;
                NotifyPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }

        public IObservable<RepositoryViewModel> RepositoryObservable { get; }

        public PTagger(IRepositoryFactory repositoryFactory, ISchedulers schedulers)
        {
            _schedulers = schedulers;

            var repositoryPath = this.FromProperty(vm => vm.RepositoryPath);

            var refreshCommand = ReactiveCommand.Create<object, object>(
                    canExecute: repositoryPath
                        .Select(path => !string.IsNullOrWhiteSpace(path)),
                    execute: (param) =>
                    {
                        return param;
                    },
                    scheduler: schedulers.Dispatcher)
                .DisposeWith(_disposable);

            var repository = Observable
                .Return(_repositoryPath)
                .Concat(refreshCommand
                    .ObserveOn(schedulers.Dispatcher)
                    .SelectMany(_ => repositoryPath.Take(1))
                    )
                .Select(path => Observable.FromAsync(ct => RepositoryViewModel.Create(ct, repositoryFactory, path)))
                .Switch()
                .SkipNull();

            RepositoryObservable = repository;

            repository
                .Subscribe(repository => { if (repository != null) Repository = repository; })
                .DisposeWith(_disposable);

            RefreshCommand = refreshCommand;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        public void Dispose()
        {
            _repository?.Dispose();
            _disposable.Dispose();
        }
    }
}