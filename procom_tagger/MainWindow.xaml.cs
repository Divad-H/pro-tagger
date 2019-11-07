﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace procom_tagger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<GitLog.LogGraphNode> Commits { get; set; }
        public MainWindow()
        {
            Commits = GitLog.LogGraph.CreateGraph("G:\\Projects\\libgit2sharp");
            DataContext = this;
            InitializeComponent();
        }
    }
}
