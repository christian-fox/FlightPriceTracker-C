using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;

namespace RyanairFlightTrackBot
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private List<bool> checkpointList = Enumerable.Repeat(false, 8).ToList();

        /// <summary>
        /// Updates the loading bar based on the percentage of true values in checkpointList.
        /// Note, the maximum checkpointNum arg value is checkpointList.Count - 1, as lists are 0-indexed in C#.
        /// </summary>
        /// <param name="checkpointNum"></param>
        internal void UpdateProgressBar(int checkpointNum)
        {
            // Update the checkpoint status in the list
            if (checkpointNum >= 0 && checkpointNum < checkpointList.Count)
            {
                checkpointList[checkpointNum] = true;
            }

            // Calculate the progress percentage
            double progressPercentage = (double)checkpointList.Count(b => b) / checkpointList.Count * 100;

            // Update the ProgressBar value
            progressBar.Value = progressPercentage;

            // Check if all checkpoints are completed
            if (checkpointList.All(b => b))
            {
                // Optionally close the loading window or perform other actions
                Close();
            }
        }

        internal LoadingWindow()
        {
            InitializeComponent();

        }
    }
}
