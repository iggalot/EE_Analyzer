using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace EE_Analyzer
{
    /// <summary>
    /// Interaction logic for EE_FDNInputDialog.xaml
    /// </summary>
    public partial class EE_FDNInputDialog : Window
    {
        private FoundationLayout CurrentFoundationLayout { get; set; }

        public static string VERSION_INFO { get; } = EE_Settings.CURRENT_VERSION_NUM;
        public static string COPYRIGHT_INFO { get; } = EE_Settings.SIGNATURE_LABEL;

        public EE_FDNInputDialog(
            int x_qty = 5, double x_spa=120, double x_width=12, double x_depth=24,
            int y_qty = 7, double y_spa = 120, double y_width = 12, double y_depth = 24,
            int beam_x_strand_qty=2, int slab_x_strand_qty=8, int beam_y_strand_qty=2, int slab_y_strand_qty=8, double neglect_pt_dim=120)
        {
            InitializeComponent();
            DataContext = this;

            BEAM_X_QTY.Text = x_qty.ToString();
            BEAM_X_SPACING.Text = x_spa.ToString();
            BEAM_X_DEPTH.Text = x_depth.ToString();
            BEAM_X_WIDTH.Text = x_width.ToString();
            BEAM_X_STRAND_QTY.Text = beam_x_strand_qty.ToString();
            SLAB_X_STRAND_QTY.Text = slab_x_strand_qty.ToString();
                        
            BEAM_Y_QTY.Text = y_qty.ToString();
            BEAM_Y_SPACING.Text = y_spa.ToString();
            BEAM_Y_DEPTH.Text = y_depth.ToString();
            BEAM_Y_WIDTH.Text = y_width.ToString();
            BEAM_Y_STRAND_QTY.Text = beam_y_strand_qty.ToString();
            SLAB_Y_STRAND_QTY.Text = slab_y_strand_qty.ToString();

            NEGLECT_PT_DIM.Text = neglect_pt_dim.ToString();

            // Create our foundation layout object
            
        }

        /// <summary>
        /// Handles the 'Click' event of the 'OK' button.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event data.</param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {

            DialogResult = true;

            int x_qty;
            double x_spa;
            double x_depth;
            double x_width;
            int beam_x_strand_qty;
            int slab_x_strand_qty;

            int y_qty;
            double y_spa;
            double y_depth;
            double y_width;
            int beam_y_strand_qty;
            int slab_y_strand_qty;

            double neglect_pt_dim;

            bool resultOK = true;
            // Grab the values from the dialog and check for validity
            try
            {
                if (Int32.TryParse(BEAM_X_QTY.Text, out x_qty) is false)
                {
                    resultOK = false;
                    MessageBox.Show("Error reading X-dir beam qty");
                }


                if (Int32.TryParse(BEAM_Y_QTY.Text, out y_qty) is false)
                {
                    MessageBox.Show("Error reading X-dir beam qty");
                    resultOK = false;
                } 

                if (Int32.TryParse(BEAM_X_STRAND_QTY.Text, out beam_x_strand_qty) is false)
                {
                    MessageBox.Show("Error reading X-dir beam qty");
                    resultOK = false;
                } 

                if (Int32.TryParse(BEAM_Y_STRAND_QTY.Text, out beam_y_strand_qty) is false)
                {
                    MessageBox.Show("Error reading Y-dir beam qty");
                    resultOK = false;
                } 

                if (Int32.TryParse(SLAB_X_STRAND_QTY.Text, out slab_x_strand_qty) is false)
                {
                    MessageBox.Show("Error reading X-dir slab strand qty");
                    resultOK = false;
                } 

                if (Int32.TryParse(SLAB_Y_STRAND_QTY.Text, out slab_y_strand_qty) is false)
                {
                    MessageBox.Show("Error reading Y-dir slab strand qty");
                    resultOK = false;
                } 

                if (Double.TryParse(BEAM_X_SPACING.Text, out x_spa) is false)
                {
                    MessageBox.Show("Error reading X-dir beam spacing");
                    resultOK = false;
                } 

                if (Double.TryParse(BEAM_X_DEPTH.Text, out x_depth) is false)
                {
                    MessageBox.Show("Error reading X-dir beam depth");
                    resultOK = false;
                } 

                if (Double.TryParse(BEAM_X_WIDTH.Text, out x_width) is false)
                {
                    MessageBox.Show("Error reading X-dir beam width");
                    resultOK = false;
                } 

                if (Double.TryParse(BEAM_Y_SPACING.Text, out y_spa) is false)
                {
                    MessageBox.Show("Error reading Y-dir beam spacing");
                    resultOK = false;
                } 

                if (Double.TryParse(BEAM_Y_DEPTH.Text, out y_depth) is false)
                {
                    MessageBox.Show("Error reading Y-dir beam depth");
                    resultOK = false;
                } 

                if (Double.TryParse(BEAM_Y_WIDTH.Text, out y_width) is false)
                {
                    MessageBox.Show("Error reading Y-dir beam width");
                    resultOK = false;
                }

                if (Double.TryParse(NEGLECT_PT_DIM.Text, out neglect_pt_dim) is false)
                {
                    MessageBox.Show("Error reading Neglect PT dimension");
                    resultOK = false;
                }

                if (resultOK)
                {
                    CurrentFoundationLayout = new FoundationLayout();

                    CurrentFoundationLayout.DrawFoundationDetails(
                        x_qty, x_spa, x_depth, x_width, 
                        y_qty, y_spa, y_depth, y_width, 
                        beam_x_strand_qty, slab_x_strand_qty, beam_y_strand_qty, slab_y_strand_qty, neglect_pt_dim);
                } else
                {
                    MessageBox.Show("Error parsing dialog window values");
                }

            } catch (System.Exception ex)
            {
                MessageBox.Show("Error in reading dialog information:  " + ex.Message);
            }
        }

        private void X_Detail_Button_Click(object sender, RoutedEventArgs e)
        {
            if(spX_DIR_DETAILS.Visibility == Visibility.Collapsed)
            {
                spX_DIR_DETAILS.Visibility = Visibility.Visible;
                spX_DIR_DEFAULT.Visibility = Visibility.Collapsed;
            } else
            {
                {
                    spX_DIR_DETAILS.Visibility = Visibility.Collapsed;
                    spX_DIR_DEFAULT.Visibility = Visibility.Visible;
                }
            }
        }

        private void Y_Detail_Button_Click(object sender, RoutedEventArgs e)
        {
            if (spY_DIR_DETAILS.Visibility == Visibility.Collapsed)
            {
                spY_DIR_DETAILS.Visibility = Visibility.Visible;
                spY_DIR_DEFAULT.Visibility = Visibility.Collapsed;
            }
            else
            {
                {
                    spY_DIR_DETAILS.Visibility = Visibility.Collapsed;
                    spY_DIR_DEFAULT.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
