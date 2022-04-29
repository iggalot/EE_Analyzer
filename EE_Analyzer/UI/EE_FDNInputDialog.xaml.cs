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
        // private field
        double radius;

        /// <summary>
        /// Gets the selected layer name.
        /// </summary>
        public string Layer => (string)cbxLayer.SelectedItem;

        /// <summary>
        /// Gets the radius.
        /// </summary>
        public double Radius => radius;

        /// <summary>
        /// Creates a new instance of ModalWpfDialog.
        /// </summary>
        /// <param name="layers">Layer names collection.</param>
        /// <param name="layer">Default layer name.</param>
        /// <param name="radius">Default radius</param>
        public EE_FDNInputDialog(List<string> layers, string layer, double radius, 
            int x_qty = 5, double x_spa=120, double x_width=12, double x_depth=24,
            int y_qty = 7, double y_spa = 120, double y_width = 12, double y_depth = 24,
            int beam_x_strand_qty=2, int slab_x_strand_qty=8, int beam_y_strand_qty=2, int slab_y_strand_qty=8)
        {
            InitializeComponent();
            this.radius = radius;
            cbxLayer.ItemsSource = layers;
            cbxLayer.SelectedItem = layer;
            txtRadius.Text = radius.ToString();

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

        }

        /// <summary>
        /// Handles the 'Click' event of the 'Radius' button.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event data.</param>
        private void btnRadius_Click(object sender, RoutedEventArgs e)
        {
            // prompt the user to specify distance
            var ed = AcAp.DocumentManager.MdiActiveDocument.Editor;
            var opts = new PromptDistanceOptions("\nSpecify the radius: ");
            opts.AllowNegative = false;
            opts.AllowZero = false;
            var pdr = ed.GetDistance(opts);
            if (pdr.Status == PromptStatus.OK)
            {
                txtRadius.Text = pdr.Value.ToString();
            }
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

            bool resultOK = true;
            // Grab the values from the dialog and check for validity
            try
            {
                if (Int32.TryParse(BEAM_X_QTY.Text, out x_qty) is false)
                    resultOK = false;

                if (Int32.TryParse(BEAM_Y_QTY.Text, out y_qty) is false)
                    resultOK = false;

                if (Int32.TryParse(BEAM_X_STRAND_QTY.Text, out beam_x_strand_qty) is false)
                    resultOK = false;
                if (Int32.TryParse(BEAM_Y_STRAND_QTY.Text, out beam_y_strand_qty) is false)
                    resultOK = false;
                if (Int32.TryParse(SLAB_Y_STRAND_QTY.Text, out slab_x_strand_qty) is false)
                    resultOK = false;
                if (Int32.TryParse(SLAB_Y_STRAND_QTY.Text, out slab_y_strand_qty) is false)
                    resultOK = false;

                if (Double.TryParse(BEAM_X_SPACING.Text, out x_spa) is false)
                    resultOK = false;
                if (Double.TryParse(BEAM_X_DEPTH.Text, out x_depth) is false)
                    resultOK = false;
                if (Double.TryParse(BEAM_X_WIDTH.Text, out x_width) is false)
                    resultOK = false;
                if (Double.TryParse(BEAM_Y_SPACING.Text, out y_spa) is false)
                    resultOK = false;
                if (Double.TryParse(BEAM_Y_DEPTH.Text, out y_depth) is false)
                    resultOK = false;
                if (Double.TryParse(BEAM_Y_WIDTH.Text, out y_width) is false)
                    resultOK = false;

                if(resultOK)
                {
                    FoundationLayout.DrawFoundationDetails(
                        x_qty, x_spa, x_depth, x_width, 
                        y_qty, y_spa, y_depth, y_width, 
                        beam_x_strand_qty, slab_x_strand_qty, beam_y_strand_qty, slab_y_strand_qty);
                } else
                {
                    MessageBox.Show("Error parsing dialog window values");
                }

            } catch (System.Exception ex)
            {
                MessageBox.Show("Error in reading dialog information:  " + ex.Message);
            }

            //FoundationLayout.DrawFoundationDetails(10, 120, 12, 12, 10, 120, 12, 12);
        }

        /// <summary>
        /// Handles the 'TextChanged' event ot the 'Radius' TextBox.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event data.</param>
        private void txtRadius_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnOK.IsEnabled = double.TryParse(txtRadius.Text, out radius);
        }
    }
}
