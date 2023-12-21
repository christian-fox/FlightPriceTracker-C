using System;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Controls;

namespace RyanairFlightTrackBot
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private int totalCheckpoints = 5;
        private int completedCheckpoints;



        internal void HandleCheckpointCompletion()
        {
            // Update the completed checkpoints count
            completedCheckpoints++;

            // Calculate the progress percentage
            double progressPercentage = (double)completedCheckpoints / totalCheckpoints * 100;

            // Update the ProgressBar value
            progressBar.Value = progressPercentage;

            // Check if all checkpoints are completed
            if (completedCheckpoints == totalCheckpoints)
            {
                // Optionally close the loading window or perform other actions
                Close();
            }
        }

        // Additional methods to handle completion of specific checkpoints
        // For example:
        // private void HandleEvent1Completion(object sender, EventArgs e)
        // {
        //     checkpoints[0] = true;
        //     HandleCheckpointCompletion();
        // }


        internal LoadingWindow()
        {
            InitializeComponent();

            // Subscribe to the events or methods that represent completion of checkpoints
            // For example:
            // Event1 += HandleEvent1Completion;
            // Event2 += HandleEvent2Completion;
            // ...

        }
    }
}
