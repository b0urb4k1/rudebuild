﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RudeBuild;

namespace RudeBuildAddIn
{
    public partial class SolutionSettingsDialog : Window
    {
        private Settings _settings;
        private SolutionInfo _solutionInfo;
        private SolutionSettings _solutionSettings;
        private TreeViewItem _selectedTreeViewItem;

        public SolutionSettingsDialog(Settings settings, SolutionInfo solutionInfo)
        {
            _settings = settings;
            _solutionInfo = solutionInfo;
            _solutionSettings = SolutionSettings.Load(settings, solutionInfo);

            InitializeComponent();
            _window.DataContext = _solutionSettings;
        }

        private void OnOK(object sender, RoutedEventArgs e)
        {
            _settings.SolutionSettings = _solutionSettings;
            _solutionSettings.Save(_settings, _solutionInfo);
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnTreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            _selectedTreeViewItem = e.OriginalSource as TreeViewItem;
        }

        private void OnTreeViewItemUnselected(object sender, RoutedEventArgs e)
        {
            _selectedTreeViewItem = null;
        }

        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject clickedControl = e.OriginalSource as DependencyObject;
            if (null != clickedControl)
            {
                TreeViewItem treeViewItem = clickedControl.VisualUpwardSearch<TreeViewItem>();
                if (null != treeViewItem)
                {
                    treeViewItem.Focus();
                    e.Handled = true;
                }
            }
        }

        private string GetSelectedFileName()
        {
            if (null == _treeViewExcludedFileNames.SelectedItem)
                return null;
            string fileName = _treeViewExcludedFileNames.SelectedItem as string;
            if (string.IsNullOrEmpty(fileName))
                return null;
            return fileName;
        }

        private ProjectInfo GetProjectInfoFromTreeViewItem(TreeViewItem projectTreeViewItem)
        {
            TextBlock textBlock = projectTreeViewItem.VisualDownwardSearch<TextBlock>();
            if (null == textBlock)
                return null;
            string projectName = textBlock.Text;
            ProjectInfo projectInfo = _solutionInfo.GetProjectInfo(projectName);
            if (null == projectInfo)
                return null;
            return projectInfo;
        }

        private void OnAddExcludedCppFileNameForProject(object sender, RoutedEventArgs e)
        {
            if (null == _selectedTreeViewItem)
                return;
            ProjectInfo projectInfo = GetProjectInfoFromTreeViewItem(_selectedTreeViewItem);
            if (null == projectInfo)
                return;

            SolutionSettingsAddExcludedCppFileNameDialog dialog = new SolutionSettingsAddExcludedCppFileNameDialog(projectInfo, _solutionSettings);
            try
            {
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    foreach (string fileName in dialog.FileNamesToExclude)
                    {
                        _solutionSettings.ExcludeCppFileNameForProject(projectInfo, fileName);
                    }
                }
            }
            finally
            {
                dialog.Close();
            }
        }

        private void OnRemoveExcludedCppFileNameForProject(object sender, RoutedEventArgs e)
        {
            if (null == _selectedTreeViewItem)
                return;

            string fileName = GetSelectedFileName();
            if (null == fileName)
                return;

            DependencyObject parentControl = VisualTreeHelper.GetParent(_selectedTreeViewItem);
            if (null == parentControl)
                return;
            TreeViewItem projectTreeViewItem = parentControl.VisualUpwardSearch<TreeViewItem>();
            if (null == projectTreeViewItem)
                return;
            ProjectInfo projectInfo = GetProjectInfoFromTreeViewItem(projectTreeViewItem);
            if (null == projectInfo)
                return;

            _solutionSettings.RemoveExcludedCppFileNameForProject(projectInfo, fileName);
        }
    }
}