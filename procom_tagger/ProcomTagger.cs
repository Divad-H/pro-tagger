using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace procom_tagger
{
    public class ProcomTagger : INotifyPropertyChanged
    {
        private string _repositoryPath = @"G:\projects\libgit2sharp";
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

        private RepositoryViewModel _repository;
        public RepositoryViewModel Repository
        {
            get { return _repository; }
            set
            {
                if (_repository == value)
                    return;
                _repository = value;
                NotifyPropertyChanged();
            }
        }

        public ProcomTagger()
        {
            _repository = new RepositoryViewModel(_repositoryPath);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
