using Autodesk.AutoCAD.EditorInput;
using EE_Analyzer.Models;
using EE_RoofFramer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace EE_RoofFramer
{
    /// <summary>
    /// Interaction logic for EE_FDNInputDialog.xaml
    /// </summary>
    public partial class EE_ROOFInputDialog : Window
    {
        // signals that the mode will use the quantity and spacings as the same for all beams
        public RoofFramingLayout ROOFLayout { get; set; } = null;

        public static string VERSION_INFO { get; } = EE_ROOF_Settings.CURRENT_VERSION_NUM;
        public static string COPYRIGHT_INFO { get; } = EE_ROOF_Settings.SIGNATURE_LABEL;
        public bool dialog_should_close { get; private set; }
        public bool dialog_is_complete { get; private set; }

        public bool preview_mode { get; set; } = true;
        public int current_preview_mode_number { get; set; }

        public EE_ROOFInputDialog(
            RoofFramingLayout roof_layout, bool should_close, int mode
            )
        {
            InitializeComponent();

            DataContext = this;
            
            current_preview_mode_number = mode;
            dialog_should_close = should_close;

            ROOFLayout = roof_layout;

        }

        /// <summary>
        /// Parses the form data.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool ParseFormData()
        {
            bool resultOK = true;

            // Enter data to be read and verified here
            // ...

            return resultOK;
        }

        /// <summary>
        /// Update the Foundation Layour data
        /// </summary>
        private void StoreFormData()
        {

        }

        /// <summary>
        /// Handles the 'Click' event of the 'OK' button.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event data.</param>
        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            preview_mode = true;
            dialog_should_close = false;
            this.DialogResult = true;

            try
            {
                if (ParseFormData())
                {
                    // write the form data to the foundation layout object
                    StoreFormData();

                    ROOFLayout.DrawRoofFramingDetails(
                        preview_mode, dialog_should_close, current_preview_mode_number
                        );

                    current_preview_mode_number++;
                    preview_mode = true;
                    dialog_should_close = false;
                }
                else
                {
                    MessageBox.Show("Error parsing dialog window values");
                    return;
                }

            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error in reading dialog information:  " + ex.Message);
                return;
            }


        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {

            preview_mode = false;
            dialog_should_close = true;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            preview_mode = false; // turn off preview mode so final drawings can be completed.
            dialog_should_close = true;
            try
            {
                if (ParseFormData())
                {
                    // write the form data to the foundation layout object
                    StoreFormData();

                    ROOFLayout.DrawRoofFramingDetails(
                        false, false, ROOFLayout.CurrentDirectionMode
                        );

                    // now set the flag to close the dialog
                    preview_mode = false;
                    dialog_should_close = true;
                    dialog_is_complete = true;

                }
                else
                {
                    MessageBox.Show("Error parsing dialog window values");
                    return;
                }

            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error in reading dialog information:  " + ex.Message);
                return;
            }

            ROOFLayout.IsComplete = dialog_is_complete;
            ROOFLayout.PreviewMode = preview_mode;
            this.DialogResult = false;

        }

        private void UpdateOKButton()
        {
            btnOK.IsEnabled = true;
            btnOK.Visibility = Visibility.Visible;
         
            start.InvalidateArrange();
        }


        /// <summary>
        /// Needed to help the window start in the upper left corner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void start_Loaded(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(100);
            Window win = sender as Window;
            win.WindowStartupLocation = WindowStartupLocation.Manual;
            win.Top = 0;
            win.Left = 0;
            //           win.Show();
        }

    }
}
